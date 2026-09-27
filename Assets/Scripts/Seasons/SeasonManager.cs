using System;
using UnityEngine;

namespace LeafGame
{
    public sealed class SeasonManager : MonoBehaviour
    {
        [Header("Seasons")]
        public SeasonData[] seasons;

        [Header("Systems")]
        public WindManager wind;
        public TreeEnvironment environment;
        public AudioManager audioManager;

        [Header("Normal Game Music")]
        [Tooltip("Single looping music track used throughout Spring, Summer and Autumn.")]
        public AudioClip cozyMusic;

        [Min(0.01f)]
        public float transitionSeconds = 0.8f;

        public int CurrentIndex { get; private set; } = -1;

        public SeasonData Current =>
            CurrentIndex >= 0 && CurrentIndex < seasons.Length
                ? seasons[CurrentIndex]
                : null;

        public float Elapsed { get; private set; }
        public bool Running { get; private set; }

        public event Action<SeasonData> Changed;
        public event Action Completed;

        private float seasonStart;
        private bool challengeStarted;
        private bool cozyMusicStarted;

        public float TotalDuration
        {
            get
            {
                float result = 0f;

                if (seasons != null)
                {
                    foreach (var s in seasons)
                    {
                        if (s)
                            result += s.duration;
                    }
                }

                return result;
            }
        }

        public void Begin()
        {
            Running = true;
            cozyMusicStarted = false;

            Enter(0, Time.unscaledTime);
        }

        private void Enter(int index, float start)
        {
            CurrentIndex = index;
            seasonStart = start;
            Elapsed = 0f;
            challengeStarted = false;

            var data = Current;

            if (!data)
            {
                StopSeasons();
                Completed?.Invoke();
                return;
            }

            // Start the wind for this season.
            if (wind)
                wind.Begin(data.windDifficulty);

            // Apply the seasonal environment.
            if (environment)
                environment.Apply(data, transitionSeconds);

            // Keep seasonal ambient audio.
            if (audioManager)
            {
                audioManager.SetAmbient(data.ambientAudio);

                // Use ONE music track for the entire normal game.
                // Only start it once so changing seasons doesn't restart/fade it.
                if (!cozyMusicStarted && cozyMusic)
                {
                    cozyMusicStarted = true;
                    audioManager.ChangeMusic(cozyMusic);
                }
            }

            Changed?.Invoke(data);
        }

        private void Update()
        {
            if (!Running || !Current)
                return;

            Elapsed = Time.unscaledTime - seasonStart;

            // Start the final wind challenge near the end of the season.
            if (!challengeStarted &&
                Current.finalChallengeProfile &&
                Elapsed >= Current.duration - Current.finalChallengeSeconds)
            {
                challengeStarted = true;
                wind.Begin(Current.finalChallengeProfile);
            }

            if (Elapsed < Current.duration)
                return;

            float nextStart = seasonStart + Current.duration;

            if (CurrentIndex + 1 < seasons.Length)
            {
                Enter(CurrentIndex + 1, nextStart);
            }
            else
            {
                StopSeasons();
                Completed?.Invoke();
            }
        }

        public void StopSeasons()
        {
            Running = false;

            if (wind)
                wind.StopWind();
        }
    }
}