using System;
using UnityEngine;

namespace LeafGame
{
    public enum GameState { Intro, Spring, Summer, Autumn, FinalSequence, Ending }

    [DefaultExecutionOrder(-50)]
    public sealed class GameFlow : MonoBehaviour
    {
        public SeasonManager seasons;
        public FinalSequenceController finalSequence;
        public BackgroundLeafSpawner backgroundLeaves;
        public LeafUI ui;
        public AudioManager audioManager;
        [Tooltip("Hard wall-clock ceiling, including the quiet ending.")]
        [Range(240f, 300f)] public float maximumSessionSeconds = 300f;
        public GameState State { get; private set; } = GameState.Intro;
        public float Elapsed => Time.unscaledTime - startTime;
        public float EndingScreenAt { get; private set; } = -1f;
        public float PlannedDuration => seasons.TotalDuration + finalSequence.data.fallDuration + finalSequence.data.endingDuration;
        public bool IsLost { get; private set; }
        public event Action<GameState> StateChanged;
        private float startTime;
        private float landingTime;
        private bool lineShown;

        private void OnEnable()
        {
            seasons.Changed += OnSeason;
            seasons.Completed += BeginFinal;
            finalSequence.Landed += OnLanded;
            if (seasons && seasons.wind) seasons.wind.WrongArrowLimitReached += LoseGame;
        }
        private void OnDisable()
        {
            seasons.Changed -= OnSeason;
            seasons.Completed -= BeginFinal;
            finalSequence.Landed -= OnLanded;
            if (seasons && seasons.wind) seasons.wind.WrongArrowLimitReached -= LoseGame;
        }
        private void Start()
        {
            Application.runInBackground = true;
            startTime = Time.unscaledTime;
            finalSequence.leaf.protectUntilFinal = true;
            if (seasons && seasons.wind)
            {
                seasons.wind.ResetWrongArrows();
                seasons.wind.WrongArrowLimitReached -= LoseGame;
                seasons.wind.WrongArrowLimitReached += LoseGame;
            }
            seasons.Begin();
        }
        private void OnSeason(SeasonData data)
        {
            SetState((GameState)((int)data.kind + 1));
            ui.SetSeason(data);
        }
        public void LoseGame()
        {
            if (IsLost || State == GameState.FinalSequence || State == GameState.Ending) return;
            IsLost = true;
            EndingScreenAt = Elapsed;
            seasons.StopSeasons();
            if (backgroundLeaves) { backgroundLeaves.Clear(); backgroundLeaves.enabled = false; }
            if (finalSequence.rain) { finalSequence.rain.StopRain(); finalSequence.rain.enabled = false; }
            if (finalSequence.dialogue) { finalSequence.dialogue.Clear(); finalSequence.dialogue.enabled = false; }
            if (finalSequence.narration) { finalSequence.narration.StopNarration(); }
            if (audioManager)
            {
                audioManager.FadeToSilence(1f);
                audioManager.PlaySFX(SFXType.Detach);
            }
            finalSequence.leaf.DetachNaturally();
            lineShown = true;
            ui.ShowLoseEnding(audioManager != null ? audioManager.library : null, "YOU LOSE");
            SetState(GameState.Ending);
            Debug.Log($"LEAF game over (lost): wrong arrow limit reached ({seasons.wind?.WrongArrowCount ?? 7} wrong arrows) at {Elapsed:F2}s");
        }
        public void BeginFinal()
        {
            if (IsLost || State == GameState.FinalSequence || State == GameState.Ending) return;
            seasons.StopSeasons();
            SetState(GameState.FinalSequence);
            ui.BeginFinal();
            finalSequence.Begin();
        }
        private void OnLanded()
        {
            landingTime = Time.unscaledTime;
            SetState(GameState.Ending);
            if (backgroundLeaves) { backgroundLeaves.Clear(); backgroundLeaves.enabled = false; }
        }
        private void Update()
        {
            if (IsLost) return;
            var data = finalSequence.data;
            float ceiling = Mathf.Clamp(maximumSessionSeconds, 240f, 300f);
            // A mistuned season cannot consume the time reserved for the cinematic and ending.
            if (State != GameState.FinalSequence && State != GameState.Ending && Elapsed >= ceiling - data.fallDuration - data.endingDuration - 0.25f) BeginFinal();
            if (State == GameState.FinalSequence && Elapsed >= ceiling - data.endingDuration - 0.1f) finalSequence.CompleteImmediately();
            if (State != GameState.Ending) return;
            float endingElapsed = Time.unscaledTime - landingTime;
            if (!lineShown && endingElapsed >= data.quietSeconds)
            {
                lineShown = true; ui.ShowFinalLine(data.finalLine);
            }
            if (!ui.MenuVisible && (endingElapsed >= data.endingDuration || Elapsed >= ceiling - 0.02f))
            {
                if (!lineShown) { lineShown = true; ui.ShowFinalLine(data.finalLine); }
                EndingScreenAt = Elapsed;
                ui.ShowMenu(audioManager.library);
                Debug.Log($"LEAF ending screen: {EndingScreenAt:F2}s (planned {PlannedDuration:F2}s)");
            }
        }
        private void SetState(GameState value) { State = value; StateChanged?.Invoke(value); }
    }
}
