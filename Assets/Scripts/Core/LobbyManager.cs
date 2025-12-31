using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GamePlay";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    public void OnStartGameClicked()
    {
        Debug.Log("Starting Game...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnTutorialClicked()
    {
        Debug.Log("Starting Tutorial...");
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
