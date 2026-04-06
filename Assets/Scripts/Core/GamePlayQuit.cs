using UnityEngine;

public class GamePlayQuit : MonoBehaviour
{
    [Header("ボタンの押下サウンド")]
    [SerializeField] private AudioClip nextButtonSound;
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
