using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BribeCardController : AbstractCardController
{
    public override void Setup(CardData data)
    {
        myCardData = data;
         // Imageコンポーネントを取得して、スプライトを設定
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        cardImage.sprite = myCardData.cardSprite;
        cardImage.raycastTarget = false;

    }
    public override void HandleClick()
    {
        if(!isPlayerOwned) return;
        Debug.Log("クリックされたBribeカード" + myCardData.cardName);
        if(GameManager.Instance != null)
        {
            GameManager.Instance.PlayerSelectBribeTrend(myCardData.numberValue);
        }
        else
        {
            FreeGameManager.Instance.PlayerSelectBribeTrend(myCardData.numberValue);
        }
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
            // 実行中のTweenを即時に完了させることで正しい位置を記録できるようにする。
            transform.DOComplete();
            initialPosition = transform.localPosition;
            siblingIndex = transform.GetSiblingIndex();

            transform.DOLocalMoveY(initialPosition.y + 20f, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetAsLastSibling(); // 最前面に表示
        }
        else if(!hover && isHovered)
        {
            // ホバー終了
            isHovered = false;
            // 実行中のTweenを即時に停止し、元の位置に戻すTweenを開始する
            transform.DOKill();
            transform.DOLocalMoveY(initialPosition.y, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetSiblingIndex(siblingIndex); // 元の重なり順に戻す
        }
    }
}