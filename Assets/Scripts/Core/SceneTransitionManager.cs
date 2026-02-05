using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("ボタンの押下サウンド")]
    [SerializeField] private AudioClip nextButtonSound;
    public void ToLobby()
    {
        SceneManager.LoadSceneAsync("Lobby");
        SoundManager.Instance.PlaySound(nextButtonSound);
    }
}
