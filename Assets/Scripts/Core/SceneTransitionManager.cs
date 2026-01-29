using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public void ToLobby()
    {
        SceneManager.LoadSceneAsync("Lobby");
    }
}
