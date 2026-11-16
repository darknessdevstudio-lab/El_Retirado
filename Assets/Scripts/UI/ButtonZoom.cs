using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonZoom : MonoBehaviour,
    ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);
    public float speed = 10f;

    private Vector3 targetScale;

    void Start()
    {
        targetScale = normalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * speed
        );
    }

    // 🎮 TECLADO
    public void OnSelect(BaseEventData eventData)
    {
        targetScale = selectedScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetScale = normalScale;
    }

    // 🖱 MOUSE
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = selectedScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }
}