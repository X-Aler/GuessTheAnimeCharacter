using UnityEngine;
using UnityEngine.EventSystems;
using PrimeTween;
using YG;
public class ButtonVFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonSound);
        YG2.InterstitialAdvShow();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonEnterSound);
        Tween.Scale(transform, Vector2.one * 1.2f, 0.5f);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        Tween.Scale(transform, Vector2.one, 0.5f);
    }
}
