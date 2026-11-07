using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class QuizAnswerCard : ButtonVFX
{
    [SerializeField] private TextMeshProUGUI tooltipNameText;

    private Image characterImage;
    private Button button;

    private CharacterData _character;
    private Action<CharacterData> _onSelected;
    private string _currentLang;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
        characterImage = GetComponent<Image>();
        HideTooltip();
    }

    public void Setup(CharacterData character, Sprite sprite, string lang, Action<CharacterData> onSelected)
    {
        _character = character;
        _currentLang = lang;
        _onSelected = onSelected;

        if (characterImage == null)
            characterImage = GetComponent<Image>();

        characterImage.sprite = sprite;
        UpdateTooltipText();
        HideTooltip();
    }

    public void UpdateLanguage(string newLang)
    {
        _currentLang = newLang;
        UpdateTooltipText();
    }

    private void UpdateTooltipText()
    {
        if (_character != null && tooltipNameText != null)
        {
            tooltipNameText.text = _character.GetName(_currentLang);
        }
    }

    private void HandleClick()
    {
        _onSelected?.Invoke(_character);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        if (tooltipNameText != null)
            tooltipNameText.gameObject.SetActive(true);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (tooltipNameText != null && tooltipNameText.gameObject != null)
            tooltipNameText.gameObject.SetActive(false);
    }
}