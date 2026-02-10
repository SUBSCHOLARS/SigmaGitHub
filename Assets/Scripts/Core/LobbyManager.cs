using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GamePlay";
    [SerializeField] private string tutorialSceneName = "Tutorial";
    [Header("ボタン")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private GameObject fillObject;
    [Header("ボタンの押下サウンド")]
    [SerializeField] private AudioClip nextButtonSound;
    void Start()
    {
        if(TutorialGameManager.isTutorialFinish)
        {
            fillObject.SetActive(true);
            startButton.interactable=true;
            buttonText.color=Color.white;
            buttonText.text="ゲーム-1";
        }
    }
    public void OnStartGameClicked()
    {
        Debug.Log("Starting Game...");
        SoundManager.Instance.PlaySound(nextButtonSound);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnTutorialClicked()
    {
        Debug.Log("Starting Tutorial...");
        SoundManager.Instance.PlaySound(nextButtonSound);
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void OnQuitClicked()
    {   
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            SoundManager.Instance.PlaySound(nextButtonSound);
            Debug.Log("Quitting Game...");
            Application.Quit();
        #endif
    }
}
