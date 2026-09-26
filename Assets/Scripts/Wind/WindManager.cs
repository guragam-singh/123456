using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LeafGame
{
    public sealed class WindManager : MonoBehaviour
    {
        public WindPromptSpawner spawner;
        public LeafController leaf;
        public AudioManager audioManager;
        public WindFeedback feedback;
        public LeafCameraController cameraController;
        public InputActionReference leftAction;
        public InputActionReference rightAction;
        [Min(0f)] public float unmatchedPressStrength = 5f;
        [Min(0.1f)] public float unmatchedPressCooldown = 0.45f;
        public bool InputEnabled { get; private set; }
        public event Action<WindResult> Evaluated;
        private float nextUnmatched;

        private void OnEnable() { leftAction?.action.Enable(); rightAction?.action.Enable(); }
        private void OnDisable() { leftAction?.action.Disable(); rightAction?.action.Disable(); StopWind(); }
        public void Begin(WindDifficultyProfile profile)
        {
            InputEnabled = true;
            spawner.Begin(profile);
            if (feedback) feedback.gameObject.SetActive(true);
        }
        public void StopWind()
        {
            InputEnabled = false;
            if (spawner) spawner.Clear();
            if (feedback) feedback.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (!InputEnabled) return;
            var keyboard = Keyboard.current;
            bool left = leftAction ? leftAction.action.WasPressedThisFrame() : keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame;
            bool right = rightAction ? rightAction.action.WasPressedThisFrame() : keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame;
            if (left) Submit(WindDirection.Left, Time.unscaledTime);
            if (right) Submit(WindDirection.Right, Time.unscaledTime);
            for (int i = spawner.Active.Count - 1; i >= 0; i--)
            {
                var prompt = spawner.Active[i];
                if (Time.unscaledTime > prompt.TargetTime + prompt.GoodWindow) Resolve(prompt, WindResult.Miss);
            }
        }

        // Explicit timestamp makes evaluation testable without faking keyboard frames.
        public void Submit(WindDirection direction, float now)
        {
            if (!InputEnabled) return;
            WindPrompt closest = null;
            float distance = float.MaxValue;
            foreach (var prompt in spawner.Active)
            {
                float error = Mathf.Abs(now - prompt.TargetTime);
                if (prompt.Direction == direction && error < distance) { distance = error; closest = prompt; }
            }
            if (closest && distance <= closest.GoodWindow)
                Resolve(closest, WindTimingEvaluator.Evaluate(now - closest.TargetTime, closest.PerfectWindow, closest.GoodWindow));
            else if (now >= nextUnmatched)
            {
                // Early/wrong-key presses cannot erase an approaching arrow or be spammed for safety.
                nextUnmatched = now + unmatchedPressCooldown;
                Apply(WindResult.Miss, direction, unmatchedPressStrength);
            }
        }
        private void Resolve(WindPrompt prompt, WindResult result)
        {
            Apply(result, prompt.Direction, prompt.Strength);
            spawner.Remove(prompt);
        }
        private void Apply(WindResult result, WindDirection direction, float strength)
        {
            leaf.ReceiveWind(result, (int)direction, strength);
            if (audioManager) audioManager.PlaySFX(result == WindResult.Miss ? SFXType.Miss : SFXType.Success);
            if (feedback) feedback.Pulse(result, strength);
            if (cameraController) cameraController.OnWind(result);
            Evaluated?.Invoke(result);
        }
    }
}
