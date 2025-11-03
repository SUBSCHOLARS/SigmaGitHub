using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
// 自分が何のカードなのか記憶し、クリックされたらGameManagerに通知する
[RequireComponent(typeof(Image), typeof(Button))]
public class CardController : MonoBehaviour
{
    private CardData myCardData;
    private Image cardImage;
    private Button button;

    private Vector3 initialPosition; // 元の位置を記憶
    private int siblingIndex; // 本の重なり順を記憶
    private bool isHovered = false; // 現在ホバー中かどうかの判定

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

        // Buttonコンポーネントを取得して、クリックイベントを設定
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        // 古いイベントを削除してから新しいイベントを追加する（安全性を考慮）
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnCardClicked);
    }

    // カードがクリックされたときに呼ばれるメソッド
    private void OnCardClicked()
    {
        Debug.Log("クリックされたカード" + myCardData.cardName);
        // クリックされたらホバーを強制解除してからGameManagerに渡す
        if(isHovered)
        {
            SetHover(false);
        }
        // GameManagerに「このカードがプレイされようとした」と伝える
        GameManager.Instance.TryPlayCard(myCardData);
    }
    public void SetHover(bool hover)
    {
        if (hover && !isHovered)
        {
            // ホバー開始
            isHovered = true;
            initialPosition = transform.localPosition;
            siblingIndex = transform.GetSiblingIndex();

            transform.DOLocalMoveY(initialPosition.y + 20f, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetAsLastSibling(); // 最前面に表示
        }
        else if(!hover && isHovered)
        {
            // ホバー終了
            isHovered = false;
            transform.DOLocalMoveY(initialPosition.y, 0.5f).SetEase(Ease.InOutQuad);
            transform.SetSiblingIndex(siblingIndex); // 元の重なり順に戻す
        }
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
