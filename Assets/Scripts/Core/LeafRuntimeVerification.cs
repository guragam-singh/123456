#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LeafGame
{
    // Opt-in development-player probe. It is absent from release builds.
    public sealed class LeafRuntimeVerification : MonoBehaviour
    {
        private GameFlow flow;
        private readonly List<string> observations = new List<string>();
        private GameState previous = GameState.Intro;
        private bool rain, decoration, done, perfect;
        private int results, successes;
        private string output;
        private float nextCapture;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-leaf-verify") < 0) return;
            new GameObject("Development Verification").AddComponent<LeafRuntimeVerification>();
        }
        private void Start()
        {
            flow = FindFirstObjectByType<GameFlow>();
            perfect = Array.IndexOf(Environment.GetCommandLineArgs(), "-leaf-perfect") >= 0;
            output = Path.Combine(Application.persistentDataPath, perfect ? "leaf-perfect" : "leaf-all-misses");
            Directory.CreateDirectory(output);
            flow.seasons.wind.Evaluated += result => { results++; if (result != WindResult.Miss) successes++; };
            nextCapture = 10;
            Debug.Log("LEAF verification player started. output=" + output);
        }
        private void Update()
        {
            if (done || !flow) return;
            try
            {
                if (flow.State != previous)
                {
                    previous = flow.State;
                    observations.Add(flow.State + " at " + flow.Elapsed.ToString("F3") + "s");
                    Debug.Log(observations[observations.Count - 1]);
                    nextCapture = flow.Elapsed + 10;
                }
                var wind = flow.seasons.wind;
                if (perfect && wind.InputEnabled)
                {
                    for (int i = wind.spawner.Active.Count - 1; i >= 0; i--)
                    {
                        var prompt = wind.spawner.Active[i];
                        if (Time.unscaledTime >= prompt.TargetTime) wind.Submit(prompt.Direction, Time.unscaledTime);
                    }
                }
                if (flow.State == GameState.Summer && flow.finalSequence.leaf.RainWeight > 0.1f) rain = true;
                if (flow.State == GameState.Autumn && flow.backgroundLeaves.GetComponentsInChildren<DecorativeLeaf>().Length > 0) decoration = true;
                Require(float.IsFinite(flow.finalSequence.leaf.CurrentAngle), "Finite rotation");
                if (flow.seasons.Running) Require(flow.finalSequence.leaf.State == LeafState.Attached || flow.finalSequence.leaf.State == LeafState.Unstable, "Narrative tether holds");
                if (flow.State == GameState.FinalSequence || flow.State == GameState.Ending)
                {
                    Require(!wind.InputEnabled && wind.spawner.Active.Count == 0, "Input and prompts disabled");
                    Require(flow.finalSequence.leaf.RainWeight == 0, "Rain cleared");
                }
                if (flow.Elapsed >= nextCapture)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, flow.State + ".png"));
                    nextCapture = float.MaxValue;
                }
                if (!flow.ui.MenuVisible) return;
                Require(rain && decoration, "Seasonal atmosphere observed");
                Require(results > 20, "Wind results observed");
                if (perfect) Require(successes > 20, "Successful inputs observed");
                Require(flow.finalSequence.leaf.State == LeafState.Landed, "Landed state");
                Require(Vector3.Distance(flow.finalSequence.leaf.transform.position, flow.finalSequence.data.landingPosition) < 0.001f, "Exact landing");
                Require(flow.EndingScreenAt >= 240 && flow.EndingScreenAt <= 300, "Runtime ceiling");
                observations.Add("PASS ending=" + flow.EndingScreenAt.ToString("F3") + "s, results=" + results + ", successes=" + successes + ", rain=" + rain + ", decoration=" + decoration + ", frames=" + Time.frameCount);
                File.WriteAllLines(Path.Combine(output, "results.txt"), observations);
                Debug.Log("LEAF FULL PLAYTHROUGH PASS\n" + string.Join("\n", observations));
                done = true;
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "EndingMenu.png"));
                Invoke(nameof(Finish), 1f);
            }
            catch (Exception e)
            {
                done = true; Debug.LogException(e); File.WriteAllText(Path.Combine(output, "FAILED.txt"), e.ToString()); Application.Quit(2);
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("LEAF VERIFY: " + message); }
        private void Finish() { Application.Quit(0); }
    }
}
#endif
