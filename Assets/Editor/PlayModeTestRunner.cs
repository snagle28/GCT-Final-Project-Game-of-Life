using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 10); // Increase wait for scene load
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 15.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 50;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            switch (state)
            {
                case "WaitingForCompile":
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;
                case "EnteringPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;
                case "InPlayMode":
                    if (EditorApplication.isPlaying) EditorApplication.update += WaitFramesThenRun;
                    break;
                case "Done":
                    EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;
        private static GameOfLifeManager _manager;
        private static int _initialAliveCount = 0;
        private static List<int> _aliveHistory = new List<int>();

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;
            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try { Setup(); }
                catch (System.Exception e) { FinishTest(true, "Setup error: " + e.Message); }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;
            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut) FinishTest(timedOut && !complete, timedOut ? "Timeout" : null);
            }
            catch (System.Exception e) { FinishTest(true, "Tick error: " + e.Message); }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;
            SessionState.SetString(ResultKey, GetResult(isError, errorMessage));
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
                AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
        }

        [System.Serializable]
        private class TestResult { public bool success; public string error; public string[] logs; public int finalAlive; public bool wasChanging; }

        private static void Setup()
        {
            SceneManager.LoadScene("Soundscape1");
            Debug.Log("[Test] Scene loading requested");
        }

        private static bool Tick(float elapsed)
        {
            if (_manager == null)
            {
                _manager = Object.FindFirstObjectByType<GameOfLifeManager>();
                if (_manager != null)
                {
                    Debug.Log("[Test] Manager found. Placing pattern.");
                    // Use reflection to unpause if private, but let's assume we can trigger it via space
                    // Or just use the public property if it exists
                    var prop = _manager.GetType().GetField("isPaused", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    prop?.SetValue(_manager, false);
                    
                    // Place a glider at (10, 10)
                    int[][] glider = { new int[] {0,1,0}, new int[] {0,0,1}, new int[] {1,1,1} };
                    _manager.SendMessage("SetPattern", new object[] { glider, 10, 10 });
                }
                return false;
            }

            if (elapsed % 0.5f < 0.1f) // Log every 0.5s
            {
                int current = _manager.AliveCount;
                _aliveHistory.Add(current);
                Debug.Log("[Test] Elapsed: " + elapsed + "s, Alive: " + current);
            }

            return elapsed >= 8.0f;
        }

        private static string GetResult(bool isError, string errorMessage)
        {
            bool changing = false;
            for (int i = 1; i < _aliveHistory.Count; i++) if (_aliveHistory[i] != _aliveHistory[i-1]) changing = true;

            var res = new TestResult {
                success = !isError && _manager != null && _manager.AliveCount > 0 && changing,
                error = errorMessage,
                logs = _capturedLogs.ToArray(),
                finalAlive = _manager != null ? _manager.AliveCount : -1,
                wasChanging = changing
            };
            return JsonUtility.ToJson(res);
        }
    }
}
