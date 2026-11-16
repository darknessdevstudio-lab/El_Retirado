using UnityEngine;

public class Interactuable : MonoBehaviour
{
    [Header("Configuración")]
    public string actionText = "Interactuar";

    // 🔥 Método que luego puedes personalizar
    public virtual void Interact()
    {
        Debug.Log("Interactuaste con: " + gameObject.name);
    }
}