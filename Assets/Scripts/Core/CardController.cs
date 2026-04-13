using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
// 自分が何のカードなのか記憶し、クリックされたらGameManagerに通知する
[RequireComponent(typeof(Image))]
public class CardController : MonoBehaviour
{
    private CardData myCardData;
    private Image cardImage;

    private Vector3 initialPosition; // 元の位置を記憶
    private int siblingIndex; // 本の重なり順を記憶
    private bool isHovered = false; // 現在ホバー中かどうかの判定

    [SerializeField] private GameObject _cardTooltipPanel;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _flavorText;

    // このカードのデータをセットアップ（設定）するメソッド
    public void Setup(CardData data)
    {
        myCardData = data;
        // Imageコンポーネントを取得して、スプライトを設定
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        cardImage.sprite = myCardData.cardSprite;
        // カード自身のImageはマウスを検知しないようにする
        cardImage.raycastTarget = false;

        // テキストを流し込む
        _descriptionText.text = "説明: "+myCardData.descriptionText;
        _flavorText.text = "フレーバー: "+myCardData.flavorText;
    }
    public void SetSigmaSpeakMode(bool active)
    {
        if(myCardData.sigmaSpeakSprite !=null)
        {
            cardImage.sprite = active ? myCardData.sigmaSpeakSprite : myCardData.cardSprite;
        }
    }
    // カードがクリックされたときに呼ばれるメソッド
    public void HandleClick()
    {
        Debug.Log("クリックされたカード" + myCardData.cardName);
        // GameManagerに「このカードがプレイされようとした」と伝える
        GameManager.Instance.TryPlayCard(myCardData);
    }
    public void SetHover(bool hover)
    {
        if (hover && !isHovered)
        {
            // ホバー開始
            isHovered = true;
            // 実行中のTweenを即時に完了させることで正しい位置を記録できるようにする。
            transform.DOComplete();
            initialPosition = transform.localPosition;
            siblingIndex = transform.GetSiblingIndex();

            transform.DOLocalMoveY(initialPosition.y + 20f, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetAsLastSibling(); // 最前面に表示

            _cardTooltipPanel.SetActive(true); // ツールチップを表示
        }
        else if(!hover && isHovered)
        {
            _cardTooltipPanel.SetActive(false); // ツールチップを非表示
            // ホバー終了
            isHovered = false;
            // 実行中のTweenを即時に停止し、元の位置に戻すTweenを開始する
            transform.DOKill();
            transform.DOLocalMoveY(initialPosition.y, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetSiblingIndex(siblingIndex); // 元の重なり順に戻す
        }
    }
}
