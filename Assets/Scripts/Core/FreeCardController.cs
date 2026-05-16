using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
// 自分が何のカードなのか記憶し、クリックされたらGameManagerに通知する
[RequireComponent(typeof(Image))]
public class FreeCardController : AbstractCardController
{
    [SerializeField] private GameObject _cardTooltipPanel;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _flavorText;
    public Image numValueIcon;

    // このカードのデータをセットアップ（設定）するメソッド
    public override void Setup(CardData data)
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
        // 数値アイコンを設定
        if (numValueIcon != null)
        {
            numValueIcon.sprite = myCardData.numValueIcon;
        }
    }

    // カードがクリックされたときに呼ばれるメソッド
    public override void HandleClick()
    {
        if (!isPlayerOwned) return; // CPUのカードはクリック不可
        Debug.Log("クリックされたカード" + myCardData.cardName);
        // FreeGameManagerに「このカードがプレイされようとした」と伝える
        FreeGameManager.Instance.StartCoroutine(FreeGameManager.Instance.TryPlayCard(myCardData));
    }
    public override void SetSigmaSpeakMode(bool active)
    {
        if(myCardData.sigmaSpeakSprite !=null)
        {
            cardImage.sprite = active ? myCardData.sigmaSpeakSprite : myCardData.cardSprite;
        }
    }
    public override void SetHover(bool hover)
    {
        if (hover && !isHovered)
        {
            // ホバー開始
            isHovered = true;
            // 即時完了
            transform.DOComplete();
            initialPosition = transform.localPosition;
            siblingIndex = transform.GetSiblingIndex();

            transform.DOLocalMoveY(initialPosition.y + 20f, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetAsLastSibling(); // 最前面に表示

            _cardTooltipPanel.SetActive(true); // ツールチップを表示
            _cardTooltipPanel.transform.SetAsLastSibling(); // ツールチップを最前面に表示
        }
        else if(!hover && isHovered)
        {
            _cardTooltipPanel.SetActive(false); // ツールチップを非表示
            // ホバー終了
            isHovered = false;
            // 即時中断
            transform.DOKill();
            transform.DOLocalMoveY(initialPosition.y, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetSiblingIndex(siblingIndex); // 元の重なり順に戻す
        }
    }
}
