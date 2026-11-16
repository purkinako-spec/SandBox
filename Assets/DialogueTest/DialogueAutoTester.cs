using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using Runtime.Dialogue;
using Runtime.Dialogue.Core;
using Runtime.Dialogue.Logic;

namespace Runtime.Dialogue.Testing
{
    public class DialogueAutoTester : MonoBehaviour
    {
        public static string LogFilePath => @"C:\Users\G2546\.gemini\antigravity\scratch\dialogue_test_report.json";

        [SerializeField] private DialogueContainer testContainer;
        [SerializeField] private DialogueViewWindow dialogueView;

        private void Start()
        {
            StartCoroutine(RunTestRoutine());
        }

        private IEnumerator RunTestRoutine()
        {
            WriteLog("TEST_INIT", "Starting Dialogue System Auto Test");
            yield return new WaitForSeconds(0.5f);

            var dm = DialogueManager.Instance;
            if (dm == null)
            {
                WriteLog("TEST_ERROR", "DialogueManager.Instance is null");
                yield break;
            }

            if (dialogueView == null)
            {
                dialogueView = FindFirstObjectByType<DialogueViewWindow>();
            }

            if (dialogueView != null)
            {
                dm.RegisterView(dialogueView);
                WriteLog("TEST_STEP", "Registered DialogueViewWindow");
            }

            // DialogueTesting から testData を取得（未設定の場合）
            if (testContainer == null)
            {
                var dt = FindFirstObjectByType<DialogueTesting>();
                if (dt != null)
                {
                    var field = typeof(DialogueTesting).GetField("testData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    testContainer = field?.GetValue(dt) as DialogueContainer;
                }
            }

            if (testContainer == null)
            {
                WriteLog("TEST_ERROR", "DialogueContainer is null");
                yield break;
            }

            WriteLog("TEST_START", $"Starting dialogue with container: {testContainer.name}");
            bool dialogueFinished = false;

            dm.StartDialogue(testContainer, () =>
            {
                dialogueFinished = true;
                WriteLog("TEST_FINISHED", "Dialogue callback onCompleted fired!");
            });

            // 会話中の進行ループ
            int steps = 0;
            while (!dialogueFinished && steps < 50)
            {
                steps++;
                yield return new WaitForSeconds(0.8f);

                string speaker = dm.CurrentSpeaker;
                string state = dm.CurrentState.ToString();
                WriteLog("STEP_OBSERVE", $"Step {steps}: State={state}, Speaker={speaker}");

                if (dm.CurrentState == DialogueState.WaitingForChoice)
                {
                    WriteLog("STEP_CHOICE", "Waiting for choice detected. Selecting first choice.");
                    // 選択肢ボタンの最初のものを探してクリック
                    var buttons = dialogueView.GetComponentsInChildren<UnityEngine.UI.Button>();
                    if (buttons != null && buttons.Length > 0)
                    {
                        WriteLog("STEP_CHOICE_CLICK", $"Clicking choice button: {buttons[0].name}");
                        buttons[0].onClick.Invoke();
                    }
                    else
                    {
                        WriteLog("STEP_CHOICE_WARN", "No choice buttons found in dialogueView hierarchy.");
                    }
                }
                else if (dm.CurrentState == DialogueState.Playing || dm.CurrentState == DialogueState.WaitingForAdvance)
                {
                    WriteLog("STEP_ADVANCE", "Sending Advance input");
                    dm.HandleAdvanceInput();
                }
                else if (dm.CurrentState == DialogueState.Idle || dm.CurrentState == DialogueState.Ended)
                {
                    break;
                }
            }

            yield return new WaitForSeconds(0.5f);
            WriteLog("TEST_COMPLETE", $"Test finished. Total steps: {steps}, Final State: {dm.CurrentState}");
        }

        private void WriteLog(string tag, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {message}\n";
            File.AppendAllText(LogFilePath, line, Encoding.UTF8);
            Debug.Log($"[DialogueAutoTester] {tag}: {message}");
        }
    }
}
