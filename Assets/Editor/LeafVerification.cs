using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LeafGame.Editor
{
    // Editor-only integration probe. Never becomes a player-build component.
    [InitializeOnLoad]
    public static class LeafVerification
    {
        private static GameFlow flow;
        private static readonly List<string> observations = new List<string>();
        private static GameState previous;
        private static bool started;
        private static bool done;
        private static bool sawRain;
        private static bool sawBackground;
        private static int results;
        private static int successes;
        private static double wallStart;
        private static double lastCheck;
        private static bool PlayingTest => SessionState.GetBool("LEAF.Verify", false);
        static LeafVerification() { EditorApplication.update += Tick; }

        public static void StartRun(bool perfectInputs = false)
        {
            if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
            ValidateAssets();
            SessionState.SetBool("LEAF.Verify", true);
            SessionState.SetBool("LEAF.Perfect", perfectInputs);
            EditorApplication.isPlaying = true;
        }
        public static void ValidateAssets()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
            Require(game, "Main scene has GameFlow");
            Require(game.PlannedDuration >= 240f && game.PlannedDuration <= 300f, "Session within 240..300 seconds");
            Require(game.seasons.seasons.Length == 3, "Three seasons");
            foreach (var s in game.seasons.seasons)
            {
                Require(s && s.windDifficulty && s.duration > 0, "Valid season " + s.name);
                foreach (var beat in s.dialogueEvents)
                {
                    float duration = 0; foreach (var line in beat.dialogue.lines) duration += line.seconds;
                    Require(duration <= 10, "Short dialogue " + beat.dialogue.name);
                    Require(beat.atSeconds + duration < s.duration, "Dialogue fits season");
                }
            }
            var p = game.seasons.seasons[0].windDifficulty;
            Require(WindTimingEvaluator.Evaluate(p.perfectWindow, p.perfectWindow, p.goodWindow) == WindResult.Perfect, "Perfect inclusive boundary");
            Require(WindTimingEvaluator.Evaluate(-p.goodWindow, p.perfectWindow, p.goodWindow) == WindResult.Good, "Good negative inclusive boundary");
            Require(WindTimingEvaluator.Evaluate(p.goodWindow + 0.001f, p.perfectWindow, p.goodWindow) == WindResult.Miss, "Miss outside window");
            var data = game.finalSequence.data;
            var start = game.finalSequence.leaf.transform.position;
            Require(Vector3.Distance(data.EvaluatePosition(start, 0), start) < 0.001f, "Fall begins at stem");
            Require(Vector3.Distance(data.EvaluatePosition(start, 1), data.landingPosition) < 0.001f, "Fall ends at exact landing");
            float lastY = start.y;
            for (int i=0; i<=100; i++) { float y=data.EvaluatePosition(start,i/100f).y; Require(y<=lastY+0.001f,"Monotonic descent"); lastY=y; }
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "No missing script on " + t.name);
            Require(game.finalSequence.leaf.animator.runtimeAnimatorController, "Animator connected");
            Require(game.seasons.wind.leftAction && game.seasons.wind.rightAction, "Keyboard input actions connected");
            Require(EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes[0].path == "Assets/Scenes/Main.unity", "Main build scene");
            Debug.Log("LEAF asset/timing/path validation PASSED. Planned " + game.PlannedDuration + " seconds.");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("LEAF VERIFY: " + message); }
        private static void Tick()
        {
            if (!PlayingTest || !EditorApplication.isPlaying || EditorApplication.isPaused || done) return;
            // Headless/tool-driven Editor sessions may stop repainting the Game View.
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if (!started)
                {
                    flow = UnityEngine.Object.FindFirstObjectByType<GameFlow>();
                    if (!flow || flow.State == GameState.Intro) return;
                    observations.Clear(); started=true; wallStart=EditorApplication.timeSinceStartup;
                    previous=flow.State; results=0; successes=0; sawRain=false; sawBackground=false;
                    observations.Add("Start " + flow.State + " at " + flow.Elapsed.ToString("F3") + "s");
                    flow.seasons.wind.Evaluated += result => { results++; if(result!=WindResult.Miss)successes++; };
                }
                if (flow.State != previous)
                {
                    observations.Add(flow.State + " at " + flow.Elapsed.ToString("F3") + "s; instability=" + flow.finalSequence.leaf.Instability.ToString("F3"));
                    previous=flow.State;
                }
                if (SessionState.GetBool("LEAF.Perfect", false) && flow.seasons.wind.InputEnabled)
                {
                    var wind=flow.seasons.wind;
                    for(int i=wind.spawner.Active.Count-1;i>=0;i--)
                    {
                        var prompt=wind.spawner.Active[i];
                        if(Time.unscaledTime>=prompt.TargetTime)wind.Submit(prompt.Direction,Time.unscaledTime);
                    }
                }
                if (flow.State == GameState.Summer && flow.finalSequence.leaf.RainWeight > 0.1f) sawRain=true;
                if (flow.State == GameState.Autumn && flow.backgroundLeaves.GetComponentsInChildren<DecorativeLeaf>().Length>0)sawBackground=true;
                Require(float.IsFinite(flow.finalSequence.leaf.CurrentAngle), "Finite leaf rotation");
                if (flow.seasons.Running) Require(flow.finalSequence.leaf.State == LeafState.Attached || flow.finalSequence.leaf.State == LeafState.Unstable,"Leaf stays attached through seasons");
                if (flow.State == GameState.FinalSequence || flow.State == GameState.Ending)
                {
                    Require(!flow.seasons.wind.InputEnabled,"Input locked at final");
                    Require(flow.seasons.wind.spawner.Active.Count==0,"All wind prompts cleared");
                    Require(flow.finalSequence.leaf.RainWeight==0,"Rain modifier cleared");
                }
                if (EditorApplication.timeSinceStartup-lastCheck>25)
                {
                    lastCheck=EditorApplication.timeSinceStartup;
                    Debug.Log("LEAF timed verification: " + flow.State + " " + flow.Elapsed.ToString("F1") + "s");
                }
                if (!flow.ui.MenuVisible) return;
                Require(sawRain,"Summer rain observed"); Require(sawBackground,"Autumn background leaves observed");
                Require(results>20,"Repeated wind opportunities");
                Require(flow.finalSequence.leaf.State==LeafState.Landed,"Leaf landed");
                Require(Vector3.Distance(flow.finalSequence.leaf.transform.position,flow.finalSequence.data.landingPosition)<0.001f,"Exact landing pose");
                Require(flow.EndingScreenAt>=240 && flow.EndingScreenAt<=300,"Ending in 4..5 minutes");
                observations.Add("PASS ending="+flow.EndingScreenAt.ToString("F3")+"s wall="+(EditorApplication.timeSinceStartup-wallStart).ToString("F3")+"s, results="+results+", successes="+successes+", rain="+sawRain+", background="+sawBackground);
                Directory.CreateDirectory("Temp/LeafVerification");
                string mode=SessionState.GetBool("LEAF.Perfect",false)?"perfect":"all-misses";
                File.WriteAllLines("Temp/LeafVerification/"+mode+".txt",observations);
                Debug.Log("LEAF FULL PLAYTHROUGH PASS\n"+string.Join("\n",observations));
                done=true; SessionState.SetBool("LEAF.Verify",false);
            }
            catch(Exception e)
            {
                done=true; SessionState.SetBool("LEAF.Verify",false); Debug.LogException(e);
            }
        }
        public static void Capture(string name)
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var window=EditorWindow.GetWindow(type); window.Repaint();
            var field=type.GetField("m_RenderTexture",BindingFlags.Instance|BindingFlags.NonPublic);
            var rt=field?.GetValue(window) as RenderTexture;
            if(!rt) throw new Exception("Game View render texture not ready");
            var old=RenderTexture.active;
            var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=rt; texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); texture.Apply();
                if(SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels=texture.GetPixels(); var row=new Color[texture.width];
                    for(int y=0;y<texture.height/2;y++)
                    {
                        Array.Copy(pixels,y*texture.width,row,0,texture.width);
                        Array.Copy(pixels,(texture.height-1-y)*texture.width,pixels,y*texture.width,texture.width);
                        Array.Copy(row,0,pixels,(texture.height-1-y)*texture.width,texture.width);
                    }
                    texture.SetPixels(pixels); texture.Apply();
                }
                Directory.CreateDirectory("Temp/LeafVerification"); File.WriteAllBytes("Temp/LeafVerification/"+name+".png",texture.EncodeToPNG());
            }
            finally { RenderTexture.active=old; UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
