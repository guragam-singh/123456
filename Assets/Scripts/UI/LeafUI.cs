using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LeafGame
{
    public sealed class LeafUI : MonoBehaviour
    {
        public Text title;
        public Text seasonLabel;
        public Text instructions;
        public CanvasGroup endingPanel;
        public Text endingLine;
        public Text menuText;
        public Text creditText;
        public Button restartButton;
        public Button quitButton;
        public float instructionSeconds = 15f;
        public float fadeSpeed = 1.8f;
        private float startTime;
        private bool final;
        private bool lineVisible;
        public bool MenuVisible { get; private set; }

        private void Awake()
        {
            endingPanel.alpha = 0; endingPanel.interactable = false; endingPanel.blocksRaycasts = false;
            restartButton.gameObject.SetActive(false); quitButton.gameObject.SetActive(false);
            menuText.gameObject.SetActive(false); creditText.gameObject.SetActive(false);
            restartButton.onClick.AddListener(Restart); quitButton.onClick.AddListener(Quit);
            startTime = Time.unscaledTime;
        }
        public void SetSeason(SeasonData data) { seasonLabel.text = data.seasonName.ToUpperInvariant(); }
        public void BeginFinal()
        {
            final = true;
            instructions.gameObject.SetActive(false); seasonLabel.gameObject.SetActive(false); title.gameObject.SetActive(false);
        }
        public void ShowFinalLine(string text)
        {
            lineVisible = true; endingLine.text = text;
        }
        public void ShowMenu(AudioLibrary audioLibrary)
        {
            MenuVisible = true;
            endingPanel.interactable = true; endingPanel.blocksRaycasts = true;
            restartButton.gameObject.SetActive(true); quitButton.gameObject.SetActive(true);
            menuText.gameObject.SetActive(true); creditText.gameObject.SetActive(true);
            creditText.text = audioLibrary.finalMusic ? audioLibrary.finalMusicCredit : "Prototype audio placeholders • final recording and narration pending\n" + audioLibrary.finalMusicCredit;
        }
        private void Update()
        {
            if (!final && Time.unscaledTime - startTime > instructionSeconds) instructions.gameObject.SetActive(false);
            endingPanel.alpha = Mathf.MoveTowards(endingPanel.alpha, lineVisible ? 1 : 0, Time.unscaledDeltaTime * fadeSpeed);
            var keyboard = Keyboard.current;
            if (!MenuVisible || keyboard == null) return;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame) Restart();
            if (keyboard.escapeKey.wasPressedThisFrame) Quit();
        }
        public void Restart() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
