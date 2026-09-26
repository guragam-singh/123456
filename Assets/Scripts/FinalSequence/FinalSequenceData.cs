using System;
using UnityEngine;

namespace LeafGame
{
    [Serializable] public sealed class NarrationCue
    {
        [Min(0f)] public float atSeconds;
        [Min(0.5f)] public float duration = 6f;
        [TextArea(1, 3)] public string text;
        public AudioClip voice;
    }
    [CreateAssetMenu(menuName = "LEAF/Final sequence")]
    public sealed class FinalSequenceData : ScriptableObject
    {
        [Min(1f)] public float fallDuration = 58f;
        [Min(0f)] public float detachDuration = 1.4f;
        [Min(1f)] public float endingDuration = 12f;
        [Min(0f)] public float quietSeconds = 3f;
        [Min(0.1f)] public float musicFadeSeconds = 4f;
        public Vector3 landingPosition = new Vector3(1.8f, -14.9f, 0f);
        public AnimationCurve descent = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve horizontalDrift = new AnimationCurve(new Keyframe(0,0),new Keyframe(0.2f,-1.5f),new Keyframe(0.45f,1.2f),new Keyframe(0.7f,-1.2f),new Keyframe(1,0));
        public AnimationCurve rotation = new AnimationCurve(new Keyframe(0,-90),new Keyframe(0.25f,-155),new Keyframe(0.5f,-30),new Keyframe(0.8f,-205),new Keyframe(1,-180));
        public float flutterAmplitude = 0.16f;
        public float flutterCycles = 5f;
        [TextArea] public string finalLine = "Some things are not meant to be held forever.";
        public NarrationCue[] narration = Array.Empty<NarrationCue>();

        public Vector3 EvaluatePosition(Vector3 start, float normalized)
        {
            float t = Mathf.Clamp01(normalized);
            if (t >= 1f) return landingPosition;
            Vector3 position = Vector3.LerpUnclamped(start, landingPosition, Mathf.Clamp01(descent.Evaluate(t)));
            position.x += horizontalDrift.Evaluate(t) + Mathf.Sin(t * flutterCycles * Mathf.PI * 2) * flutterAmplitude * Mathf.Sin(t * Mathf.PI);
            return position;
        }
    }
}
