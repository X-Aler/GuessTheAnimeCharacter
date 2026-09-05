using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PrimeTween;
using YG; // Подключаем SDK Яндекс Игр

public class QuizGameController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI quoteText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private QuizAnswerCard[] answerCards; // Ровно 4 карточки
    [SerializeField] private GameObject quizScreen;
    [SerializeField] private GameObject resultScreen;
    [SerializeField] private TextMeshProUGUI resultScoreText;
    [SerializeField] private Image asnwerColorPanel;
    [SerializeField] private TextMeshProUGUI actionButtonText;

    [Header("Settings")]
    [SerializeField] private int questionsPerRound = 10;

    private List<CharacterData> _allCharacters;
    private List<CharacterData> _remainingTargets;
    private CharacterData _currentTarget;
    private int _score;
    private int _currentQuestionIndex;
    private bool _isQuizStarted = false; // Отслеживает, начата ли игра

    private void OnEnable()
    {
        // Подписываемся на смену языка плагина
        YG2.onSwitchLang += OnLanguageChanged;
    }

    private void OnDisable()
    {
        // Обязательно отписываемся во избежание утечек памяти
        YG2.onSwitchLang -= OnLanguageChanged;
    }

    private void Start()
    {
        LoadDatabase();

        // Выставляем стартовый текст кнопки ("Старт" / "Start") под язык пользователя
        UpdateActionButtonText();

        // Сигнализируем платформе Яндекс Игры, что игра загрузилась и готова к показу
        YG2.GameReadyAPI();
    }

    private void LoadDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("characters");
        if (jsonFile == null)
        {
            Debug.LogError("Файл characters.json не найден в Resources!");
            return;
        }

        CharacterDatabase db = JsonUtility.FromJson<CharacterDatabase>(jsonFile.text);
        _allCharacters = db.characters;
    }

    public void StartQuiz()
    {
        _isQuizStarted = true;
        _score = 0;
        _currentQuestionIndex = 0;

        _remainingTargets = new List<CharacterData>(_allCharacters);

        quizScreen.SetActive(true);
        resultScreen.SetActive(false);

        // Переключаем надпись кнопки на "Начать заново" / "Play Again"
        UpdateActionButtonText();

        NextQuestion();
    }

    private void NextQuestion()
    {
        if (_currentQuestionIndex >= questionsPerRound || _remainingTargets.Count == 0)
        {
            EndQuiz();
            return;
        }

        _currentQuestionIndex++;
        progressText.text = $"{_currentQuestionIndex} / {questionsPerRound}";

        // 1. Выбираем целевого персонажа
        int targetIndex = UnityEngine.Random.Range(0, _remainingTargets.Count);
        _currentTarget = _remainingTargets[targetIndex];
        _remainingTargets.RemoveAt(targetIndex);

        UpdateQuoteDisplay();

        // 2. Выбираем 3 случайных неправильных варианта
        List<CharacterData> options = new List<CharacterData> { _currentTarget };
        List<CharacterData> pool = new List<CharacterData>(_allCharacters);
        pool.Remove(_currentTarget);

        while (options.Count < 4 && pool.Count > 0)
        {
            int randIndex = UnityEngine.Random.Range(0, pool.Count);
            options.Add(pool[randIndex]);
            pool.RemoveAt(randIndex);
        }

        // 3. Тасуем варианты (алгоритм Фишера-Йетса)
        for (int i = options.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            (options[i], options[rnd]) = (options[rnd], options[i]);
        }

        // 4. Отрисовываем карточки с учетом текущего языка SDK
        for (int i = 0; i < answerCards.Length; i++)
        {
            CharacterData data = options[i];
            Sprite charSprite = Resources.Load<Sprite>($"Characters/{data.card_asset_name}");
            answerCards[i].Setup(data, charSprite, YG2.lang, OnCardSelected);
        }
    }

    private void OnCardSelected(CharacterData selectedCharacter)
    {
        if (selectedCharacter.id == _currentTarget.id)
        {
            _score++;
            AudioManager.Instance.PlaySFX(AudioManager.Instance.correctSound);
            Tween.Color(asnwerColorPanel, Color.green, 1f);
        }
        else
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.errorSound);
            Tween.Color(asnwerColorPanel, Color.red, 1f);
        }

        Tween.Alpha(asnwerColorPanel, 1, 0, 1f);

        NextQuestion();
    }

    private void EndQuiz()
    {
        quizScreen.SetActive(false);
        resultScreen.SetActive(true);
        UpdateResultScoreText();
    }

    // Реакция на смену языка в плагине (вызывается автоматически при YG2.SwitchLanguage)
    private void OnLanguageChanged(string newLang)
    {
        UpdateQuoteDisplay();
        UpdateActionButtonText();

        // Обновляем текст на карточках ответов (тултипы)
        for (int i = 0; i < answerCards.Length; i++)
        {
            if (answerCards[i] != null)
                answerCards[i].UpdateLanguage(newLang);
        }
    }

    private void UpdateQuoteDisplay()
    {
        if (_currentTarget != null && quoteText != null)
        {
            quoteText.text = $"«{_currentTarget.GetQuote(YG2.lang)}»";
        }
    }

    private void UpdateActionButtonText()
    {
        if (actionButtonText == null) return;

        if (!_isQuizStarted)
        {
            actionButtonText.text = YG2.lang == "ru" ? "Старт" : "Start";
        }
        else
        {
            actionButtonText.text = YG2.lang == "ru" ? "Начать заново" : "Play Again";
        }
    }

    private void UpdateResultScoreText()
    {
        if (resultScoreText != null)
        {
            if (YG2.lang == "ru")
                resultScoreText.text = $"Твой результат: {_score} из {questionsPerRound}!";
            else
                resultScoreText.text = $"Your score: {_score} of {questionsPerRound}!";
        }
    }
}