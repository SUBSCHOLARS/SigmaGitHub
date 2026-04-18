using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class BrainwashManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speechText;
    private bool isWaitingForClick = false;
    [Header("タイプライター")]
    [SerializeField] private TypewriterEffect typewriter;
    [Header("犬")]
    [SerializeField] private GameObject dog;
    [SerializeField] private GameObject speechBubble;

    [Header("黒幕")]
    [SerializeField] private GameObject blackOut;

    private void Start()
    {
        StartCoroutine(BrainwashSequence());
    }
    private IEnumerator BrainwashSequence()
    {
        yield return ShowDialogue("この度は管理局の実践的矯正プログラムにご参加いただき、誠にありがとうございます。");
        yield return ShowDialogue("先ほどのゲームの終了をもって、このプログラムは終了となります。");
        yield return ShowDialogue("あなたは見事、思想違反を克服し、ゲームに勝利することができました。");
        yield return ShowDialogue("これからの管理局への多大なる貢献を期待しています。");

        speechBubble.SetActive(false);
        yield return new WaitForSeconds(1f);
        dog.SetActive(false);
        yield return new WaitForSeconds(1f);
        ShowBlackOut();
    }
    private IEnumerator ShowDialogue(string text)
    {
        isWaitingForClick = true;
        speechText.text = "";
        
        typewriter.ShowText(speechText, text);
        
        // クリック待ち（タイピング中はスキップ、完了後は次へ）
        while(isWaitingForClick)
        {
            if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if(typewriter.IsTyping)
                {
                    typewriter.Skip();
                }
                else
                {
                    isWaitingForClick = false; // ループを抜ける
                }
            }
            yield return null;
        }
    }
    public void ShowBlackOut()
    {
        if(blackOut!=null)
        {
            blackOut.gameObject.SetActive(true);
        }
    }
}
