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

        [Header("Timing")]
        [Tooltip("Pause duration (in seconds) after UI disappears before the leaf begins to fall.")]
        public float preFallPause = 30f;

        [Header("Ground Detection")]
        [Tooltip("Optional reference to the PLACEHOLDER_Ground object. If null, auto-discovered by name.")]
        public Transform groundTransform;
        [Tooltip("Vertical offset adjustment for ground contact.")]
        public float groundTouchOffset = 0f;

        [Header("Audio")]
        [Tooltip("The dialogue/voice AudioSource child of AudioManager playing the ending song. Auto-discovered if null.")]
        public AudioSource dialogueAudioSource;

        [Header("Background Transition")]
        [Tooltip("Full-screen black CanvasGroup.")]
        public CanvasGroup fade;

        [Tooltip("Seconds after the final sequence begins before the fade starts.")]
        public float fadeStartTime = 21f;

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
        public bool HasDetached { get; private set; }
        public bool HasTouchedGround { get; private set; }
        public bool IsAudioComplete { get; private set; }
        public float TotalAudioDuration { get; private set; }

        public event Action Landed;

        private Vector3 startPosition;
        private float startAngle;
        private float startTime;
        private Vector3 landedPosition;
        private float landedAngle;

        private bool blackScreenEventFired;
        private bool landSfxPlayed;

        public AudioSource ResolveDialogueAudioSource()
        {
            if (dialogueAudioSource != null && dialogueAudioSource.clip != null)
                return dialogueAudioSource;

            if (audioManager != null)
            {
                if (audioManager.voice != null && audioManager.voice.clip != null)
                    return audioManager.voice;

                // Check children of AudioManager for Dialogue or Voice
                foreach (Transform child in audioManager.transform)
                {
                    string n = child.name.ToLowerInvariant();
                    if (n.Contains("dialogue") || n.Contains("voice") || n.Contains("music") || n.Contains("song"))
                    {
                        var src = child.GetComponent<AudioSource>();
                        if (src != null && src.clip != null)
                            return src;
                    }
                }

                // Any AudioSource child with a clip that isn't standard music/rain/ambient/sfx
                foreach (var src in audioManager.GetComponentsInChildren<AudioSource>(true))
                {
                    if (src != audioManager.music && src != audioManager.musicCrossfade &&
                        src != audioManager.ambient && src != audioManager.rain && src != audioManager.sfx &&
                        src.clip != null)
                    {
                        return src;
                    }
                }

                if (audioManager.voice != null)
                    return audioManager.voice;
            }

            var go = GameObject.Find("Dialogue") ?? GameObject.Find("Voice") ?? GameObject.Find("dialogue") ?? GameObject.Find("voice");
            if (go != null)
            {
                var src = go.GetComponent<AudioSource>();
                if (src != null)
                    return src;
            }

            return null;
        }

        public float GetGroundTopY()
        {
            if (groundTransform == null)
            {
                var groundGo = GameObject.Find("PLACEHOLDER_Ground") ?? GameObject.Find("Ground") ?? GameObject.Find("ground");
                if (groundGo != null)
                    groundTransform = groundGo.transform;
            }

            if (groundTransform != null)
            {
                var sprite = groundTransform.GetComponent<SpriteRenderer>();
                if (sprite != null && sprite.sprite != null)
                    return sprite.bounds.max.y + groundTouchOffset;

                var col = groundTransform.GetComponent<Collider2D>();
                if (col != null)
                    return col.bounds.max.y + groundTouchOffset;

                return groundTransform.position.y + (groundTransform.lossyScale.y * 0.5f) + groundTouchOffset;
            }

            return data != null ? data.landingPosition.y : -14.9f;
        }

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
            HasDetached = false;
            HasTouchedGround = false;
            IsAudioComplete = false;
            landSfxPlayed = false;
            blackScreenEventFired = false;

            // -------------------------------------------------------------
            // PLAY DIALOGUE/VOICE AUDIO IMMEDIATELY AS UI DISAPPEARS
            // -------------------------------------------------------------
            dialogueAudioSource = ResolveDialogueAudioSource();
            if (dialogueAudioSource != null && dialogueAudioSource.clip != null)
            {
                TotalAudioDuration = dialogueAudioSource.clip.length;
                dialogueAudioSource.loop = false;
                dialogueAudioSource.volume = 1f;
                dialogueAudioSource.mute = false;
                dialogueAudioSource.Play();

                // Stop competing background music so dialogue song is crystal clear
                if (audioManager != null)
                {
                    audioManager.BeginFinal(false);
                }
            }
            else
            {
                TotalAudioDuration = preFallPause + (data != null ? data.fallDuration : 58f);
                if (audioManager != null)
                    audioManager.BeginFinal(true);
            }

            // Note: Scripted detachment will begin AFTER the 30-second pause.
            narration.Begin(data);
            cameraController.BeginFollow();
        }

        private void Update()
        {
            if (!Running)
                return;

            Elapsed = Time.unscaledTime - startTime;

            // -------------------------------------------------------------
            // 1. 30-SECOND PAUSE BEFORE FALLING
            // Right after control UI disappears, leaf holds stationary on branch
            // -------------------------------------------------------------
            if (Elapsed < preFallPause)
            {
                leaf.transform.position = startPosition;
                if (leaf.stemPivot != null)
                {
                    leaf.stemPivot.localRotation = Quaternion.Euler(0f, 0f, startAngle);
                }

                narration.Tick(Elapsed);
                return;
            }

            // -------------------------------------------------------------
            // 2. DETACHMENT AND EXTENDED FALL ANIMATION
            // Leaf detachment begins at Elapsed == preFallPause.
            // Fall animation is extended across the remaining audio duration.
            // -------------------------------------------------------------
            if (!HasDetached)
            {
                HasDetached = true;
                leaf.BeginScriptedDetachment();
            }

            float fallElapsed = Elapsed - preFallPause;
            float availableFallDuration = Mathf.Max(5f, TotalAudioDuration - preFallPause);
            float t = Mathf.Clamp01(fallElapsed / availableFallDuration);

            if (!HasTouchedGround)
            {
                if (fallElapsed < data.detachDuration)
                {
                    float u = Mathf.SmoothStep(
                        0f,
                        1f,
                        fallElapsed / Mathf.Max(0.01f, data.detachDuration)
                    );

                    if (leaf.stemPivot != null)
                    {
                        leaf.stemPivot.localRotation = Quaternion.Euler(
                            0f,
                            0f,
                            Mathf.Lerp(startAngle, data.rotation.Evaluate(0), u)
                        );
                    }
                }
                else
                {
                    Vector3 desiredPos = data.EvaluatePosition(startPosition, t);
                    float desiredRot = data.rotation.Evaluate(t);
                    float groundTopY = GetGroundTopY();

                    // Stop animation as soon as the leaf touches PLACEHOLDER_Ground
                    if (desiredPos.y <= groundTopY || t >= 1f)
                    {
                        HasTouchedGround = true;
                        landedPosition = new Vector3(desiredPos.x, Mathf.Max(desiredPos.y, groundTopY), desiredPos.z);
                        landedAngle = desiredRot;

                        leaf.SetScriptedPose(landedPosition, landedAngle, true);

                        // Halt animator immediately on ground touch
                        if (leaf.animator != null)
                            leaf.animator.speed = 0f;

                        if (!landSfxPlayed)
                        {
                            landSfxPlayed = true;
                            if (audioManager != null)
                                audioManager.PlaySFX(SFXType.Land);
                        }

                        cameraController.Freeze();
                    }
                    else
                    {
                        leaf.SetScriptedPose(desiredPos, desiredRot, false);
                    }
                }
            }
            else
            {
                // Leaf has touched ground: hold its landed pose and keep animation stopped
                leaf.SetScriptedPose(landedPosition, landedAngle, true);
                if (leaf.animator != null)
                    leaf.animator.speed = 0f;
            }

            narration.Tick(Elapsed);

            // -------------------------------------------------------------
            // 3. BACKGROUND TRANSITION
            // -------------------------------------------------------------
            UpdateBackgroundTransition();

            // -------------------------------------------------------------
            // 4. AUDIO COMPLETION CHECK
            // Keep the audio playing even if the leaf touches ground!
            // Only finish once the entire audio clip completes.
            // -------------------------------------------------------------
            bool audioFinished = false;
            if (dialogueAudioSource != null && dialogueAudioSource.clip != null)
            {
                if (Elapsed >= TotalAudioDuration || (!dialogueAudioSource.isPlaying && Elapsed > preFallPause + 3f))
                {
                    audioFinished = true;
                }
            }
            else
            {
                if (Elapsed >= preFallPause + availableFallDuration)
                {
                    audioFinished = true;
                }
            }

            if (!audioFinished)
                return;

            CompleteImmediately();
        }

        private void UpdateBackgroundTransition()
        {
            if (!fade)
                return;

            float effectiveFadeStart = (fadeStartTime <= preFallPause) ? (preFallPause + 3f) : fadeStartTime;
            float transitionElapsed = Elapsed - effectiveFadeStart;

            if (transitionElapsed < 0f)
                return;

            // Fade to black
            if (transitionElapsed < fadeInDuration)
            {
                float t = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, fadeInDuration));
                fade.alpha = Mathf.SmoothStep(0f, 1f, t);
                return;
            }

            // Black screen hold
            float blackElapsed = transitionElapsed - fadeInDuration;
            if (blackElapsed < blackDuration)
            {
                fade.alpha = 1f;
                return;
            }

            // Black screen event
            if (!blackScreenEventFired)
            {
                blackScreenEventFired = true;
                onBlackScreenEnd?.Invoke();
            }

            // Fade back out
            float fadeOutElapsed = blackElapsed - blackDuration;
            float fadeOutT = Mathf.Clamp01(fadeOutElapsed / Mathf.Max(0.01f, fadeOutDuration));
            fade.alpha = 1f - Mathf.SmoothStep(0f, 1f, fadeOutT);
        }

        public void CompleteImmediately()
        {
            if (!Running)
                return;

            Running = false;
            IsAudioComplete = true;

            if (fade)
                fade.alpha = 0f;

            Vector3 finalPos = data != null ? data.landingPosition : (HasTouchedGround ? landedPosition : data.landingPosition);
            float finalAngle = data != null ? data.rotation.Evaluate(1) : landedAngle;

            leaf.SetScriptedPose(finalPos, finalAngle, true);

            if (leaf.animator != null)
                leaf.animator.speed = 0f;

            narration.StopNarration();

            if (!landSfxPlayed)
            {
                landSfxPlayed = true;
                if (audioManager != null)
                    audioManager.PlaySFX(SFXType.Land);
            }

            if (audioManager != null)
            {
                audioManager.FadeToSilence(data != null ? data.musicFadeSeconds : 4f);
            }

            cameraController.Freeze();

            Landed?.Invoke();
        }
    }
}