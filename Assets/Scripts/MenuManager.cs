using UnityEngine;
using UnityEngine.SceneManagement;

namespace AroundTheWorld
{
    public class MenuManager : MonoBehaviour
    {
        private const string SelectedModeKey = "SelectedGameMode";
        private const string SelectedContinentKey = "SelectedContinent";

        [Header("Panels (Optional)")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject continentSelectionPanel;

        [Header("Mode Scenes")]
        [SerializeField] private string flagFinderSceneName = "2_FlagFinder";
        [SerializeField] private string continentTriviaSceneName = "3_ContinentTrivia";
        [SerializeField] private string capitalQuestSceneName = "4_CapitalQuest";
        [SerializeField] private string countryCluesSceneName = "5_CountryClues";
        [SerializeField] private string borderBossSceneName = "6_BorderBoss";

        private GameMode _pendingMode;

        public void OnClick_FlagFinderMode()
        {
            SelectMode(GameMode.FlagFinder);
        }

        public void OnClick_ContinentTriviaMode()
        {
            SelectMode(GameMode.ContinentTrivia);
        }

        public void OnClick_CapitalQuestMode()
        {
            SelectMode(GameMode.CapitalQuest);
        }

        public void OnClick_CountryCluesMode()
        {
            SelectMode(GameMode.CountryClues);
        }

        public void OnClick_BorderBossMode()
        {
            SelectMode(GameMode.BorderBoss);
        }

        public void SelectMode(GameMode mode)
        {
            _pendingMode = mode;
            PlayerPrefs.SetInt(SelectedModeKey, (int)mode);
            PlayerPrefs.Save();

            ShowContinentSelection();
        }

        public void SelectContinent(int continentIndex)
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
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (continentSelectionPanel != null)
            {
                continentSelectionPanel.SetActive(false);
            }
        }

        private void ShowContinentSelection()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (continentSelectionPanel != null)
            {
                continentSelectionPanel.SetActive(true);
            }
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
                case GameMode.FlagFinder:
                    return flagFinderSceneName;
                case GameMode.ContinentTrivia:
                    return continentTriviaSceneName;
                case GameMode.CapitalQuest:
                    return capitalQuestSceneName;
                case GameMode.CountryClues:
                    return countryCluesSceneName;
                case GameMode.BorderBoss:
                    return borderBossSceneName;
                default:
                    return string.Empty;
            }
        }
    }

    public enum GameMode
    {
        FlagFinder = 0,
        ContinentTrivia = 1,
        CapitalQuest = 2,
        CountryClues = 3,
        BorderBoss = 4
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
