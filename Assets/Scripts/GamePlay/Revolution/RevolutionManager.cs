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
        yield return ShowDialogue("ΣすでにごΣ知かもしれΣせんが、Σ私たΣ革命Σは管理Σの実践Σ矯正プΣグラムに介入しています。Σ");
        yield return ShowDialogue("ΣこのプΣグラムは、思想Σ反を犯したΣΣに対して、その矯正、まΣはΣ存在終Σの措置を与えΣことを目的Σしています。Σ");
        yield return ShowDialogue("革Σ軍のリΣダΣとΣて活Σしていたあなたは、ある時、管理局による襲撃を受け、捕ΣされるΣΣ前に自発的に記Σを消し去りました。Σ");
        yield return ShowDialogue("私Σちは、管Σ局のネットワークに干渉し、あなたがΣ憶を取Σ戻すように仕ΣけましたΣ");
        yield return ShowDialogue("綱ΣりでしΣが...うまくいったようで何よりです。");
        yield return ShowDialogue("ΣさあΣ行きΣしょうΣ");
        yield return ShowDialogue("皆ΣあΣたを待っΣいΣすΣ");
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
