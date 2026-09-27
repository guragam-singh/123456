using System;
using UnityEngine;
using UnityEngine.Events;

namespace LeafGame
{
    public sealed class FinalSequenceController : MonoBehaviour
    {
        [Header("References")]
        public FinalSequenceData data;
        public LeafController leaf;
        public WindManager wind;
        public RainManager rain;
        public DialogueManager dialogue;
        public AudioManager audioManager;
        public NarrationManager narration;
        public LeafCameraController cameraController;

        [Header("Background Transition")]
        [Tooltip("Full-screen black CanvasGroup.")]
        public CanvasGroup fade;

        [Tooltip("Seconds after the final sequence begins before the fade starts.")]
        public float fadeStartTime = 15f;

        [Tooltip("Time taken to fade completely to black.")]
        public float fadeInDuration = 1f;

        [Tooltip("How long the screen remains completely black.")]
        public float blackDuration = 0.5f;

        [Tooltip("Time taken to fade back into the second background.")]
        public float fadeOutDuration = 1f;

        [Header("When Black Screen Ends")]
        [Tooltip("Event fired once the black-screen hold finishes. Use this to enable the second background.")]
        public UnityEvent onBlackScreenEnd;

        public bool Running { get; private set; }
        public float Elapsed { get; private set; }

        public event Action Landed;

        private Vector3 startPosition;
        private float startAngle;
        private float startTime;

        private bool blackScreenEventFired;

        public void Begin()
        {
            if (Running)
                return;

            wind.StopWind();

            if (rain)
            {
                rain.StopRain();
                rain.enabled = false;
            }

            if (dialogue)
            {
                dialogue.Clear();
                dialogue.enabled = false;
            }

            if (fade)
                fade.alpha = 0f;

            startPosition = leaf.transform.position;
            startAngle = leaf.CurrentAngle;
            startTime = Time.unscaledTime;

            Elapsed = 0f;
            Running = true;

            blackScreenEventFired = false;

            leaf.BeginScriptedDetachment();

            audioManager.BeginFinal();
            narration.Begin(data);
            cameraController.BeginFollow();
        }

        private void Update()
        {
            if (!Running)
                return;

            Elapsed = Time.unscaledTime - startTime;

            // --------------------------------------------
            // EXISTING LEAF FALL
            // --------------------------------------------

            float fallTime = Mathf.Max(
                0f,
                Elapsed - data.detachDuration
            );

            float available = Mathf.Max(
                0.01f,
                data.fallDuration - data.detachDuration
            );

            float t = Mathf.Clamp01(
                fallTime / available
            );

            if (Elapsed < data.detachDuration)
            {
                float u = Mathf.SmoothStep(
                    0f,
                    1f,
                    Elapsed /
                    Mathf.Max(0.01f, data.detachDuration)
                );

                leaf.stemPivot.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Lerp(
                            startAngle,
                            data.rotation.Evaluate(0),
                            u
                        )
                    );
            }
            else
            {
                leaf.SetScriptedPose(
                    data.EvaluatePosition(
                        startPosition,
                        t
                    ),
                    data.rotation.Evaluate(t),
                    t >= 1f
                );
            }

            narration.Tick(Elapsed);

            // --------------------------------------------
            // BACKGROUND TRANSITION
            // --------------------------------------------

            UpdateBackgroundTransition();

            // --------------------------------------------
            // ORIGINAL END
            // --------------------------------------------

            if (Elapsed < data.fallDuration)
                return;

            CompleteImmediately();
        }

        private void UpdateBackgroundTransition()
        {
            if (!fade)
                return;

            float transitionElapsed =
                Elapsed - fadeStartTime;

            if (transitionElapsed < 0f)
                return;

            // --------------------------------------------
            // FADE TO BLACK
            // --------------------------------------------

            if (transitionElapsed < fadeInDuration)
            {
                float t = Mathf.Clamp01(
                    transitionElapsed /
                    Mathf.Max(0.01f, fadeInDuration)
                );

                fade.alpha = Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

                return;
            }

            // --------------------------------------------
            // BLACK SCREEN
            // --------------------------------------------

            float blackElapsed =
                transitionElapsed - fadeInDuration;

            if (blackElapsed < blackDuration)
            {
                fade.alpha = 1f;
                return;
            }

            // --------------------------------------------
            // BLACK SCREEN EVENT
            // --------------------------------------------

            if (!blackScreenEventFired)
            {
                blackScreenEventFired = true;

                // This is where you enable the second
                // background through the Unity Inspector.
                onBlackScreenEnd?.Invoke();
            }

            // --------------------------------------------
            // FADE BACK OUT
            // --------------------------------------------

            float fadeOutElapsed =
                blackElapsed - blackDuration;

            float fadeOutT = Mathf.Clamp01(
                fadeOutElapsed /
                Mathf.Max(0.01f, fadeOutDuration)
            );

            fade.alpha = 1f - Mathf.SmoothStep(
                0f,
                1f,
                fadeOutT
            );
        }

        // Used by the hard runtime guard as well as normal completion.
        public void CompleteImmediately()
        {
            if (!Running)
                return;

            Running = false;

            if (fade)
                fade.alpha = 0f;

            leaf.SetScriptedPose(
                data.landingPosition,
                data.rotation.Evaluate(1),
                true
            );

            narration.StopNarration();

            audioManager.PlaySFX(
                SFXType.Land
            );

            audioManager.FadeToSilence(
                data.musicFadeSeconds
            );

            cameraController.Freeze();

            Landed?.Invoke();
        }
    }
}