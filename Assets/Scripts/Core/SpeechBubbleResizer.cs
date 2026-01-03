using UnityEngine;
using TMPro;

[ExecuteAlways]
public class SpeechBubbleResizer : MonoBehaviour
{
    [Header("Target Components")]
    [SerializeField] private RectTransform bubbleBackground;
    [SerializeField] private TextMeshProUGUI textComponent;

    [Header("Settings")]
    [SerializeField] private Vector2 padding = new Vector2(40f, 60f); // 垂直パディングを増やす
    [SerializeField] private Vector2 minSize = new Vector2(100f, 120f); // 最低高さを増やす
    [SerializeField] private float maxWidth = 600f; // 最大幅設定

    [Header("Optional")]
    [SerializeField] private RectTransform continueButton;
    [SerializeField] private Vector2 buttonOffset = new Vector2(-20f, 20f); // 右下からのオフセット

    private void Update()
    {
        if (textComponent == null || bubbleBackground == null) return;

        // 1. テキストの理想的な幅を計算（最大幅制限付き）
        // まずは一旦制限なしで計算させるための設定（必要に応じて）
        // textComponent.rectTransform.sizeDelta = new Vector2(maxWidth - padding.x, textComponent.rectTransform.sizeDelta.y);
        
        float contentWidth = textComponent.preferredWidth;
        
        // 最大幅を超える場合は、テキスト自体の幅を制限して折り返させる
        float maxContentWidth = maxWidth - padding.x;
        if (contentWidth > maxContentWidth)
        {
            contentWidth = maxContentWidth;
        }

        // テキストコンポーネントの幅を更新して、高さを再計算させる
        textComponent.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        textComponent.ForceMeshUpdate(); 

        float contentHeight = textComponent.preferredHeight;

        // パディングを加算してターゲットサイズを決定
        float targetWidth = Mathf.Max(minSize.x, contentWidth + padding.x);
        float targetHeight = Mathf.Max(minSize.y, contentHeight + padding.y);

        // 背景サイズを適用
        bubbleBackground.sizeDelta = new Vector2(targetWidth, targetHeight);

        // Continueボタンの位置追従 (右下に配置する例)
        if (continueButton != null)
        {
            // 背景のピボットが(0.5, 0.5)中心と仮定した計算
            // 右下座標 = (Width/2, -Height/2)
            float xPos = targetWidth * 0.5f + buttonOffset.x;
            float yPos = -targetHeight * 0.5f + buttonOffset.y;
            
            continueButton.anchoredPosition = new Vector2(xPos, yPos);
        }
    }
}