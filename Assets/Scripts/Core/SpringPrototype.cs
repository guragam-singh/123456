using UnityEngine;

namespace LeafGame
{
    // Standalone Phase 1 scene harness; the story scene uses GameFlow instead.
    public sealed class SpringPrototype : MonoBehaviour
    {
        public WindManager wind;
        public WindDifficultyProfile spring;
        private void Start() { wind.Begin(spring); }
    }
}
