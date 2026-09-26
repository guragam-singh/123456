using UnityEngine;
using UnityEngine.UI;

namespace LeafGame
{
    public sealed class NarrationManager : MonoBehaviour
    {
        public AudioManager audioManager;
        public CanvasGroup panel;
        public Text subtitle;
        public float fadeSpeed = 2f;
        private FinalSequenceData data;
        private int nextCue;
        private float hideAt;
        private bool visible;
        public void Begin(FinalSequenceData sequence) { data = sequence; nextCue = 0; visible = false; panel.alpha = 0; }
        public void Tick(float elapsed)
        {
            if (!data) return;
            if (nextCue < data.narration.Length && elapsed >= data.narration[nextCue].atSeconds)
            {
                var cue = data.narration[nextCue++];
                subtitle.text = cue.text;
                hideAt = elapsed + cue.duration;
                visible = true;
                if (audioManager) audioManager.Speak(cue.voice);
            }
            if (elapsed >= hideAt) visible = false;
            panel.alpha = Mathf.MoveTowards(panel.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * fadeSpeed);
        }
        public void StopNarration()
        {
            data = null; visible = false; panel.alpha = 0;
            if (audioManager && audioManager.voice) audioManager.voice.Stop();
        }
    }
}
