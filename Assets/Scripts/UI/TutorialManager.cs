using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;



public class TutorialManager : MonoBehaviour
{
    public GameObject tutorialPanel;

    private bool isOpen = false;

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CloseTutorial();
        }
    }

    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
        isOpen = true;

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        isOpen = false;
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
