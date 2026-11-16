using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    public Transform target; // objeto a seguir
    public Vector3 hitPoint;
    public Vector3 offset = Vector3.zero;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        gameObject.SetActive(false); // 👈 importante
    }

    void LateUpdate()
    {
        if (target == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // posición pegada al objeto
        Vector3 dirToCamera = (cam.transform.position - hitPoint).normalized;
        transform.position = hitPoint + dirToCamera * 0.1f + offset;

        Vector3 direction = cam.transform.position - transform.position;
        direction.y = 0; // 🔥 evita inclinación vertical

        transform.rotation = Quaternion.LookRotation(-direction);
    }
}