using System;
using UnityEngine;

namespace LeafGame
{
    public enum SFXType { LeafRustle, Success, Miss, Wind, Detach, Land }
    [Serializable] public sealed class SoundEntry
    {
        public SFXType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.4f;
    }
    [CreateAssetMenu(menuName = "LEAF/Audio library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public AudioClip gameplayMusic;
        [Tooltip("Assign Melon's supplied recording here. Never loaded by filename.")]
        public AudioClip finalMusic;
        public AudioClip placeholderFinalMusic;
        public AudioClip ambient;
        public AudioClip rain;
        public SoundEntry[] sounds = Array.Empty<SoundEntry>();
        [TextArea] public string finalMusicCredit = "Music: El Testament d'Amèlia — arrangement by Miguel Llobet (1900), performed/recorded by Melon.";
    }
}
