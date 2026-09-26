using UnityEngine;
using UnityEngine.UI;

namespace LeafGame
{
    public sealed class WindFeedback : MonoBehaviour
    {
        public Image timingCircle;
        public RectTransform ringTransform;
        public ParticleSystem windStreaks;
        public Color idleColor = new Color(0.95f, 0.93f, 0.81f, 0.65f);
        public Color successColor = new Color(0.73f, 1f, 0.78f);
        public Color missColor = new Color(0.94f, 0.67f, 0.46f);
        public float recoverySpeed = 5f;
        public float pulseScale = 0.16f;
        private float pulse;
        public void Pulse(WindResult result, float strength)
        {
            timingCircle.color = result == WindResult.Miss ? missColor : successColor;
            pulse = result == WindResult.Perfect ? 1f : 0.55f;
            if (windStreaks) windStreaks.Emit(Mathf.Clamp(Mathf.RoundToInt(strength * 0.2f), 1, 16));
        }
        private void Update()
        {
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * recoverySpeed);
            ringTransform.localScale = Vector3.one * (1f + pulse * pulseScale);
            timingCircle.color = Color.Lerp(timingCircle.color, idleColor, Time.unscaledDeltaTime * recoverySpeed);
        }
    }
}
