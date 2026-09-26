using System;
using UnityEngine;

namespace LeafGame
{
    public sealed class FinalSequenceController : MonoBehaviour
    {
        public FinalSequenceData data;
        public LeafController leaf;
        public WindManager wind;
        public RainManager rain;
        public DialogueManager dialogue;
        public AudioManager audioManager;
        public NarrationManager narration;
        public LeafCameraController cameraController;
        public bool Running { get; private set; }
        public float Elapsed { get; private set; }
        public event Action Landed;
        private Vector3 startPosition;
        private float startAngle;
        private float startTime;

        public void Begin()
        {
            if (Running) return;
            wind.StopWind();
            if (rain) { rain.StopRain(); rain.enabled = false; }
            if (dialogue) { dialogue.Clear(); dialogue.enabled = false; }
            startPosition = leaf.transform.position;
            startAngle = leaf.CurrentAngle;
            startTime = Time.unscaledTime;
            Elapsed = 0; Running = true;
            leaf.BeginScriptedDetachment();
            audioManager.BeginFinal();
            narration.Begin(data);
            cameraController.BeginFollow();
        }
        private void Update()
        {
            if (!Running) return;
            Elapsed = Time.unscaledTime - startTime;
            float fallTime = Mathf.Max(0, Elapsed - data.detachDuration);
            float available = Mathf.Max(0.01f, data.fallDuration - data.detachDuration);
            float t = Mathf.Clamp01(fallTime / available);
            if (Elapsed < data.detachDuration)
            {
                float u = Mathf.SmoothStep(0f, 1f, Elapsed / Mathf.Max(0.01f, data.detachDuration));
                leaf.stemPivot.localRotation = Quaternion.Euler(0,0,Mathf.Lerp(startAngle, data.rotation.Evaluate(0), u));
            }
            else leaf.SetScriptedPose(data.EvaluatePosition(startPosition, t), data.rotation.Evaluate(t), t >= 1f);
            narration.Tick(Elapsed);
            if (Elapsed < data.fallDuration) return;
            CompleteImmediately();
        }
        // Used by the hard runtime guard as well as normal completion.
        public void CompleteImmediately()
        {
            if (!Running) return;
            Running = false;
            leaf.SetScriptedPose(data.landingPosition, data.rotation.Evaluate(1), true);
            narration.StopNarration();
            audioManager.PlaySFX(SFXType.Land);
            audioManager.FadeToSilence(data.musicFadeSeconds);
            cameraController.Freeze();
            Landed?.Invoke();
        }
    }
}
