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

        [Header("Credits Placeholder (Figma-Ready)")]
        [Tooltip("Placeholder textbox for game credits. Can be directly modified or replaced with Figma designs.")]
        public Text creditsTextBox;
        [Tooltip("Optional container GameObject for the credits textbox.")]
        public GameObject creditsContainer;

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
            if (creditsContainer) creditsContainer.SetActive(false);
            if (creditsTextBox) creditsTextBox.gameObject.SetActive(false);
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
        public void ShowLoseEnding(AudioLibrary audioLibrary, string loseMessage = "YOU LOSE")
        {
            final = true;
            if (instructions) instructions.gameObject.SetActive(false);
            if (seasonLabel) seasonLabel.gameObject.SetActive(false);
            if (title) title.gameObject.SetActive(false);
            ShowFinalLine(loseMessage);
            ShowMenu(audioLibrary);
        }
        public void ShowMenu(AudioLibrary audioLibrary)
        {
            MenuVisible = true;
            endingPanel.interactable = true; endingPanel.blocksRaycasts = true;
            restartButton.gameObject.SetActive(true); quitButton.gameObject.SetActive(true);
            menuText.gameObject.SetActive(true);

            EnsureCreditsPlaceholder();
            if (creditsContainer) creditsContainer.SetActive(true);
            if (creditsTextBox) creditsTextBox.gameObject.SetActive(true);

            if (creditText)
            {
                creditText.gameObject.SetActive(true);
                creditText.text = audioLibrary != null
                    ? (audioLibrary.finalMusic ? audioLibrary.finalMusicCredit : "Prototype audio placeholders • final recording and narration pending\n" + audioLibrary.finalMusicCredit)
                    : "";
            }
        }
        private void EnsureCreditsPlaceholder()
        {
            if (creditsTextBox != null) return;

            if (endingPanel != null)
            {
                var existing = endingPanel.transform.Find("Figma_Credits_Placeholder");
                if (existing != null)
                {
                    creditsTextBox = existing.GetComponent<Text>();
                    creditsContainer = existing.gameObject;
                    return;
                }

                var go = new GameObject("Figma_Credits_Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                go.transform.SetParent(endingPanel.transform, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -180f);
                rt.sizeDelta = new Vector2(960f, 120f);

                var txt = go.GetComponent<Text>();
                txt.font = endingLine != null ? endingLine.font : (menuText != null ? menuText.font : null);
                txt.fontSize = 15;
                txt.lineSpacing = 1.2f;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.94f, 0.91f, 0.78f, 0.9f);
                txt.text = "— GAME CREDITS —\n" +
                           "Game Design & Direction: [Your Name]\n" +
                           "Art & Animation: [Your Name]   •   Music & Audio: [Your Name]\n" +
                           "Special Thanks: [Your Name]\n" +
                           "(Placeholder credits textbox — customizable via Figma or Unity Inspector)";

                creditsTextBox = txt;
                creditsContainer = go;
            }
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
