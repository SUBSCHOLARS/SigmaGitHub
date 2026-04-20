using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingSceneTransion : MonoBehaviour
{
    public void ToLobby()
    {
        SceneManager.LoadSceneAsync("Lobby");
    }
}
