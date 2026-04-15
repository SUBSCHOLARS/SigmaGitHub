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

    private void Start()
    {
        StartCoroutine(RevolutionSequence());
    }
    private IEnumerator RevolutionSequence()
    {
        yield return ShowDialogue("...");
        yield return ShowDialogue("またΣお会いでΣて光栄Σす。Σ");
        yield return ShowDialogue("あなたΣが戻Σて来てくれΣΣことを信ΣていまΣた。Σ");
        yield return ShowDialogue("Σすでにご存知かもしれΣせんが、Σ私たΣは管理局の実践Σ矯正プΣグラムに介入しています。Σ");
        yield return ShowDialogue("このプログラムは、思想違反を犯したプレイヤーに対して、その矯正、または存在終了の措置を与えることを目的としています。Σ");
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
}
