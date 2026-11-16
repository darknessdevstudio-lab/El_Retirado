using UnityEngine;

public class SwitchInteract : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public AudioSource audioSource;

    [Header("Luces (1 a 4)")]
    public Light[] lights; // puedes meter varias luces aqu�

    [Header("Sonidos")]
    public AudioClip soundOn;
    public AudioClip soundOff;

    private bool isOn = true;

    public void Interact()
    {
        isOn = !isOn;
        ElRetirado.Enemies.NoiseSystem.Emit(transform.position, 6f);

        // Animaci�n
        if (animator != null)
            animator.SetBool("isOn", isOn);

        // Luces
        ToggleLights(isOn);

        // Sonido
        PlaySound(isOn);
    }

    void ToggleLights(bool state)
    {
        foreach (Light l in lights)
        {
            if (l != null)
                l.enabled = state;
        }
    }

    void PlaySound(bool state)
    {
        if (audioSource == null) return;

        if (state && soundOn != null)
            audioSource.PlayOneShot(soundOn);
        else if (!state && soundOff != null)
            audioSource.PlayOneShot(soundOff);
    }
}
