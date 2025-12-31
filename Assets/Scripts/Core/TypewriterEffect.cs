using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    [SerializeField] private float defaultSpeed = 0.05f;
    private Coroutine typingCoroutine;
    public bool IsTyping { get; private set; }
    private string currentFullText;
    private TextMeshProUGUI targetText;

    public void ShowText(TextMeshProUGUI textComponent, string text, float speed = -1f)
    {
        if (textComponent == null) return;
        
        targetText = textComponent;
        currentFullText = text;
        float typeSpeed = (speed < 0) ? defaultSpeed : speed;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeTextRoutine(text, typeSpeed));
    }

    public void Skip()
    {
        if (IsTyping && targetText != null)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            targetText.text = currentFullText;
            IsTyping = false;
        }
    }

    private IEnumerator TypeTextRoutine(string text, float speed)
    {
        IsTyping = true;
        targetText.text = "";
        
        // 1文字ずつ表示
        // HTMLタグを考慮する必要がある場合、もう少し複雑になるが、
        // 今回は単純な文字送りとする。タグが含まれるとタグも1文字ずつ出てしまうので注意。
        // リッチテキスト対応版にするなら、一度パースするか、maxVisibleCharactersを使う。
        
        // maxVisibleCharactersを使用する方法（推奨）
        targetText.text = text;
        targetText.maxVisibleCharacters = 0;

        int totalChars = text.Length; // 注: リッチテキストタグがあるとズレるが、TMProのtextInfoを使うと正確
        // 簡易実装として文字数分ループするが、正しくはTMProの解析結果を使う
        
        // TMProがテキストを解析するのを1フレーム待つ
        yield return null; 
        totalChars = targetText.textInfo.characterCount;

        for (int i = 0; i <= totalChars; i++)
        {
            targetText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(speed);
        }

        targetText.maxVisibleCharacters = 99999;
        IsTyping = false;
    }
}
