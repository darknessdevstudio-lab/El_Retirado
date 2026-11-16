using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public Button[] buttons;
    private int currentIndex = 0;

    float inputDelay = 0.2f;
    float lastInputTime;

    void Start()
    {
        SelectButton(0);
    }

    void Update()
    {
        if (Time.time - lastInputTime < inputDelay) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = buttons.Length - 1;

            SelectButton(currentIndex);
            lastInputTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex++;
            if (currentIndex >= buttons.Length) currentIndex = 0;

            SelectButton(currentIndex);
            lastInputTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            buttons[currentIndex].onClick.Invoke();
        }
    }

    void SelectButton(int index)
    {
        EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
    }

    // FUNCIONES
    public void PlayGame()
    {
        SceneManager.LoadScene("casa01");
    }

    public void Options()
    {
        Debug.Log("Opciones");
    }

    public void Exit()
    {
        Application.Quit();
    }
}