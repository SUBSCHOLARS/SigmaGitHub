using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// ホバー処理の司令塔となる。playerHandAreaにアタッチする。
public class HandHoverDetector : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
{
    // UIManagerが手札のリストをここに設定する
    public List<CardController> cardsInHand = new List<CardController>();
    private CardController currentlyHoveredCard = null;
    // マウスがHandPlayArea(透明な壁)の上を移動し続けている間、常に呼ばれる。
    public void OnPointerMove(PointerEventData eventData)
    {
        // マウスに一番近いカードを探す
        CardController closestCard = FindClosestCard(eventData.position);
        if (closestCard != currentlyHoveredCard)
        {
            // 以前ホバーしていたカードがあれば、ホバーを削除
            if (currentlyHoveredCard != null)
            {
                currentlyHoveredCard.SetHover(false);
            }
            // 新しく一番近いカードをホバー
            currentlyHoveredCard = closestCard;
            if (currentlyHoveredCard != null)
            {
                currentlyHoveredCard.SetHover(true);
            }
        }
    }
    // マウスがHandPlayArea(透明な壁)から離れた時に呼ばれる
    public void OnPointerExit(PointerEventData eventData)
    {
        // どのカードもホバーしていない状態にする
        if (currentlyHoveredCard != null)
        {
            currentlyHoveredCard.SetHover(false);
            currentlyHoveredCard = null;
        }
    }
    // マウスのぁ表に地番近いカードを探すロジック
    private CardController FindClosestCard(Vector2 mousePosition)
    {
        CardController closest = null;
        float minDistance = float.MaxValue;
        foreach (CardController card in cardsInHand)
        {
            // カードのスクリーン座標とマウス座標の距離を計算
            float distance = Vector2.Distance(card.transform.position, mousePosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = card;
            }
        }
        return closest;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
