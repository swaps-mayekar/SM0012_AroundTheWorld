using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace AroundTheWorld
{
    public class MenuManager : MonoBehaviour
    {
        private const string SelectedModeKey = "SelectedGameMode";
        private const string SelectedContinentKey = "SelectedContinent";

        [Header("Panels (Optional)")]
        [SerializeField] private GameObject m_mainMenuPanel;
        [SerializeField] RectTransform[] m_mainMenuElements;
        [SerializeField] private GameObject continentSelectionPanel;
        [SerializeField] RectTransform[] m_continentSelectionElements;
        [SerializeField] TextMeshProUGUI m_headingText;

        [Header("Button Appear Animation")]
        [SerializeField] private float appearDuration = 0.18f;
        [SerializeField] private float appearStagger = 0.05f;
        [SerializeField] private float appearScale = 0.96f;

        [Header("Mode Scenes")]
        [SerializeField] private string flagFinderSceneName = "2_FlagFinder";
        [SerializeField] private string continentTriviaSceneName = "3_ContinentTrivia";
        [SerializeField] private string capitalQuestSceneName = "4_CapitalQuest";
        [SerializeField] private string countryCluesSceneName = "5_CountryClues";
        [SerializeField] private string borderBossSceneName = "6_BorderBoss";

        private GameMode _pendingMode;
        private Coroutine _mainMenuAppearRoutine;
        private Coroutine _continentAppearRoutine;

        private void Start()
        {
            BackToMainMenu();
        }

        public void OnClick_FlagFinderMode()
        {
            SelectMode(GameMode.Flag_Finder);
        }

        public void OnClick_ContinentTriviaMode()
        {
            SelectMode(GameMode.Continent_Trivia);
        }

        public void OnClick_CapitalQuestMode()
        {
            SelectMode(GameMode.Capital_Quest);
        }

        public void OnClick_CountryCluesMode()
        {
            SelectMode(GameMode.Country_Clues);
        }

        public void OnClick_BorderBossMode()
        {
            SelectMode(GameMode.Border_Boss);
        }

        public void SelectMode(GameMode mode)
        {
            _pendingMode = mode;
            PlayerPrefs.SetInt(SelectedModeKey, (int)mode);
            PlayerPrefs.Save();

            ShowContinentSelection();
        }

        public void OnClick_SelectContinent(int continentIndex)
        {
            var continent = (Continent)continentIndex;
            SelectContinent(continent);
        }

        public void SelectContinent(Continent continent)
        {
            PlayerPrefs.SetInt(SelectedContinentKey, (int)continent);
            PlayerPrefs.Save();

            LoadModeScene(_pendingMode);
        }

        public void BackToMainMenu()
        {
            if (m_mainMenuPanel != null)
            {
                m_mainMenuPanel.SetActive(true);
            }

            if (continentSelectionPanel != null)
            {
                continentSelectionPanel.SetActive(false);
            }

            PlayMainMenuButtons();
        }

        private void ShowContinentSelection()
        {
            if (m_mainMenuPanel != null)
            {
                m_mainMenuPanel.SetActive(false);
            }

            if (continentSelectionPanel != null)
            {
                continentSelectionPanel.SetActive(true);
            }

            m_headingText.text = _pendingMode.ToString().Replace("_", " ");

            PlayContinentButtons();
        }

        private void LoadModeScene(GameMode mode)
        {
            var sceneName = GetSceneName(mode);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("MenuManager: Scene name is empty for mode " + mode);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private string GetSceneName(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Flag_Finder:
                    return flagFinderSceneName;
                case GameMode.Continent_Trivia:
                    return continentTriviaSceneName;
                case GameMode.Capital_Quest:
                    return capitalQuestSceneName;
                case GameMode.Country_Clues:
                    return countryCluesSceneName;
                case GameMode.Border_Boss:
                    return borderBossSceneName;
                default:
                    return string.Empty;
            }
        }

        private void PlayMainMenuButtons()
        {
            if (m_mainMenuElements == null || m_mainMenuElements.Length == 0)
            {
                return;
            }

            if (_mainMenuAppearRoutine != null)
            {
                StopCoroutine(_mainMenuAppearRoutine);
            }

            _mainMenuAppearRoutine = StartCoroutine(PlayAppearAnimation(m_mainMenuElements));
        }

        private void PlayContinentButtons()
        {
            if (m_continentSelectionElements == null || m_continentSelectionElements.Length == 0)
            {
                return;
            }

            if (_continentAppearRoutine != null)
            {
                StopCoroutine(_continentAppearRoutine);
            }

            _continentAppearRoutine = StartCoroutine(PlayAppearAnimation(m_continentSelectionElements));
        }

        private IEnumerator PlayAppearAnimation(RectTransform[] buttons)
        {
            var states = new List<ButtonState>(buttons.Length);
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                var group = button.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = button.gameObject.AddComponent<CanvasGroup>();
                }

                states.Add(new ButtonState(button, group, button.localScale));
            }

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                state.Group.alpha = 0f;
                state.Group.interactable = false;
                state.Group.blocksRaycasts = false;
                state.Rect.localScale = state.BaseScale * appearScale;
            }

            yield return null;

            for (int i = 0; i < states.Count; i++)
            {
                StartCoroutine(AnimateButton(states[i]));
                if (appearStagger > 0f)
                {
                    yield return new WaitForSecondsRealtime(appearStagger);
                }
            }
        }

        private IEnumerator AnimateButton(ButtonState state)
        {
            if (appearDuration <= 0f)
            {
                state.Group.alpha = 1f;
                state.Rect.localScale = state.BaseScale;
                state.Group.interactable = true;
                state.Group.blocksRaycasts = true;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < appearDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / appearDuration);
                float eased = t * t * (3f - 2f * t);
                state.Group.alpha = eased;
                state.Rect.localScale = Vector3.Lerp(state.BaseScale * appearScale, state.BaseScale, eased);
                yield return null;
            }

            state.Group.alpha = 1f;
            state.Rect.localScale = state.BaseScale;
            state.Group.interactable = true;
            state.Group.blocksRaycasts = true;
        }

        private readonly struct ButtonState
        {
            public RectTransform Rect { get; }
            public CanvasGroup Group { get; }
            public Vector3 BaseScale { get; }

            public ButtonState(RectTransform rect, CanvasGroup group, Vector3 baseScale)
            {
                Rect = rect;
                Group = group;
                BaseScale = baseScale;
            }
        }
    }

    public enum GameMode
    {
        Flag_Finder = 0,
        Continent_Trivia = 1,
        Capital_Quest = 2,
        Country_Clues = 3,
        Border_Boss = 4
    }

    public enum Continent
    {
        Africa = 0,
        Antarctica = 1,
        Asia = 2,
        Europe = 3,
        NorthAmerica = 4,
        Oceania = 5,
        SouthAmerica = 6
    }
}
