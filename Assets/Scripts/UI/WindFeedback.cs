using UnityEngine;
using UnityEngine.UI;

namespace LeafGame
{
    public sealed class WindFeedback : MonoBehaviour
    {
        public Image timingCircle;
        public RectTransform ringTransform;
        public ParticleSystem windStreaks;

        [Header("Pixel Wind Visuals (Optional)")]
        [Tooltip("Assign an Animator to play or trigger the wind animation on gusts.")]
        public Animator windAnimator;
        [Tooltip("Assign a GameObject with pixel wind animation to activate when wind is active.")]
        public GameObject windObject;
        [Tooltip("Assign a SpriteRenderer for pixel wind.")]
        public SpriteRenderer windRenderer;
        [Tooltip("Optional trigger name to fire on gusts (leave blank to replay default animation).")]
        public string gustTriggerName = "";

        public Color idleColor = new Color(0.95f, 0.93f, 0.81f, 0.65f);
        public Color successColor = new Color(0.73f, 1f, 0.78f);
        public Color missColor = new Color(0.94f, 0.67f, 0.46f);
        public float recoverySpeed = 5f;
        public float pulseScale = 0.16f;
        private float pulse;

        private void OnEnable()
        {
            if (windObject) windObject.SetActive(true);
            if (windRenderer) windRenderer.enabled = true;
        }

        private void OnDisable()
        {
            if (windObject) windObject.SetActive(false);
            if (windRenderer) windRenderer.enabled = false;
        }

        public void Pulse(WindResult result, float strength)
        {
            timingCircle.color = result == WindResult.Miss ? missColor : successColor;
            pulse = result == WindResult.Perfect ? 1f : 0.55f;
            if (windStreaks) windStreaks.Emit(Mathf.Clamp(Mathf.RoundToInt(strength * 0.2f), 1, 16));

            if (windObject && !windObject.activeSelf) windObject.SetActive(true);
            if (windAnimator)
            {
                if (!string.IsNullOrEmpty(gustTriggerName))
                    windAnimator.SetTrigger(gustTriggerName);
                else
                    windAnimator.Play(0, -1, 0f);
            }
        }
        private void Update()
        {
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * recoverySpeed);
            ringTransform.localScale = Vector3.one * (1f + pulse * pulseScale);
            timingCircle.color = Color.Lerp(timingCircle.color, idleColor, Time.unscaledDeltaTime * recoverySpeed);
        }
    }
}
