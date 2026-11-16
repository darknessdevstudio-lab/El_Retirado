using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    public float scaleMultiplier = 1.1f;
    public float speed = 15f;
    public AudioSource moveSound;

    private bool isSelected = false;
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        Vector3 target = isSelected ? baseScale * scaleMultiplier : baseScale;

        float smooth = 1 - Mathf.Exp(-speed * Time.deltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, target, smooth);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!isSelected)
        {
            isSelected = true;

            if (moveSound != null)
                moveSound.Play();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }
}