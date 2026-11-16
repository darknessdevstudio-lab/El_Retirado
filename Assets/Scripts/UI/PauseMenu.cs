using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Panel completo (fondo + botones)
    public MonoBehaviour playerMovement; // Script del movimiento
    public MonoBehaviour mouseLook; // Script de la cámara
    public GameObject firstButton;

    private bool isPaused = false;
    public FadeManager fadeManager;
    public TutorialManager tutorialManager;

    public void GoToMainMenu()
    {
        fadeManager.FadeToScene("Menu_Ini");
    }

    void Start()
    {
        pauseMenuUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 🧠 PRIORIDAD: si el tutorial está abierto, no dejar que el menú se cierre
        if (tutorialManager != null && tutorialManager.IsOpen())
        {
            return;
        }

        // Menú normal
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        // Click derecho SOLO cierra menú (si no hay tutorial)
        if (isPaused && Input.GetMouseButtonDown(1))
        {
            Resume();
        }
    }
    

public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerMovement.enabled = true;
        mouseLook.enabled = true;

        isPaused = false;
        AudioListener.pause = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerMovement.enabled = false;
        mouseLook.enabled = false;

        isPaused = true;
        AudioListener.pause = true;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("Menu_Ini"); // nombre EXACTO de tu escena
    }


}