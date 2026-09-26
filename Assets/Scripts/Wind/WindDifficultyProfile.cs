using UnityEngine;

namespace LeafGame
{
    [CreateAssetMenu(menuName = "LEAF/Wind difficulty")]
    public sealed class WindDifficultyProfile : ScriptableObject
    {
        [Min(0.5f)] public float spawnInterval = 5f;
        [Min(30f)] public float arrowSpeed = 150f;
        [Min(0.01f)] public float perfectWindow = 0.1f;
        [Min(0.02f)] public float goodWindow = 0.24f;
        [Min(0f)] public float windStrength = 16f;
        [Range(0f, 1f)] public float gustProbability;
        [Min(1f)] public float gustStrength = 1.7f;
        [Range(1, 3)] public int maximumSimultaneousPrompts = 1;
        [Range(0f, 1f)] public float sequenceProbability;
        [Min(0.5f)] public float sequenceSpacing = 0.85f;
        private void OnValidate() { goodWindow = Mathf.Max(perfectWindow, goodWindow); }
    }
}
