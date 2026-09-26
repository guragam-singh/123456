using System;
using UnityEngine;

namespace LeafGame
{
    public enum SeasonKind { Spring, Summer, Autumn }
    [Serializable] public sealed class SeasonDialogueEvent
    {
        [Min(0f)] public float atSeconds;
        public DialogueData dialogue;
    }
    [CreateAssetMenu(menuName = "LEAF/Season")]
    public sealed class SeasonData : ScriptableObject
    {
        public SeasonKind kind;
        public string seasonName;
        [Min(5f)] public float duration = 50f;
        public WindDifficultyProfile windDifficulty;
        public Sprite backgroundSprite;
        public Sprite[] foliageSprites = Array.Empty<Sprite>();
        public Color backgroundColor = new Color(0.16f, 0.27f, 0.23f);
        public Color foliageColor = new Color(0.25f, 0.42f, 0.25f);
        public Color leafColor = new Color(0.68f, 0.82f, 0.37f);
        public Color lightingColor = Color.white;
        [Range(0f, 1f)] public float foliageDensity = 1f;
        public AudioClip ambientAudio;
        public AudioClip music;
        [Range(0f, 1f)] public float rainIntensity;
        [Min(0f)] public float rainStartDelay = 8f;
        [Min(0f)] public float rainInstabilityPerSecond = 0.002f;
        [Min(0f)] public float fallingLeafInterval;
        public SeasonDialogueEvent[] dialogueEvents = Array.Empty<SeasonDialogueEvent>();
        [Header("Final challenge (within season duration)")]
        [Min(0f)] public float finalChallengeSeconds;
        public WindDifficultyProfile finalChallengeProfile;
    }
}
