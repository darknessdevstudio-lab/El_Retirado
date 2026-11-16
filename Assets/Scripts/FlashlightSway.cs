
using UnityEngine;

public class FlashlightSway : MonoBehaviour
{
    public GameObject flashlight;

    [Header("Sway")]
    public float smoothSpeed = 8f;
    public float rotationAmount = 5f;

    [Header("Flicker (vibración)")]
    public float minFlickerTime = 120f;
    public float maxFlickerTime = 600f;
    public float flickerDuration = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;
    public AudioClip flickerSound;

    [Header("Battery")]
    public float battery = 100f;
    public bool useBattery = true;

    [Tooltip("Tiempo en segundos que dura la batería al 100%")]
    public float batteryDurationSeconds = 180f;

    private Quaternion initialRotation;
    private bool isOn = true;

    private float flickerTimer;
    private float nextFlickerTime;
    private bool isFlickering;

    private Light[] lights;
    private float baseIntensity = 1f;

    void Start()
    {
        initialRotation = transform.localRotation;
        lights = flashlight.GetComponentsInChildren<Light>();

        if (lights.Length > 0)
            baseIntensity = lights[0].intensity;

        SetNextFlickerTime();
    }

    void Update()
    {
        HandleToggle();
        HandleSway();
        HandleBattery();
        HandleFlicker();
    }

    // ENCENDER / APAGAR
    void HandleToggle()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!isOn && useBattery && battery <= 0f)
                return;

            isOn = !isOn;

            foreach (Light l in lights)
                l.enabled = isOn;

            if (audioSource)
            {
                if (isOn && turnOnSound)
                    audioSource.PlayOneShot(turnOnSound);
                else if (!isOn && turnOffSound)
                    audioSource.PlayOneShot(turnOffSound);
            }
        }
    }

    // SWAY
    void HandleSway()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationAmount;
        float mouseY = Input.GetAxis("Mouse Y") * rotationAmount;

        Quaternion targetRotation = Quaternion.Euler(-mouseY, mouseX, 0);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            initialRotation * targetRotation,
            Time.deltaTime * smoothSpeed
        );
    }

    // BATERÍA
    void HandleBattery()
    {
        if (!isOn || !useBattery) return;

        // Consume el 100% durante el tiempo configurado.
        float duration = Mathf.Max(1f, batteryDurationSeconds);
        battery -= (100f / duration) * Time.deltaTime;

        battery = Mathf.Clamp(battery, 0f, 100f);

        if (battery <= 0f)
        {
            isOn = false;

            foreach (Light l in lights)
                l.enabled = false;
        }
    }

    // FLICKER
    void HandleFlicker()
    {
        if (!isOn) return;

        if (useBattery && battery < 20f)
        {
            foreach (Light l in lights)
                l.enabled = Random.value > 0.5f;

            return;
        }

        flickerTimer += Time.deltaTime;

        if (!isFlickering && flickerTimer >= nextFlickerTime)
        {
            isFlickering = true;
            flickerTimer = 0;

            if (audioSource && flickerSound)
                audioSource.PlayOneShot(flickerSound);
        }

        if (isFlickering)
        {
            foreach (Light l in lights)
                l.intensity = Random.Range(0.2f, 1f);

            if (flickerTimer >= flickerDuration)
            {
                isFlickering = false;
                flickerTimer = 0;

                foreach (Light l in lights)
                    l.intensity = baseIntensity;

                SetNextFlickerTime();
            }
        }
    }

    void SetNextFlickerTime()
    {
        if (useBattery)
        {
            float batteryPercent = battery / 100f;

            float minTime = Mathf.Lerp(5f, 300f, batteryPercent);
            float maxTime = Mathf.Lerp(15f, 900f, batteryPercent);

            nextFlickerTime = Random.Range(minTime, maxTime);
        }
        else
        {
            nextFlickerTime = Random.Range(minFlickerTime, maxFlickerTime);
        }
    }
}
