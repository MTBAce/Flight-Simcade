using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject helpPanel;
    
    private void Start()
    {
        // Show main menu on start
        ShowMainMenu();
    }
    
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        helpPanel.SetActive(false);
    }
    
    public void ShowHelp()
    {
        helpPanel.SetActive(true);
    }
   
    public void StartGame()
    {
        SceneManager.LoadScene("Main");
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
}
