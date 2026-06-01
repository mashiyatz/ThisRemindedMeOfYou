using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Graphic targetGraphic;
    public Color normalColor;
    public Color hoverColor;
    public float duration = 0.2f;

    void Start()
    {
        if (targetGraphic != null) targetGraphic.color = normalColor;
    }

    void OnDisable()
    {
        if (targetGraphic != null)
        {
            DOTween.Kill(targetGraphic);
            targetGraphic.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData _) =>
        targetGraphic.DOColor(hoverColor, duration);

    public void OnPointerExit(PointerEventData _) =>
        targetGraphic.DOColor(normalColor, duration);
}
