using UnityEngine;
using UnityEngine.UI;

namespace LeafGame
{
    public sealed class WindPrompt : MonoBehaviour
    {
        [Header("References")]
        public RectTransform rect;
        public Text glyph;
        public Image halo;

        [Header("Colors")]
        public Color normalColor = new Color(0.94f, 0.91f, 0.77f);
        public Color gustColor = new Color(1f, 0.69f, 0.36f);

        [Header("Position")]
        [Tooltip("Vertical position of the arrow lane relative to its parent.")]
        public float verticalOffset = 0f;

        public WindDirection Direction { get; private set; }
        public float TargetTime { get; private set; }
        public float Strength { get; private set; }
        public float PerfectWindow { get; private set; }
        public float GoodWindow { get; private set; }

        private float speed;

        public void Initialize(
            WindDirection direction,
            float targetTime,
            WindDifficultyProfile profile,
            bool gust)
        {
            Direction = direction;
            TargetTime = targetTime;
            speed = profile.arrowSpeed;
            Strength = profile.windStrength * (gust ? profile.gustStrength : 1f);
            PerfectWindow = profile.perfectWindow;
            GoodWindow = profile.goodWindow;

            glyph.text = direction == WindDirection.Left ? "\u2190" : "\u2192";
            glyph.color = gust ? gustColor : normalColor;

            if (halo)
            {
                halo.color = new Color(
                    glyph.color.r,
                    glyph.color.g,
                    glyph.color.b,
                    0.12f
                );
            }

            Render(Time.unscaledTime);
        }

        public void Render(float now)
        {
            // Arrows move horizontally toward the centre.
            // verticalOffset controls the entire arrow lane's local Y.
            float x = (int)Direction * (TargetTime - now) * speed;

            rect.anchoredPosition = new Vector2(
                x,
                verticalOffset
            );
        }
    }
}