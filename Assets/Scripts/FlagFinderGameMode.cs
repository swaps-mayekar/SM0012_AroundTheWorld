using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("UI")]
        [SerializeField] private Image flagImage;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;
        [SerializeField] private Button submitButton;
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Option Colors")]
        [SerializeField] private Color normalOptionColor = Color.white;
        [SerializeField] private Color selectedOptionColor = new Color(0.85f, 0.9f, 1f, 1f);

        [Header("Feedback")]
        [SerializeField] private float feedbackDuration = 1.25f;

        private readonly List<FlagQuestion> _questions = new List<FlagQuestion>();
        private readonly Dictionary<string, Sprite> _flagSpriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private int _currentQuestionIndex;
        private int _selectedOptionIndex = -1;
        private int _correctCount;
        private int _answeredCount;
        private Coroutine _feedbackRoutine;

        private void Awake()
        {
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
            if (optionIndex < 0 || optionIndex >= optionButtons.Length)
            {
                return;
            }

            _selectedOptionIndex = optionIndex;
            UpdateOptionColors();
            UpdateSubmitInteractivity();
        }

        public void SubmitAnswer()
        {
            if (_questions.Count == 0 || _selectedOptionIndex < 0)
            {
                return;
            }

            var question = _questions[_currentQuestionIndex];
            bool isCorrect = _selectedOptionIndex == question.correctOptionIndex;
            if (isCorrect)
            {
                _correctCount++;
            }

            _answeredCount++;
            ShowFeedback(isCorrect, question);
            UpdateScoreUI();

            _currentQuestionIndex++;
            if (_currentQuestionIndex >= _questions.Count)
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

            if (summaryPanel != null)
            {
                summaryPanel.SetActive(false);
            }

            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(true);
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
            UpdateOptions(question.options);
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

            if (_flagSpriteLookup.TryGetValue(flagImageName, out var sprite))
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
            if (options == null || options.Length == 0)
            {
                return;
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

        private void BuildFlagSpriteLookup()
        {
            _flagSpriteLookup.Clear();
            if (loadFlagsFromResources)
            {
                var resourceSprites = Resources.LoadAll<Sprite>(resourcesFlagsPath);
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

        private void ShowFeedback(bool isCorrect, FlagQuestion question)
        {
            if (feedbackText == null)
            {
                return;
            }

            string message = isCorrect ? "Correct!" : GetWrongAnswerMessage(question);
            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
            }

            _feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message));
        }

        private IEnumerator ShowFeedbackRoutine(string message)
        {
            feedbackText.gameObject.SetActive(true);
            feedbackText.text = message;
            yield return new WaitForSecondsRealtime(feedbackDuration);
            feedbackText.gameObject.SetActive(false);
        }

        private string GetWrongAnswerMessage(FlagQuestion question)
        {
            if (question.options == null || question.options.Length == 0)
            {
                return "Wrong!";
            }

            int correctIndex = Mathf.Clamp(question.correctOptionIndex, 0, question.options.Length - 1);
            return "Wrong! Correct: " + question.options[correctIndex];
        }

        private void UpdateScoreUI()
        {
            if (scoreText == null)
            {
                return;
            }

            scoreText.text = "Score: " + _correctCount + " / " + _answeredCount;
        }

        private void EndRound()
        {
            if (gameplayPanel != null)
            {
                gameplayPanel.SetActive(false);
            }

            if (summaryPanel != null)
            {
                summaryPanel.SetActive(true);
            }

            if (summaryText != null)
            {
                summaryText.text = "Round complete!\nScore: " + _correctCount + " / " + _answeredCount;
            }
        }

        private void UpdateSubmitInteractivity()
        {
            if (submitButton == null)
            {
                return;
            }

            submitButton.interactable = _selectedOptionIndex >= 0;
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
