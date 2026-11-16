using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrosshairController : MonoBehaviour
{
    [Header("Raycast")]
    public float distance = 3f;
    public Camera cam;

    [Header("Layer")]
    public LayerMask interactLayer;

    [Header("Crosshair")]
    public Image crosshairImage;
    public Sprite normalCrosshair;
    public Sprite interactCrosshair;

    [Header("UI")]
    public GameObject interactionUI;
    public TextMeshProUGUI interactionText;

    private Interactuable currentInteractuable;

    void Start()
    {
        interactionUI.SetActive(true);
        crosshairImage.sprite = normalCrosshair;
    }

    void Update()
    {
        DetectInteractuable();
        HandleInput();
    }

    void DetectInteractuable()
    {
        RaycastHit hit;

        Debug.DrawRay(cam.transform.position, cam.transform.forward * distance, Color.red);

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, distance, interactLayer))
        {
            Interactuable interact = hit.collider.GetComponentInParent<Interactuable>();

            if (interact != null)
            {
                currentInteractuable = interact;

                // TEXTO
                interactionText.text = interact.actionText;

                // ACTIVAR UI
                if (!interactionUI.activeSelf)
                    interactionUI.SetActive(true);

                // 🔥 POSICIONAR UI EN EL MUNDO
                Vector3 offset = hit.normal * 0.05f + Vector3.up * 0.1f;
                interactionUI.transform.position = hit.point + offset;
                interactionUI.transform.position = hit.point + hit.normal * 0.05f;

                // 🔥 HACER QUE MIRE A LA CÁMARA
                interactionUI.transform.LookAt(cam.transform);
                interactionUI.transform.Rotate(0, 180, 0);
                interactionUI.transform.forward = cam.transform.forward;

                // CROSSHAIR
                crosshairImage.sprite = interactCrosshair;

                return;
            }
        }

        currentInteractuable = null;

        if (interactionUI.activeSelf)
            interactionUI.SetActive(false);

        crosshairImage.sprite = normalCrosshair;
    }

    void HandleInput()
    {
        if (currentInteractuable != null && Input.GetMouseButtonDown(1))
        {
            currentInteractuable.Interact();
        }
    }
}