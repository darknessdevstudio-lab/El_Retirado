using UnityEngine;
using UnityEngine.UI;

public class BatteryUI : MonoBehaviour
{
    [Header("Referencia")]
    public FlashlightSway flashlight; // tu script de linterna
    public Image fillImage;

    [Header("Colores")]
    public Color highColor = Color.green;
    public Color mediumColor = Color.yellow;
    public Color lowColor = Color.red;

    [Header("Parpadeo")]
    public float blinkSpeed = 5f;
    public float lowThreshold = 0.2f; // 20%



    void Update()
    {
        if (flashlight == null || fillImage == null) return;

        float batteryPercent = flashlight.battery / 100f;

        // 🔋 llenar barra
        fillImage.fillAmount = batteryPercent;

        // 🎨 cambiar color según nivel
        if (batteryPercent > 0.5f)
        {
            fillImage.color = highColor;
        }
        else if (batteryPercent > lowThreshold)
        {
            fillImage.color = mediumColor;
        }
        else
        {
            // 🔴 rojo + parpadeo
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            fillImage.color = new Color(lowColor.r, lowColor.g, lowColor.b, alpha);
        }
    }
}