using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AroundTheWorld
{
    public class FlagFinderGameMode : MonoBehaviour
    {
        private const string SelectedContinentKey = "SelectedContinent";

        [Header("Data")]
        [SerializeField] private TextAsset questionsJson;
        [SerializeField] private Sprite[] flagSprites;
        [SerializeField] private bool loadFlagsFromResources = true;
        [SerializeField] private string resourcesFlagsPath = "Flags";
        [SerializeField] private bool useContinentSubfolder = true;

        [Header("UI")]
        [SerializeField] private Image flagImage;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;
        private Button submitButton;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text scoreText, streakText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private RectTransform optionsPanelTransform;
        [SerializeField] private CanvasGroup optionsCanvasGroup;
        [SerializeField] private CanvasGroup gameplayCanvasGroup;
        [SerializeField] private CanvasGroup summaryCanvasGroup;

        [Header("Option Colors")]
        [SerializeField] private Color normalOptionColor = Color.white;
        [SerializeField] private Color selectedOptionColor = new Color(0.85f, 0.9f, 1f, 1f);

        [Header("Question Animation")]
        [SerializeField] private bool playQuestionIntroAnimation = true;
        [SerializeField] private float questionAnimDuration = 0.2f;
        [SerializeField] private float questionStartScale = 0.96f;

        [Header("Round Settings")]
        [SerializeField] private int questionsPerRound = 10;
        [SerializeField] private float panelFadeDuration = 0.25f;

        private readonly List<FlagQuestion> _questions = new List<FlagQuestion>();
        private readonly Dictionary<string, Sprite> _flagSpriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private int _currentQuestionIndex;
        private int _roundQuestionCount;
        private int _selectedOptionIndex = -1;
        private int _correctCount;
        private int _answeredCount;
        private int _score;
        private int _streak = 1;
        private bool _lastAnswerCorrect;
        private Coroutine _questionAnimRoutine;
        private Coroutine _panelFadeRoutine;
        private string[] _currentOptions = Array.Empty<string>();
        private int _currentCorrectIndex = -1;

        private void Awake()
        {
            if (optionsPanelTransform == null && optionButtons != null && optionButtons.Length > 0 && optionButtons[0] != null)
            {
                optionsPanelTransform = optionButtons[0].transform.parent as RectTransform;
            }

            if (optionsCanvasGroup == null && optionsPanelTransform != null)
            {
                optionsCanvasGroup = optionsPanelTransform.GetComponent<CanvasGroup>();
            }

            if (gameplayCanvasGroup == null && gameplayPanel != null)
            {
                gameplayCanvasGroup = gameplayPanel.GetComponent<CanvasGroup>();
            }

            if (summaryCanvasGroup == null && summaryPanel != null)
            {
                summaryCanvasGroup = summaryPanel.GetComponent<CanvasGroup>();
            }
            BuildFlagSpriteLookup();
        }

        private void Start()
        {
            BindOptionButtons();
            BindSubmitButton();
            LoadQuestions();
            ShuffleQuestions();
            StartRound();
        }

        public void SelectOption(int optionIndex)
        {
            Debug.Log("SelectOption: " + optionIndex);
            if (optionIndex < 0 || optionIndex >= optionButtons.Length)
            {
                return;
            }

            _selectedOptionIndex = optionIndex;
            UpdateOptionColors();
            UpdateSubmitInteractivity();
            SubmitAnswer();
        }

        public void SubmitAnswer()
        {
            if (_questions.Count == 0 || _selectedOptionIndex < 0)
            {
                return;
            }

            var question = _questions[_currentQuestionIndex];
            bool isCorrect = _selectedOptionIndex == _currentCorrectIndex;
            if (isCorrect)
            {
                _correctCount++;
                UpdateStreakOnCorrect();
                _score += CalculatePointsForStreak(_streak);
            }
            else
            {
                _streak = 1;
                _lastAnswerCorrect = false;
            }

            _answeredCount++;
            UpdateScoreUI();

            _currentQuestionIndex++;
            if (_currentQuestionIndex >= _roundQuestionCount)
            {
                EndRound();
                return;
            }

            _selectedOptionIndex = -1;
            UpdateOptionColors();
            UpdateSubmitInteractivity();
            ShowCurrentQuestion();
        }

        public void StartRound()
        {
            _currentQuestionIndex = 0;
            _selectedOptionIndex = -1;
            _correctCount = 0;
            _answeredCount = 0;
            _score = 0;
            _streak = 1;
            _lastAnswerCorrect = false;
            _roundQuestionCount = _questions.Count == 0
                ? 0
                : Mathf.Min(Mathf.Max(1, questionsPerRound), _questions.Count);

            if (summaryPanel != null)
            {
                summaryPanel.SetActive(false);
            }

            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(true);
            }

            if (gameplayCanvasGroup != null)
            {
                gameplayCanvasGroup.alpha = 1f;
            }

            if (summaryCanvasGroup != null)
            {
                summaryCanvasGroup.alpha = 0f;
            }

            UpdateOptionColors();
            UpdateSubmitInteractivity();
            UpdateScoreUI();
            ShowCurrentQuestion();
        }

        private void BindOptionButtons()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SelectOption(index));
            }
        }

        private void BindSubmitButton()
        {
            if (submitButton == null)
            {
                return;
            }

            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitAnswer);
        }

        private void LoadQuestions()
        {
            _questions.Clear();

            if (questionsJson == null)
            {
                Debug.LogError("FlagFinderGameMode: Questions JSON is missing.");
                return;
            }

            var data = JsonUtility.FromJson<FlagQuestionList>(questionsJson.text);
            if (data == null || data.questions == null || data.questions.Length == 0)
            {
                Debug.LogError("FlagFinderGameMode: Questions JSON is empty or invalid.");
                return;
            }

            string selectedContinent = GetSelectedContinentName();
            for (int i = 0; i < data.questions.Length; i++)
            {
                var question = data.questions[i];
                if (IsContinentMatch(selectedContinent, question.continent))
                {
                    _questions.Add(question);
                }
            }

            if (_questions.Count == 0)
            {
                Debug.LogWarning("FlagFinderGameMode: No questions found for continent " + selectedContinent);
            }
        }

        private void ShowCurrentQuestion()
        {
            if (_questions.Count == 0)
            {
                return;
            }

            var question = _questions[_currentQuestionIndex];
            UpdateFlagImage(question.flagImageName);
            PrepareAndShowOptions(question);
            PlayQuestionIntroAnimation();
        }

        private void PrepareAndShowOptions(FlagQuestion question)
        {
            if (question == null || question.options == null || question.options.Length == 0)
            {
                _currentOptions = Array.Empty<string>();
                _currentCorrectIndex = -1;
                UpdateOptions(_currentOptions);
                return;
            }

            var shuffledOptions = new List<string>(question.options);
            for (int i = 0; i < shuffledOptions.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, shuffledOptions.Count);
                string temp = shuffledOptions[i];
                shuffledOptions[i] = shuffledOptions[j];
                shuffledOptions[j] = temp;
            }

            string correctOption = question.options[Mathf.Clamp(question.correctOptionIndex, 0, question.options.Length - 1)];
            _currentOptions = shuffledOptions.ToArray();
            _currentCorrectIndex = shuffledOptions.IndexOf(correctOption);
            UpdateOptions(_currentOptions);
        }

        private void UpdateFlagImage(string flagImageName)
        {
            if (flagImage == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(flagImageName))
            {
                Debug.LogWarning("FlagFinderGameMode: Flag image name is empty.");
                return;
            }

            string normalizedName = flagImageName.Trim();
            if (_flagSpriteLookup.TryGetValue(normalizedName, out var sprite))
            {
                flagImage.sprite = sprite;
                flagImage.enabled = true;
            }
            else
            {
                Debug.LogWarning("FlagFinderGameMode: Missing sprite for flag " + flagImageName);
                flagImage.enabled = false;
            }
        }

        private void UpdateOptions(string[] options)
        {
            if (options == null)
            {
                options = Array.Empty<string>();
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool hasOption = i < options.Length;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(hasOption);
                }

                if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                {
                    optionLabels[i].text = hasOption ? options[i] : string.Empty;
                }
            }
        }

        private void PlayQuestionIntroAnimation()
        {
            if (!playQuestionIntroAnimation)
            {
                return;
            }

            if (_questionAnimRoutine != null)
            {
                StopCoroutine(_questionAnimRoutine);
            }

            _questionAnimRoutine = StartCoroutine(QuestionIntroRoutine());
        }

        private IEnumerator QuestionIntroRoutine()
        {
            float duration = Mathf.Max(0.05f, questionAnimDuration);
            float elapsed = 0f;

            RectTransform flagRect = flagImage != null ? flagImage.rectTransform : null;
            Vector3 startScale = flagRect != null ? Vector3.one * questionStartScale : Vector3.one;
            Vector3 optionsStartScale = optionsPanelTransform != null ? Vector3.one * questionStartScale : Vector3.one;

            if (flagRect != null)
            {
                flagRect.localScale = startScale;
            }

            if (optionsPanelTransform != null)
            {
                optionsPanelTransform.localScale = optionsStartScale;
            }

            if (optionsCanvasGroup != null)
            {
                optionsCanvasGroup.alpha = 0f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);

                if (flagRect != null)
                {
                    flagRect.localScale = Vector3.Lerp(startScale, Vector3.one, eased);
                }

                if (optionsPanelTransform != null)
                {
                    optionsPanelTransform.localScale = Vector3.Lerp(optionsStartScale, Vector3.one, eased);
                }

                if (optionsCanvasGroup != null)
                {
                    optionsCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
                }

                yield return null;
            }

            if (flagRect != null)
            {
                flagRect.localScale = Vector3.one;
            }

            if (optionsPanelTransform != null)
            {
                optionsPanelTransform.localScale = Vector3.one;
            }

            if (optionsCanvasGroup != null)
            {
                optionsCanvasGroup.alpha = 1f;
            }
        }

        private void UpdateOptionColors()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                var button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                var image = button.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                image.color = i == _selectedOptionIndex ? selectedOptionColor : normalOptionColor;
            }
        }

        private void BuildFlagSpriteLookup()
        {
            _flagSpriteLookup.Clear();
            if (loadFlagsFromResources)
            {
                string loadPath = resourcesFlagsPath;
                if (useContinentSubfolder)
                {
                    string continent = GetSelectedContinentName();
                    if (!string.IsNullOrWhiteSpace(continent))
                    {
                        loadPath = resourcesFlagsPath + "/" + continent;
                    }
                }

                var resourceSprites = Resources.LoadAll<Sprite>(loadPath);
                AddSpritesToLookup(resourceSprites);
            }

            AddSpritesToLookup(flagSprites);
        }

        private void AddSpritesToLookup(Sprite[] sprites)
        {
            if (sprites == null)
            {
                return;
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null || string.IsNullOrWhiteSpace(sprite.name))
                {
                    continue;
                }

                if (!_flagSpriteLookup.ContainsKey(sprite.name))
                {
                    _flagSpriteLookup.Add(sprite.name, sprite);
                }
            }
        }

        private void ShuffleQuestions()
        {
            for (int i = 0; i < _questions.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, _questions.Count);
                var temp = _questions[i];
                _questions[i] = _questions[j];
                _questions[j] = temp;
            }
        }

        private string GetSelectedContinentName()
        {
            int continentIndex = PlayerPrefs.GetInt(SelectedContinentKey, 0);
            if (Enum.IsDefined(typeof(Continent), continentIndex))
            {
                return ((Continent)continentIndex).ToString();
            }

            return string.Empty;
        }

        private bool IsContinentMatch(string selected, string candidate)
        {
            if (string.IsNullOrWhiteSpace(selected) || string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            return NormalizeContinent(selected) == NormalizeContinent(candidate);
        }

        private string NormalizeContinent(string value)
        {
            return value.Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = _score.ToString();
            }

            if (streakText != null)
            {
                streakText.text = "Streak:\n" + _streak + "x";
            }
        }

        private void EndRound()
        {
            if (_panelFadeRoutine != null)
            {
                StopCoroutine(_panelFadeRoutine);
            }

            _panelFadeRoutine = StartCoroutine(FadeToSummaryRoutine());
        }

        private IEnumerator FadeToSummaryRoutine()
        {
            TMP_Text targetText = resultText != null ? resultText : summaryText;
            if (targetText != null)
            {
                targetText.text = "Congratulations!\nScore: " + _score;
            }

            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
            }

            float duration = Mathf.Max(0.05f, panelFadeDuration);
            float elapsed = 0f;

            if (summaryCanvasGroup != null)
            {
                summaryCanvasGroup.alpha = 0f;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (gameplayCanvasGroup != null)
                {
                    gameplayCanvasGroup.alpha = 1f - t;
                }

                if (summaryCanvasGroup != null)
                {
                    summaryCanvasGroup.alpha = t;
                }

                yield return null;
            }

            if (gameplayCanvasGroup != null)
            {
                gameplayCanvasGroup.alpha = 0f;
            }

            if (summaryCanvasGroup != null)
            {
                summaryCanvasGroup.alpha = 1f;
            }

            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(false);
            }
        }

        private void UpdateStreakOnCorrect()
        {
            _streak = _lastAnswerCorrect ? _streak + 1 : 1;
            _lastAnswerCorrect = true;
        }

        private int CalculatePointsForStreak(int streak)
        {
            int multiplier = Mathf.Max(1, streak - 1);
            return 10 * multiplier;
        }

        private void UpdateSubmitInteractivity()
        {
            if (submitButton == null)
            {
                return;
            }

            submitButton.interactable = _selectedOptionIndex >= 0;
        }

        public void OnClick_BackToMenu()
        {
            SceneManager.LoadScene("1_MenuScene");
        }

        [Serializable]
        private class FlagQuestionList
        {
            public FlagQuestion[] questions;
        }

        [Serializable]
        private class FlagQuestion
        {
            public string flagImageName;
            public string continent;
            public string[] options;
            public int correctOptionIndex;
        }
    }
}
