using UnityEngine;
using UnityEngine.EventSystems;

public class MenuAudio : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    public AudioSource audioSource;
    public AudioClip hoverSound;

    public void OnSelect(BaseEventData eventData)
    {
        PlaySound();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound();
    }

    void PlaySound()
    {
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }
    void Start()
    {
        if (audioSource != null)
        {
            audioSource.ignoreListenerPause = true;
        }
    }
}