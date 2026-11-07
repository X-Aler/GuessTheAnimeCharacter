using UnityEngine;
using TMPro;
using YG;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("Translations")]
    [TextArea(2, 5)]
    [SerializeField] private string textRu;

    [TextArea(2, 5)]
    [SerializeField] private string textEn;

    private TextMeshProUGUI _textMeshPro;

    private void Awake()
    {
        _textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Подписываемся на смену языка в SDK
        YG2.onSwitchLang += UpdateText;

        // Обновляем текст при каждом включении объекта на случай, 
        // если язык изменился, пока объект был скрыт
        UpdateText(YG2.lang);
    }

    private void OnDisable()
    {
        // Обязательно отписываемся от события при деактивации объекта
        YG2.onSwitchLang -= UpdateText;
    }

    private void Start()
    {
        // Первичное обновление на старте
        UpdateText(YG2.lang);
    }

    /// <summary>
    /// Принудительно обновляет текст согласно переданному языковому коду.
    /// </summary>
    public void UpdateText(string lang)
    {
        if (_textMeshPro == null)
            _textMeshPro = GetComponent<TextMeshProUGUI>();

        // Если язык "ru" — выводим русский, для всех остальных случаев (en, tr и т.д.) — английский фоллбэк
        _textMeshPro.text = lang == "ru" ? textRu : textEn;
    }

    // Для удобства: отображение изменений прямо в редакторе при вводе текста
    private void OnValidate()
    {
        if (_textMeshPro == null)
            _textMeshPro = GetComponent<TextMeshProUGUI>();

        if (_textMeshPro != null && !Application.isPlaying)
        {
            // В режиме редактирования отображаем русский вариант по умолчанию
            if (!string.IsNullOrEmpty(textRu))
                _textMeshPro.text = textRu;
        }
    }
}