using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Antigravity.Bridge
{
    [InitializeOnLoad]
    public static class AntigravityBridge
    {
        private const int DefaultPort = 8088;
        private static int currentPort = DefaultPort;
        private static HttpListener listener;
        private static Thread listenerThread;
        private static bool isRunning = false;

        private static readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();
        private static readonly List<string> capturedLogs = new List<string>();

        static AntigravityBridge()
        {
            EditorApplication.update += ProcessMainThreadQueue;
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
            EditorApplication.quitting += StopServer;
            Application.logMessageReceived += (condition, stackTrace, type) =>
            {
                lock (capturedLogs)
                {
                    if (capturedLogs.Count > 200) capturedLogs.RemoveAt(0);
                    capturedLogs.Add($"[{type}] {condition}");
                }
            };

            StartServer();
        }

        [MenuItem("Tools/Antigravity/Restart Bridge Server")]
        public static void RestartServer()
        {
            StopServer();
            StartServer();
        }

        [MenuItem("Tools/Antigravity/Check Bridge Status")]
        public static void ShowStatus()
        {
            EditorUtility.DisplayDialog(
                "Antigravity Bridge",
                $"Status: {(isRunning ? "Running" : "Stopped")}\nPort: {currentPort}\nURL: http://127.0.0.1:{currentPort}/",
                "OK"
            );
        }

        private static void StartServer()
        {
            if (isRunning) return;

            int[] candidatePorts = { 8088, 8089, 8090, 8091 };
            foreach (var port in candidatePorts)
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Start();
                    currentPort = port;
                    isRunning = true;
                    break;
                }
                catch (Exception)
                {
                    listener?.Close();
                    listener = null;
                }
            }

            if (!isRunning || listener == null)
            {
                Debug.LogWarning("[Antigravity Bridge] Could not start HTTP listener on ports 8088-8091.");
                return;
            }

            listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "AntigravityBridgeListener"
            };
            listenerThread.Start();
            Debug.Log($"<color=#4CAF50>[Antigravity Bridge]</color> Server listening on http://127.0.0.1:{currentPort}/");
        }

        private static void StopServer()
        {
            isRunning = false;
            try
            {
                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                }
            }
            catch { }
            finally
            {
                listener = null;
            }

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Abort();
                listenerThread = null;
            }
        }

        private static void ProcessMainThreadQueue()
        {
            while (mainThreadQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Antigravity Bridge] Main thread action error: {ex}");
                }
            }
        }

        private static void ListenLoop()
        {
            while (isRunning && listener != null && listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning($"[Antigravity Bridge] Listener loop exception: {ex.Message}");
                    }
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            // CORS headers
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string rawPath = req.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
            string body = "";
            if (req.HasEntityBody)
            {
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }
            }

            string responseString = "";
            int statusCode = 200;

            try
            {
                switch (rawPath)
                {
                    case "/status":
                    case "/ping":
                    case "":
                        responseString = DispatchToMainThread(GetStatusJson);
                        break;

                    case "/hierarchy":
                        responseString = DispatchToMainThread(GetHierarchyJson);
                        break;

                    case "/screenshot":
                        string cameraMode = req.QueryString["camera"] ?? "auto";
                        responseString = DispatchToMainThread(() => TakeScreenshot(cameraMode));
                        break;

                    case "/action":
                    case "/execute":
                        responseString = DispatchToMainThread(() => ExecuteAction(body));
                        break;

                    default:
                        statusCode = 404;
                        responseString = "{\"error\": \"Endpoint not found\"}";
                        break;
                }
            }
            catch (Exception ex)
            {
                statusCode = 500;
                responseString = $"{{\"error\": \"{EscapeJson(ex.Message)}\"}}";
            }

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                res.ContentType = "application/json; charset=utf-8";
                res.ContentLength64 = buffer.Length;
                res.StatusCode = statusCode;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.OutputStream.Close();
            }
            catch { }
        }

        private static string DispatchToMainThread(Func<string> function, int timeoutMs = 8000)
        {
            string result = null;
            Exception caughtException = null;
            var doneEvent = new ManualResetEventSlim(false);

            mainThreadQueue.Enqueue(() =>
            {
                try
                {
                    result = function();
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
                finally
                {
                    doneEvent.Set();
                }
            });

            if (!doneEvent.Wait(timeoutMs))
            {
                return "{\"error\": \"Execution timed out on Unity main thread\"}";
            }

            if (caughtException != null)
            {
                throw caughtException;
            }

            return result ?? "{}";
        }

        private static string GetStatusJson()
        {
            var scene = SceneManager.GetActiveScene();
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"status\":\"ok\",");
            sb.Append($"\"unityVersion\":\"{EscapeJson(Application.unityVersion)}\",");
            sb.Append($"\"productName\":\"{EscapeJson(Application.productName)}\",");
            sb.Append($"\"isPlaying\":{(EditorApplication.isPlaying ? "true" : "false")},");
            sb.Append($"\"isPaused\":{(EditorApplication.isPaused ? "true" : "false")},");
            sb.Append($"\"activeScene\":\"{EscapeJson(scene.name)}\",");
            sb.Append($"\"rootCount\":{scene.rootCount},");
            sb.Append($"\"dataPath\":\"{EscapeJson(Application.dataPath)}\"");
            sb.Append("}");
            return sb.ToString();
        }

        private static string GetHierarchyJson()
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            var sb = new StringBuilder();
            sb.Append("{\"scene\":\"").Append(EscapeJson(scene.name)).Append("\",\"objects\":[");

            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (i > 0) sb.Append(",");
                AppendGameObjectJson(sb, rootObjects[i]);
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendGameObjectJson(StringBuilder sb, GameObject go)
        {
            sb.Append("{");
            sb.Append($"\"name\":\"{EscapeJson(go.name)}\",");
            sb.Append($"\"instanceID\":{go.GetInstanceID()},");
            sb.Append($"\"active\":{(go.activeSelf ? "true" : "false")},");
            sb.Append($"\"tag\":\"{EscapeJson(go.tag)}\",");
            sb.Append($"\"layer\":{go.layer},");

            var pos = go.transform.position;
            sb.Append($"\"position\":[{pos.x:F3},{pos.y:F3},{pos.z:F3}],");

            var rot = go.transform.eulerAngles;
            sb.Append($"\"rotation\":[{rot.x:F3},{rot.y:F3},{rot.z:F3}],");

            var scale = go.transform.localScale;
            sb.Append($"\"scale\":[{scale.x:F3},{scale.y:F3},{scale.z:F3}],");

            // Components
            var components = go.GetComponents<Component>();
            sb.Append("\"components\":[");
            for (int c = 0; c < components.Length; c++)
            {
                if (c > 0) sb.Append(",");
                string typeName = components[c] != null ? components[c].GetType().Name : "null";
                sb.Append($"\"{EscapeJson(typeName)}\"");
            }
            sb.Append("],");

            // Children
            sb.Append("\"children\":[");
            for (int ch = 0; ch < go.transform.childCount; ch++)
            {
                if (ch > 0) sb.Append(",");
                AppendGameObjectJson(sb, go.transform.GetChild(ch).gameObject);
            }
            sb.Append("]");

            sb.Append("}");
        }

        [Serializable]
        private class ActionPayload
        {
            public string action;
            public string type;
            public string name;
            public string target;
            public string component;
            public bool play;
            public int index;
            public float[] position;
            public float[] rotation;
            public float[] scale;
            public float[] color;
        }

        private static string ExecuteAction(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return "{\"success\":false,\"error\":\"Empty payload\"}";
            }

            ActionPayload payload;
            try
            {
                payload = JsonUtility.FromJson<ActionPayload>(json);
            }
            catch (Exception ex)
            {
                return $"{{\"success\":false,\"error\":\"Invalid JSON: {EscapeJson(ex.Message)}\"}}";
            }

            string act = (payload.action ?? "").ToLowerInvariant();

            switch (act)
            {
                case "create_primitive":
                {
                    PrimitiveType primType = PrimitiveType.Cube;
                    if (!string.IsNullOrEmpty(payload.type))
                    {
                        if (Enum.TryParse<PrimitiveType>(payload.type, true, out var parsed))
                        {
                            primType = parsed;
                        }
                    }

                    GameObject go = GameObject.CreatePrimitive(primType);
                    if (!string.IsNullOrEmpty(payload.name))
                    {
                        go.name = payload.name;
                    }

                    if (payload.position != null && payload.position.Length == 3)
                    {
                        go.transform.position = new Vector3(payload.position[0], payload.position[1], payload.position[2]);
                    }
                    if (payload.scale != null && payload.scale.Length == 3)
                    {
                        go.transform.localScale = new Vector3(payload.scale[0], payload.scale[1], payload.scale[2]);
                    }

                    if (payload.color != null && payload.color.Length >= 3)
                    {
                        var renderer = go.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                            Color c = new Color(payload.color[0], payload.color[1], payload.color[2], payload.color.Length > 3 ? payload.color[3] : 1f);
                            mat.color = c;
                            renderer.material = mat;
                        }
                    }

                    Undo.RegisterCreatedObjectUndo(go, "Create Primitive via Antigravity");
                    Selection.activeGameObject = go;
                    return $"{{\"success\":true,\"name\":\"{EscapeJson(go.name)}\",\"instanceID\":{go.GetInstanceID()}}}";
                }

                case "create_empty":
                {
                    GameObject go = new GameObject(string.IsNullOrEmpty(payload.name) ? "GameObject" : payload.name);
                    if (payload.position != null && payload.position.Length == 3)
                    {
                        go.transform.position = new Vector3(payload.position[0], payload.position[1], payload.position[2]);
                    }
                    Undo.RegisterCreatedObjectUndo(go, "Create Empty GameObject via Antigravity");
                    Selection.activeGameObject = go;
                    return $"{{\"success\":true,\"name\":\"{EscapeJson(go.name)}\",\"instanceID\":{go.GetInstanceID()}}}";
                }

                case "modify_transform":
                {
                    GameObject go = FindTarget(payload.target);
                    if (go == null) return "{\"success\":false,\"error\":\"Target not found\"}";

                    Undo.RecordObject(go.transform, "Modify Transform via Antigravity");

                    if (payload.position != null && payload.position.Length == 3)
                    {
                        go.transform.position = new Vector3(payload.position[0], payload.position[1], payload.position[2]);
                    }
                    if (payload.rotation != null && payload.rotation.Length == 3)
                    {
                        go.transform.eulerAngles = new Vector3(payload.rotation[0], payload.rotation[1], payload.rotation[2]);
                    }
                    if (payload.scale != null && payload.scale.Length == 3)
                    {
                        go.transform.localScale = new Vector3(payload.scale[0], payload.scale[1], payload.scale[2]);
                    }

                    return $"{{\"success\":true,\"name\":\"{EscapeJson(go.name)}\"}}";
                }

                case "add_component":
                {
                    GameObject go = FindTarget(payload.target);
                    if (go == null) return "{\"success\":false,\"error\":\"Target not found\"}";

                    Type compType = FindComponentType(payload.component);
                    if (compType == null)
                    {
                        return $"{{\"success\":false,\"error\":\"Component type '{EscapeJson(payload.component)}' not found\"}}";
                    }

                    Component comp = Undo.AddComponent(go, compType);
                    return $"{{\"success\":true,\"name\":\"{EscapeJson(go.name)}\",\"component\":\"{compType.Name}\"}}";
                }

                case "delete_object":
                {
                    GameObject go = FindTarget(payload.target);
                    if (go == null) return "{\"success\":false,\"error\":\"Target not found\"}";

                    string name = go.name;
                    Undo.DestroyObjectImmediate(go);
                    return $"{{\"success\":true,\"deleted\":\"{EscapeJson(name)}\"}}";
                }

                case "set_play_mode":
                {
                    EditorApplication.isPlaying = payload.play;
                    return $"{{\"success\":true,\"isPlaying\":{(EditorApplication.isPlaying ? "true" : "false")}}}";
                }

                case "request_recompile":
                {
                    UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    return "{\"success\":true}";
                }

                case "select_object":
                {
                    GameObject go = FindTarget(payload.target);
                    if (go == null) return "{\"success\":false,\"error\":\"Target not found\"}";

                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                    return $"{{\"success\":true,\"selected\":\"{EscapeJson(go.name)}\"}}";
                }

                case "execute_menu":
                {
                    if (string.IsNullOrEmpty(payload.target)) return "{\"success\":false,\"error\":\"No menu item target specified\"}";
                    bool executed = EditorApplication.ExecuteMenuItem(payload.target);
                    return $"{{\"success\":{executed.ToString().ToLowerInvariant()},\"menu\":\"{EscapeJson(payload.target)}\"}}";
                }

                case "get_logs":
                {
                    var sb = new StringBuilder();
                    sb.Append("{\"success\":true,\"logs\":[");
                    lock (capturedLogs)
                    {
                        for (int i = 0; i < capturedLogs.Count; i++)
                        {
                            if (i > 0) sb.Append(",");
                            sb.Append("\"").Append(EscapeJson(capturedLogs[i])).Append("\"");
                        }
                        capturedLogs.Clear();
                    }
                    sb.Append("]}");
                    return sb.ToString();
                }

                case "dialogue_start":
                {
                    Type dtType = FindTypeInAssemblies("Runtime.Dialogue.DialogueTesting");
                    Type dmType = FindTypeInAssemblies("Runtime.Dialogue.Logic.DialogueManager");
                    if (dmType == null) return "{\"success\":false,\"error\":\"DialogueManager not found\"}";

                    var dmInstProp = dmType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object dmInst = dmInstProp?.GetValue(null);
                    if (dmInst == null) return "{\"success\":false,\"error\":\"DialogueManager.Instance is null (Is scene playing?)\"}";

                    Component dtComp = dtType != null ? UnityEngine.Object.FindFirstObjectByType(dtType) as Component : null;
                    if (dtComp != null)
                    {
                        var viewField = dtType.GetField("dialogueView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var dataField = dtType.GetField("testData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        object view = viewField?.GetValue(dtComp);
                        object data = dataField?.GetValue(dtComp);

                        if (view != null)
                        {
                            var regMethod = dmType.GetMethod("RegisterView");
                            regMethod?.Invoke(dmInst, new object[] { view });
                        }

                        if (data != null)
                        {
                            var startMethod = dmType.GetMethod("StartDialogue", new Type[] { data.GetType(), typeof(Action) });
                            Action callback = () => Debug.Log("[Bridge] Dialogue completed!");
                            startMethod?.Invoke(dmInst, new object[] { data, callback });
                            return "{\"success\":true,\"message\":\"Started dialogue via DialogueTesting settings\"}";
                        }
                        return "{\"success\":false,\"error\":\"testData field is null on DialogueTesting\"}";
                    }

                    return "{\"success\":false,\"error\":\"DialogueTesting component not found in scene\"}";
                }

                case "dialogue_advance":
                {
                    Type dmType = FindTypeInAssemblies("Runtime.Dialogue.Logic.DialogueManager");
                    var dmInstProp = dmType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object dmInst = dmInstProp?.GetValue(null);
                    if (dmInst == null) return "{\"success\":false,\"error\":\"DialogueManager.Instance is null\"}";

                    var advMethod = dmType.GetMethod("HandleAdvanceInput");
                    advMethod?.Invoke(dmInst, null);
                    return "{\"success\":true,\"message\":\"Advance handled\"}";
                }

                case "dialogue_auto":
                {
                    Type dmType = FindTypeInAssemblies("Runtime.Dialogue.Logic.DialogueManager");
                    var dmInstProp = dmType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object dmInst = dmInstProp?.GetValue(null);
                    if (dmInst == null) return "{\"success\":false,\"error\":\"DialogueManager.Instance is null\"}";

                    var autoMethod = dmType.GetMethod("ToggleAutoMode");
                    autoMethod?.Invoke(dmInst, null);
                    var isAutoProp = dmType.GetProperty("IsAutoMode");
                    bool isAuto = (bool)(isAutoProp?.GetValue(dmInst) ?? false);
                    return $"{{\"success\":true,\"isAuto\":{(isAuto ? "true" : "false")}}}";
                }

                case "dialogue_skip":
                {
                    Type dmType = FindTypeInAssemblies("Runtime.Dialogue.Logic.DialogueManager");
                    var dmInstProp = dmType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object dmInst = dmInstProp?.GetValue(null);
                    if (dmInst == null) return "{\"success\":false,\"error\":\"DialogueManager.Instance is null\"}";

                    var skipMethod = dmType.GetMethod("ToggleSkipMode");
                    skipMethod?.Invoke(dmInst, null);
                    var isSkipProp = dmType.GetProperty("IsSkipMode");
                    bool isSkip = (bool)(isSkipProp?.GetValue(dmInst) ?? false);
                    return $"{{\"success\":true,\"isSkip\":{(isSkip ? "true" : "false")}}}";
                }

                case "dialogue_backlog":
                {
                    Type dtType = FindTypeInAssemblies("Runtime.Dialogue.DialogueTesting");
                    Component dtComp = dtType != null ? UnityEngine.Object.FindFirstObjectByType(dtType) as Component : null;
                    if (dtComp != null)
                    {
                        var toggleMethod = dtType.GetMethod("ToggleBacklogUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        toggleMethod?.Invoke(dtComp, null);
                        return "{\"success\":true,\"message\":\"Backlog toggled\"}";
                    }
                    return "{\"success\":false,\"error\":\"DialogueTesting component not found\"}";
                }

                case "dialogue_status":
                {
                    Type dmType = FindTypeInAssemblies("Runtime.Dialogue.Logic.DialogueManager");
                    var dmInstProp = dmType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object dmInst = dmInstProp?.GetValue(null);
                    if (dmInst == null) return "{\"success\":false,\"error\":\"DialogueManager.Instance is null\"}";

                    string state = dmType.GetProperty("CurrentState")?.GetValue(dmInst)?.ToString() ?? "Unknown";
                    string speaker = dmType.GetProperty("CurrentSpeaker")?.GetValue(dmInst)?.ToString() ?? "";
                    bool isAuto = (bool)(dmType.GetProperty("IsAutoMode")?.GetValue(dmInst) ?? false);
                    bool isSkip = (bool)(dmType.GetProperty("IsSkipMode")?.GetValue(dmInst) ?? false);

                    Type viewType = FindTypeInAssemblies("Runtime.Dialogue.DialogueViewWindow");
                    Component viewComp = viewType != null ? UnityEngine.Object.FindFirstObjectByType(viewType) as Component : null;
                    string bodyText = "";
                    string nameText = "";
                    bool isTyping = false;
                    bool isShowingChoices = false;
                    List<string> choices = new List<string>();

                    if (viewComp != null)
                    {
                        var bTextProp = viewType.GetField("bodyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var nTextProp = viewType.GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var isTypingProp = viewType.GetProperty("IsTyping");
                        var isChoicesProp = viewType.GetProperty("IsShowingChoices");
                        var choiceParentField = viewType.GetField("choiceButtonParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (bTextProp != null)
                        {
                            object tmp = bTextProp.GetValue(viewComp);
                            if (tmp != null) bodyText = tmp.GetType().GetProperty("text")?.GetValue(tmp)?.ToString() ?? "";
                        }
                        if (nTextProp != null)
                        {
                            object tmp = nTextProp.GetValue(viewComp);
                            if (tmp != null) nameText = tmp.GetType().GetProperty("text")?.GetValue(tmp)?.ToString() ?? "";
                        }
                        if (isTypingProp != null) isTyping = (bool)(isTypingProp.GetValue(viewComp) ?? false);
                        if (isChoicesProp != null) isShowingChoices = (bool)(isChoicesProp.GetValue(viewComp) ?? false);

                        Transform choiceParent = choiceParentField?.GetValue(viewComp) as Transform;
                        if (choiceParent != null)
                        {
                            for (int i = 0; i < choiceParent.childCount; i++)
                            {
                                var child = choiceParent.GetChild(i);
                                if (child.gameObject.activeSelf)
                                {
                                    var textComp = child.GetComponentInChildren<TMPro.TMP_Text>() ?? (Component)child.GetComponentInChildren<UnityEngine.UI.Text>();
                                    string t = textComp != null ? textComp.GetType().GetProperty("text")?.GetValue(textComp)?.ToString() : child.name;
                                    choices.Add(t ?? "");
                                }
                            }
                        }
                    }

                    int logCount = 0;
                    Type logMgrType = FindTypeInAssemblies("Runtime.Dialogue.DialogueLogManager");
                    var logInstProp = logMgrType?.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object logInst = logInstProp?.GetValue(null);
                    if (logInst != null)
                    {
                        var logsProp = logMgrType.GetProperty("Logs");
                        var logsObj = logsProp?.GetValue(logInst) as System.Collections.ICollection;
                        if (logsObj != null) logCount = logsObj.Count;
                    }

                    var sb = new StringBuilder();
                    sb.Append("{");
                    sb.Append("\"success\":true,");
                    sb.Append($"\"state\":\"{EscapeJson(state)}\",");
                    sb.Append($"\"speaker\":\"{EscapeJson(speaker)}\",");
                    sb.Append($"\"nameText\":\"{EscapeJson(nameText)}\",");
                    sb.Append($"\"bodyText\":\"{EscapeJson(bodyText)}\",");
                    sb.Append($"\"isTyping\":{(isTyping ? "true" : "false")},");
                    sb.Append($"\"isAuto\":{(isAuto ? "true" : "false")},");
                    sb.Append($"\"isSkip\":{(isSkip ? "true" : "false")},");
                    sb.Append($"\"isShowingChoices\":{(isShowingChoices ? "true" : "false")},");
                    sb.Append($"\"logCount\":{logCount},");
                    sb.Append("\"choices\":[");
                    for (int i = 0; i < choices.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append("\"").Append(EscapeJson(choices[i])).Append("\"");
                    }
                    sb.Append("]");
                    sb.Append("}");
                    return sb.ToString();
                }

                case "dialogue_choice":
                {
                    Type viewType = FindTypeInAssemblies("Runtime.Dialogue.DialogueViewWindow");
                    Component viewComp = viewType != null ? UnityEngine.Object.FindFirstObjectByType(viewType) as Component : null;
                    if (viewComp == null) return "{\"success\":false,\"error\":\"DialogueViewWindow not found\"}";

                    var choiceParentField = viewType.GetField("choiceButtonParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Transform choiceParent = choiceParentField?.GetValue(viewComp) as Transform;
                    if (choiceParent == null) return "{\"success\":false,\"error\":\"choiceButtonParent not found\"}";

                    List<UnityEngine.UI.Button> btns = new List<UnityEngine.UI.Button>();
                    for (int i = 0; i < choiceParent.childCount; i++)
                    {
                        var child = choiceParent.GetChild(i);
                        if (child.gameObject.activeSelf)
                        {
                            var btn = child.GetComponent<UnityEngine.UI.Button>();
                            if (btn != null) btns.Add(btn);
                        }
                    }

                    if (payload.index >= 0 && payload.index < btns.Count)
                    {
                        btns[payload.index].onClick.Invoke();
                        return $"{{\"success\":true,\"message\":\"Choice {payload.index} clicked\"}}";
                    }

                    return $"{{\"success\":false,\"error\":\"Choice index {payload.index} out of range (count={btns.Count})\"}}";
                }

                default:
                    return $"{{\"success\":false,\"error\":\"Unknown action: {EscapeJson(act)}\"}}";
            }
        }

        private static GameObject FindTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) return null;

            if (int.TryParse(target, out int instanceId))
            {
                var obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
                if (obj != null) return obj;
            }

            return GameObject.Find(target);
        }

        private static Type FindComponentType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType(typeName, false, true);
                if (t != null && typeof(Component).IsAssignableFrom(t)) return t;

                t = assembly.GetType("UnityEngine." + typeName, false, true);
                if (t != null && typeof(Component).IsAssignableFrom(t)) return t;
            }

            return null;
        }

        private static Type FindTypeInAssemblies(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType(typeName, false, true);
                if (t != null) return t;

                t = assembly.GetType("UnityEngine." + typeName, false, true);
                if (t != null) return t;
            }

            return null;
        }

        private static string TakeScreenshot(string cameraMode)
        {
            Camera cam = null;

            if (cameraMode == "scene" || Camera.main == null)
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    cam = SceneView.lastActiveSceneView.camera;
                }
            }

            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                // Fallback to any camera in the scene
                cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (cam == null)
            {
                return "{\"success\":false,\"error\":\"No Camera found in scene\"}";
            }

            int width = 1280;
            int height = 720;

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            byte[] pngBytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);

            string scratchDir = @"C:\Users\G2546\.gemini\antigravity\scratch";
            if (!Directory.Exists(scratchDir))
            {
                Directory.CreateDirectory(scratchDir);
            }

            string savePath = Path.Combine(scratchDir, "unity_screenshot.png");
            File.WriteAllBytes(savePath, pngBytes);

            return $"{{\"success\":true,\"path\":\"{EscapeJson(savePath)}\",\"camera\":\"{EscapeJson(cam.name)}\",\"width\":{width},\"height\":{height}}}";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
