using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LeafGame
{
    public sealed class DialogueManager : MonoBehaviour
    {
        public SeasonManager seasons;
        public CanvasGroup panel;
        public Text speakerLabel;
        public Text lineLabel;
        public AudioManager audioManager;
        public float fadeSpeed = 5f;
        private bool[] triggered;
        private Coroutine conversation;
        private bool visible;

        private void OnEnable() { if (seasons) seasons.Changed += OnSeason; }
        private void OnDisable()
        {
            if (seasons) seasons.Changed -= OnSeason;
            Clear();
        }
        private void OnSeason(SeasonData data)
        {
            Clear();
            triggered = new bool[data.dialogueEvents.Length];
        }
        private void Update()
        {
            if (panel) panel.alpha = Mathf.MoveTowards(panel.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * fadeSpeed);
            if (!seasons || !seasons.Running || !seasons.Current || triggered == null) return;
            var events = seasons.Current.dialogueEvents;
            for (int i = 0; i < events.Length; i++)
            {
                if (triggered[i] || seasons.Elapsed < events[i].atSeconds || conversation != null) continue;
                triggered[i] = true;
                if (events[i].dialogue) conversation = StartCoroutine(Show(events[i].dialogue));
                break;
            }
        }
        private IEnumerator Show(DialogueData data)
        {
            foreach (var line in data.lines)
            {
                speakerLabel.text = line.speaker;
                lineLabel.text = line.text;
                visible = true;
                if (audioManager) audioManager.Speak(line.voice);
                yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, line.seconds));
            }
            visible = false;
            conversation = null;
        }
        public void Clear()
        {
            if (conversation != null) StopCoroutine(conversation);
            conversation = null; visible = false;
            if (panel) panel.alpha = 0f;
            if (audioManager && audioManager.voice) audioManager.voice.Stop();
        }
    }
}
