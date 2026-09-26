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
        }
        public void StopRain()
        {
            intensity = 0;
            if (leaf) leaf.RainWeight = 0;
            if (audioManager) audioManager.SetRain(0);
            if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnDisable() { StopRain(); }
    }
}
