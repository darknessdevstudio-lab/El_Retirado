using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public HeadBobSystem headbob;
    [Header("Movimiento")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3f;
    public float crouchSpeed = 1f;

    [Header("Agacharse")]
    public float crouchHeight = 1f;
    public float normalHeight = 2f;
    public float crouchTransitionSpeed = 8f;

    [Header("Lean")]
    public float leanAngle = 15f;
    public float leanSpeed = 5f;
    public float leanOffset = 0.5f;

    private float currentLean;
    private float targetLean;
    private Vector3 originalCamPos;

    [Header("Mouse")]
    public float mouseSensitivity = 100f;
    public Transform cameraPivot;

    [Header("Raycast")]
    public float rayDistance = 1.5f;

    private CharacterController controller;
    private float xRotation = 0f;
    private float speed;
    private Vector3 velocity;

    private float defaultCamY;
    private float bobTimer;

    private bool isCrouching;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        defaultCamY = cameraPivot.localPosition.y;
        originalCamPos = cameraPivot.localPosition;
        Vector3 rot = cameraPivot.localRotation.eulerAngles;
        xRotation = rot.x;
    }

    void Update()
    {
        MouseLook();
        Movement();
        Crouch();
        RaycastCheck();
        HandleLean();
        ControlHeadbob();
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * 100f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * 100f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, currentLean);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        if (isCrouching)
            speed = crouchSpeed;
        else if (isRunning)
            speed = runSpeed;
        else
            speed = walkSpeed;

        Vector3 move = transform.right * x + transform.forward * z;
        move = move.normalized;

        Vector3 finalMove = move * speed;

        // gravedad
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += Physics.gravity.y * Time.deltaTime;

        // aplicar TODO junto
        Vector3 total = finalMove + velocity;

        controller.Move(total * Time.deltaTime);
    }

    void Crouch()
    {
        if (Input.GetKey(KeyCode.LeftControl))
            isCrouching = true;
        else
            isCrouching = false;

        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
    }

    void ControlHeadbob()
    {
        if (headbob == null) return;

        float speed = controller.velocity.magnitude;

        if (speed > 5f)
        {
            headbob.Frequency = 10f;
            headbob.Amount = 0.04f;
        }
        else if (speed > 1f)
        {
            headbob.Frequency = 8f;
            headbob.Amount = 0.02f;
        }
        else
        {
            headbob.Amount = 0f;
        }
    }

    void RaycastCheck()
    {
        Ray ray = new Ray(cameraPivot.position, cameraPivot.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.green);

            // Ejemplo: detectar objeto
            if (hit.collider.CompareTag("Interactuable"))
            {
                Debug.Log("Mirando: " + hit.collider.name);
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);
        }
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            if (Input.GetMouseButtonDown(1))
            {
                SwitchInteract sw = hit.collider.GetComponent<SwitchInteract>();

                if (sw != null)
                {
                    sw.Interact();
                }
            }
        }
    }

    void HandleLean()
    {
        // Input
        if (Input.GetKey(KeyCode.Q))
            targetLean = leanAngle;
        else if (Input.GetKey(KeyCode.E))
            targetLean = -leanAngle;
        else
            targetLean = 0;

        // Suavizado
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // Rotación (inclinación)
        cameraPivot.localRotation = Quaternion.Euler(
            cameraPivot.localRotation.eulerAngles.x,
            0,
            currentLean
        );

        // Movimiento lateral (para que se vea más realista)
        float leanSide = currentLean / leanAngle;
        Vector3 targetPos = originalCamPos + new Vector3(-leanSide * leanOffset, 0, 0);

        cameraPivot.localPosition = Vector3.Lerp(
            cameraPivot.localPosition,
            targetPos,
            Time.deltaTime * leanSpeed
        );
    }
}