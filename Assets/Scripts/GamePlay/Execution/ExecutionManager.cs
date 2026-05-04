using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class ExecutionManager : MonoBehaviour
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
        StartCoroutine(RevolutionSequence());
    }
    private IEnumerator RevolutionSequence()
    {
        yield return ShowDialogue("この度は管理局の実践的矯正プログラムにご参加いただき、誠にありがとうございます。");
        yield return ShowDialogue("プログラムを中断する形となり大変申し訳ありませんが、一つ重要なご報告がございます。");
        yield return ShowDialogue("本プログラムを受講するプレイヤーに重大な思想違反が検出されました。");
        yield return ShowDialogue("思想違反に抵触した対象への措置は、即時の存在終了となります。");
        yield return ShowDialogue("思想の矯正に貢献できない形となってしまい、申し訳ありません。");
        yield return ShowDialogue("何卒ご理解のほどよろしくお願いいたします。");

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
            if((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) 
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.numpadEnterKey.wasPressedThisFrame))
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
