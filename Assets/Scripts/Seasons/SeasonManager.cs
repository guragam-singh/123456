using System;
using UnityEngine;

namespace LeafGame
{
    public sealed class SeasonManager : MonoBehaviour
    {
        public SeasonData[] seasons;
        public WindManager wind;
        public TreeEnvironment environment;
        public AudioManager audioManager;
        [Min(0.01f)] public float transitionSeconds = 0.8f;
        public int CurrentIndex { get; private set; } = -1;
        public SeasonData Current => CurrentIndex >= 0 && CurrentIndex < seasons.Length ? seasons[CurrentIndex] : null;
        public float Elapsed { get; private set; }
        public bool Running { get; private set; }
        public event Action<SeasonData> Changed;
        public event Action Completed;
        private float seasonStart;
        private bool challengeStarted;

        public float TotalDuration
        {
            get { float result = 0; if (seasons != null) foreach (var s in seasons) if (s) result += s.duration; return result; }
        }
        public void Begin() { Running = true; Enter(0, Time.unscaledTime); }
        private void Enter(int index, float start)
        {
            CurrentIndex = index; seasonStart = start; Elapsed = 0; challengeStarted = false;
            var data = Current;
            if (!data) { StopSeasons(); Completed?.Invoke(); return; }
            wind.Begin(data.windDifficulty);
            if (environment) environment.Apply(data, transitionSeconds);
            if (audioManager) { audioManager.SetAmbient(data.ambientAudio); audioManager.ChangeMusic(data.music); }
            Changed?.Invoke(data);
        }
        private void Update()
        {
            if (!Running || !Current) return;
            Elapsed = Time.unscaledTime - seasonStart;
            if (!challengeStarted && Current.finalChallengeProfile && Elapsed >= Current.duration - Current.finalChallengeSeconds)
            {
                challengeStarted = true;
                wind.Begin(Current.finalChallengeProfile);
            }
            if (Elapsed < Current.duration) return;
            float nextStart = seasonStart + Current.duration;
            if (CurrentIndex + 1 < seasons.Length) Enter(CurrentIndex + 1, nextStart);
            else { StopSeasons(); Completed?.Invoke(); }
        }
        public void StopSeasons() { Running = false; if (wind) wind.StopWind(); }
    }
}
