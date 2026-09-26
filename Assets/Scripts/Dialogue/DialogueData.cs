using System;
using UnityEngine;

namespace LeafGame
{
    [Serializable] public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(1, 3)] public string text;
        [Min(0.5f)] public float seconds = 3f;
        public AudioClip voice;
    }
    [CreateAssetMenu(menuName = "LEAF/Dialogue")]
    public sealed class DialogueData : ScriptableObject
    {
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
    }
}
