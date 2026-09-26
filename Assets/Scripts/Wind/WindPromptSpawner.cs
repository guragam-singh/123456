using System.Collections.Generic;
using UnityEngine;

namespace LeafGame
{
    public sealed class WindPromptSpawner : MonoBehaviour
    {
        public WindPrompt promptPrefab;
        public RectTransform promptParent;
        [Min(100f)] public float approachDistance = 330f;
        [Min(0f)] public float firstPromptDelay = 2f;
        public readonly List<WindPrompt> Active = new List<WindPrompt>();
        public WindDifficultyProfile Profile { get; private set; }
        public bool Running { get; private set; }
        private float nextSpawn;
        private bool sequencePending;
        private WindDirection previousDirection;

        public void Begin(WindDifficultyProfile profile)
        {
            Clear();
            Profile = profile;
            Running = profile != null;
            nextSpawn = Time.unscaledTime + firstPromptDelay;
        }

        private void Update()
        {
            if (!Running || !Profile) return;
            float now = Time.unscaledTime;
            foreach (var prompt in Active) prompt.Render(now);
            if (now < nextSpawn) return;
            if (Active.Count >= Profile.maximumSimultaneousPrompts) return;
            var direction = sequencePending
                ? (previousDirection == WindDirection.Left ? WindDirection.Right : WindDirection.Left)
                : (Random.value < 0.5f ? WindDirection.Left : WindDirection.Right);
            var spawned = Instantiate(promptPrefab, promptParent);
            spawned.Initialize(direction, now + approachDistance / Profile.arrowSpeed, Profile, Random.value < Profile.gustProbability);
            Active.Add(spawned);
            previousDirection = direction;
            bool nextInSequence = !sequencePending && Profile.maximumSimultaneousPrompts > 1 && Random.value < Profile.sequenceProbability;
            sequencePending = nextInSequence;
            nextSpawn = now + (nextInSequence ? Mathf.Max(Profile.sequenceSpacing, 2f * Profile.goodWindow + 0.1f) : Profile.spawnInterval);
        }

        public void Remove(WindPrompt prompt) { Active.Remove(prompt); Destroy(prompt.gameObject); }
        public void Clear()
        {
            Running = false;
            sequencePending = false;
            foreach (var prompt in Active) if (prompt) Destroy(prompt.gameObject);
            Active.Clear();
        }
        private void OnDisable() { Clear(); }
    }
}
