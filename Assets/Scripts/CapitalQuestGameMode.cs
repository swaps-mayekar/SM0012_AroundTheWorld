using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AroundTheWorld
{
    public class CapitalQuestGameMode : MonoBehaviour
    {
        private const string SelectedContinentKey = "SelectedContinent";

        [Header("Data")]
        [SerializeField] private TextAsset questionsJson;

        [Header("UI")]
        [SerializeField] private TMP_Text countryNameText;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text streakText;
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

        private readonly List<CapitalQuestion> _questions = new List<CapitalQuestion>();
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
        }

        private void Start()
        {
            BindOptionButtons();
            LoadQuestions();
            ShuffleQuestions();
            StartRound();
        }

        public void SelectOption(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= optionButtons.Length)
            {
                return;
            }

            _selectedOptionIndex = optionIndex;
            UpdateOptionColors();
            SubmitAnswer();
        }

        private void SubmitAnswer()
        {
            if (_questions.Count == 0 || _selectedOptionIndex < 0)
            {
                return;
            }

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

        private void LoadQuestions()
        {
            _questions.Clear();

            if (questionsJson == null)
            {
                Debug.LogError("CapitalQuestGameMode: Questions JSON is missing.");
                return;
            }

            var data = JsonUtility.FromJson<CapitalQuestionList>(questionsJson.text);
            if (data == null || data.questions == null || data.questions.Length == 0)
            {
                Debug.LogError("CapitalQuestGameMode: Questions JSON is empty or invalid.");
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
                Debug.LogWarning("CapitalQuestGameMode: No questions found for continent " + selectedContinent);
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

        private void ShowCurrentQuestion()
        {
            if (_questions.Count == 0 || _roundQuestionCount == 0)
            {
                return;
            }

            var question = _questions[_currentQuestionIndex];
            if (countryNameText != null)
            {
                countryNameText.text = question.countryName;
            }
            else
            {
                Debug.LogWarning("CapitalQuestGameMode: Country name text is not assigned.");
            }

            PrepareAndShowOptions(question);
            if (playQuestionIntroAnimation)
            {
                PlayQuestionIntroAnimation();
            }
            else
            {
                EnsureQuestionVisible();
            }
        }

        private void PrepareAndShowOptions(CapitalQuestion question)
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

        private void EnsureQuestionVisible()
        {
            if (optionsCanvasGroup != null)
            {
                optionsCanvasGroup.alpha = 1f;
            }

            if (optionsPanelTransform != null)
            {
                optionsPanelTransform.localScale = Vector3.one;
            }
        }

        private IEnumerator QuestionIntroRoutine()
        {
            float duration = Mathf.Max(0.05f, questionAnimDuration);
            float elapsed = 0f;

            Vector3 optionsStartScale = optionsPanelTransform != null ? Vector3.one * questionStartScale : Vector3.one;

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

            if (optionsPanelTransform != null)
            {
                optionsPanelTransform.localScale = Vector3.one;
            }

            if (optionsCanvasGroup != null)
            {
                optionsCanvasGroup.alpha = 1f;
            }
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
            if (resultText != null)
            {
                resultText.text = "Congratulations!\nScore: " + _score;
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

        [Serializable]
        private class CapitalQuestionList
        {
            public CapitalQuestion[] questions;
        }

        [Serializable]
        private class CapitalQuestion
        {
            public string countryName;
            public string continent;
            public string[] options;
            public int correctOptionIndex;
        }
    }
}
