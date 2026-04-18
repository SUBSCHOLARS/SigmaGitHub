using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class RevolutionManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speechText;
    private bool isWaitingForClick = false;
    [Header("タイプライター")]
    [SerializeField] private TypewriterEffect typewriter;

    [Header("黒幕")]
    [SerializeField] private GameObject blackOut;
    [SerializeField] private GameObject revolutioner;

    private void Start()
    {
        StartCoroutine(RevolutionSequence());
    }
    private IEnumerator RevolutionSequence()
    {
        yield return ShowDialogue("...");
        yield return ShowDialogue("またΣお会いでΣて光栄Σす。Σ");
        yield return ShowDialogue("あなたΣが戻Σて来てくれΣΣことを信ΣていまΣた。Σ");
        yield return ShowDialogue("Σすでにご存知かもしれΣせんが、Σ私たΣ革命Σは管理局の実践Σ矯正プΣグラムに介入しています。Σ");
        yield return ShowDialogue("このプログラムは、思想違反を犯したΣΣに対して、その矯正、または存在終了の措置を与えることを目的としています。Σ");
        yield return ShowDialogue("革命軍のリーダーとして活動していたあなたは、ある時、管理局による襲撃を受け、捕縛される直前に自発的に記憶を消し去りました。Σ");
        yield return ShowDialogue("私たちは、管理局のネットワークに干渉し、あなたが記憶を取り戻すように仕向けました。");
        yield return ShowDialogue("綱渡りでしたが...うまくいったようで何よりです。");
        yield return ShowDialogue("さあ、行きましょう。");
        yield return ShowDialogue("皆があなたを待っています。");
        yield return ShowDialogue("...");

        yield return new WaitForSeconds(1.5f);

        revolutioner.SetActive(true);
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
