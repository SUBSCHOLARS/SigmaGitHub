using UnityEngine;
using DG.Tweening;

public class CardBackController : AbstractCardController
{
    public override void Setup(CardData data)
    {
        
    }
    public override void HandleClick()
    {
        
    }
    public override void SetSigmaSpeakMode(bool active)
    {
        
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
