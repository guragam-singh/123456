using UnityEngine;

namespace LeafGame
{
    public sealed class RainManager : MonoBehaviour
    {
        public SeasonManager seasons;
        public LeafController leaf;
        public AudioManager audioManager;
        public ParticleSystem particles;
        public float particlesPerSecond = 60f;
        public float intensityBlendSpeed = 0.25f;

        [Header("Pixel Rain Visuals (Optional)")]
        [Tooltip("Assign an animated SpriteRenderer for pixel rain to fade its alpha with intensity.")]
        public SpriteRenderer rainRenderer;
        [Tooltip("Assign a GameObject with pixel rain animation to activate/deactivate when raining.")]
        public GameObject rainObject;
        [Tooltip("Assign a CanvasGroup if pixel rain is a UI Image overlay.")]
        public CanvasGroup rainCanvasGroup;

        private float intensity;
        private void Update()
        {
            var season = seasons.Current;
            float target = seasons.Running && season && seasons.Elapsed >= season.rainStartDelay ? season.rainIntensity : 0f;
            intensity = Mathf.MoveTowards(intensity, target, Time.unscaledDeltaTime * intensityBlendSpeed);
            if (leaf)
            {
                leaf.RainWeight = seasons.Running ? intensity : 0f;
                if (seasons.Running && season) leaf.AddInstability(season.rainInstabilityPerSecond * intensity * Time.unscaledDeltaTime);
            }
            if (particles) { var emission = particles.emission; emission.rateOverTime = intensity * particlesPerSecond; }
            if (audioManager) audioManager.SetRain(intensity);

            bool isRaining = intensity > 0.01f;
            if (rainRenderer)
            {
                var c = rainRenderer.color;
                c.a = intensity;
                rainRenderer.color = c;
                rainRenderer.enabled = isRaining;
            }
            if (rainCanvasGroup)
            {
                rainCanvasGroup.alpha = intensity;
                rainCanvasGroup.gameObject.SetActive(isRaining);
            }
            if (rainObject)
            {
                rainObject.SetActive(isRaining);
            }
        }
        public void StopRain()
        {
            intensity = 0;
            if (leaf) leaf.RainWeight = 0;
            if (audioManager) audioManager.SetRain(0);
            if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (rainRenderer) { var c = rainRenderer.color; c.a = 0f; rainRenderer.color = c; rainRenderer.enabled = false; }
            if (rainCanvasGroup) { rainCanvasGroup.alpha = 0f; rainCanvasGroup.gameObject.SetActive(false); }
            if (rainObject) rainObject.SetActive(false);
        }
        private void OnDisable() { StopRain(); }
    }
}
