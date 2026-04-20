using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("ボタンの押下サウンド")]
    [SerializeField] private AudioClip nextButtonSound;

    private void Start()
    {
        // PDM がまだ存在しなければ生成（Awake シーンが起動シーンの場合）
        if (PersistentDataManager.Instance == null)
        {
            GameObject pdm = new GameObject("PersistentDataManager");
            pdm.AddComponent<PersistentDataManager>(); // Awake() で LoadData() が走る
        }
        // チュートリアル完了済みなら即 Lobby へリダイレクト
        if (PersistentDataManager.Instance.TutorialFinished)
        {
            SceneManager.LoadScene("Lobby");
        }
    }

    public void ToLobby()
    {
        SceneManager.LoadSceneAsync("Lobby");
        SoundManager.Instance.PlaySound(nextButtonSound);
    }
}
