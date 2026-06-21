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
    [SerializeField] private Button freePlayButton;
    [SerializeField] private TextMeshProUGUI freePlayButtonText;
    [SerializeField] private GameObject freePlayFillObject;
    [Header("ボタンの押下サウンド")]
    [SerializeField] private AudioClip nextButtonSound;
    [Header("エンディング到達マーク")]
    [SerializeField] private GameObject markBrainwash;
    [SerializeField] private GameObject markDisqualification;
    [SerializeField] private GameObject markDisqualificationBeforeIdeology;
    [SerializeField] private GameObject markRevolution;

    void Start()
    {
        // Lobby に直接 PDM なしで来た場合の保険
        if (PersistentDataManager.Instance == null)
        {
            GameObject pdm = new GameObject("PersistentDataManager");
            pdm.AddComponent<PersistentDataManager>();
        }

        // チュートリアル完了判定（保存済み or 同セッション内完了の両方を考慮）
        bool tutorialDone = PersistentDataManager.Instance.TutorialFinished
                         || TutorialGameManager.isTutorialFinish;
        if (tutorialDone)
        {
            fillObject.SetActive(true);
            startButton.interactable = true;
            startButton.GetComponent<HoldButton>().enabled = true;
            buttonText.color = Color.white;
            int flag = PersistentDataManager.Instance.GameProgressFlag;
            buttonText.text = $"ゲーム-{flag + 1}/6";
        }

        // エンディング到達マークの表示制御
        if (markBrainwash != null)
            markBrainwash.SetActive(PersistentDataManager.Instance.EndingBrainwash);
        if (markDisqualification != null)
            markDisqualification.SetActive(PersistentDataManager.Instance.EndingDisqualification);
        if (markDisqualificationBeforeIdeology != null)
            markDisqualificationBeforeIdeology.SetActive(
                PersistentDataManager.Instance.EndingDisqualificationBeforeIdeology);
        if (markRevolution != null)
            markRevolution.SetActive(PersistentDataManager.Instance.EndingRevolution);

        if(markBrainwash || markDisqualification || markDisqualificationBeforeIdeology || markRevolution)
        {
            freePlayFillObject.SetActive(true);
            freePlayButton.interactable = true;
            freePlayButton.GetComponent<HoldButton>().enabled = true;
            freePlayButtonText.color = Color.white;
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

    public void OnFreePlayClicked()
    {
        Debug.Log("Starting Free Play...");
        SoundManager.Instance.PlaySound(nextButtonSound);
        SceneManager.LoadScene("FreeGamePlay");
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
