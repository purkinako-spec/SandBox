using UnityEngine;
using UnityEditor;
using System.IO;

namespace MyActionGame
{
    public static class BuildHooks
    {
        // ビルド前に実行されるメソッド
        // ここでバージョンインクリメントやアセット準備などを行う
        public static void PreBuild()
        {
            Debug.Log("=== Unity CLI Loop連携: PreBuild 開始 ===");
            
            // 例: ビルド番号をPlayerSettingsに設定（実際のプロジェクトではCIのビルド番号を使用）
            // PlayerSettings.bundleVersion = "1.0." + System.DateTime.Now.ToString("yyMMddHHmm");
            
            // 一時フォルダーをクリア（ビルドの安定性向上のため）
            string tempPath = Path.Combine(Application.dataPath, "..", "Temp");
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                    Debug.Log("Tempフォルダーをクリアしました");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Tempフォルダーのクリアに失敗: " + e.Message);
                }
            }
            
            // ここでAIアシスタントからの提案を反映させる例：
            // 例えば、PlayerControllerの移動速度を一時的に調整するなど
            // （実際の運用では、スクリプト可能なオブジェクトや設定ファイルを推奨）
        }

        // ビルド後に実行されるメソッド
        // ここで成果物の後処理や通知などを行う
        public static void PostBuild(string buildPath)
        {
            Debug.Log($"=== Unity CLI Loop連携: PostBuild 完了 ===\nビルド出力: {buildPath}");
            
            // 例: ビルド成果物のサイズをログに出力
            if (File.Exists(buildPath))
            {
                FileInfo info = new FileInfo(buildPath);
                Debug.Log($"ビルドサイズ: {info.Length / 1024} KB");
            }
            
            // 例: ビルド成功通知を送信（Slack, Discordなど）
            // 実際の実装では、環境変数や設定ファイルからWebhook URLを取得
            
            // ここでAIアシスタントからの提案を反映させる例：
            // ビルド後に自動でテストを実行するなどのフックを追加可能
        }
    }
}
