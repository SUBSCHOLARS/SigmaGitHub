using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// ホバー処理の司令塔となる。playerHandAreaにアタッチする。
// Monobehaviourの他に、IPointerMoveHandler, IPointerExitHandler, IPointerClickHandlerを実装する。
public class HandHoverDetector : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
{
    // UIManagerが手札のリストをここに設定する
    public List<CardController> cardsInHand = new List<CardController>();
    private CardController currentlyHoveredCard = null;
    // カメラへの参照を追加
    private Camera mainCamera;
    // マウスがHandPlayArea(透明な壁)の上を移動し続けている間、常に呼ばれる。
    public void OnPointerMove(PointerEventData eventData)
    {
        // マウスに一番近いカードを探す（eventData.positionはピクセル座標）
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
    public void OnPointerClick(PointerEventData eventData)
    {
        if(GameManager.Instance.isPlayerInputLocked)
        {
            return;
        }
        if (currentlyHoveredCard != null)
        {
            currentlyHoveredCard.HandleClick();
            // クリックしたカードは存在しなくなるので、参照をクリアする
            currentlyHoveredCard = null;
        }
    }
    // マウスがHandPlayArea(透明な壁)から離れた時に呼ばれる
    public void OnPointerExit(PointerEventData eventData)
    {
        // どのカードもホバーしていない状態にする
        if (currentlyHoveredCard != null)
        {
            // オブジェクトが破棄されていないか安全確認
            if (currentlyHoveredCard.gameObject != null)
            {
                currentlyHoveredCard.SetHover(false);
            }
            currentlyHoveredCard = null;
        }
    }
    // UIManagerから呼び出されるリセット用のメソッド
    public void ResetHover()
    {
        // 参照を強制的に解除する
        currentlyHoveredCard = null;
    }
    // マウスの座標に地番近いカードを探すロジック（座標変換のロジックも含む）
    private CardController FindClosestCard(Vector2 mousePosition)
    {
        CardController closest = null;
        float minDistance = float.MaxValue;
        foreach (CardController card in cardsInHand)
        {
            // カードのスクリーン座標とマウス座標の距離を計算
            // カードのワールド座標（transform.positionをmainCamera.WorldToScreenPointでピクセル座標に変換）
            Vector2 cardScreenPosition = mainCamera.WorldToScreenPoint(card.transform.position);
            float distance = Vector2.Distance(cardScreenPosition, mousePosition);
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
        // CanvasがScreen Space - Cameraなので座標変換のためにカメラが必須
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
