using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPanel : MonoBehaviour
{

    [SerializeField] private GameObject menuPanel;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuPanel.SetActive(true);
        }
    }
    public void QuitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    public void ReturnToGame()
    {
        menuPanel.SetActive(false);
    }
}

