using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
// 自分が何のカードなのか記憶し、クリックされたらGameManagerに通知する
[RequireComponent(typeof(Image), typeof(Button))]
public class CardController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CardData myCardData;
    private Image cardImage;
    private Button button;

    private Vector3 initialPosition; // 元の位置を記憶
    private int siblingIndex; // 本の重なり順を記憶

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
        // GameManagerに「このカードがプレイされようとした」と伝える
        GameManager.Instance.TryPlayCard(myCardData);
    }
    // マウスカーソルがカードの上に乗った時に呼ばれるメソッド
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 本の位置と重なり順を記憶
        initialPosition = transform.localPosition;
        siblingIndex = transform.GetSiblingIndex();

        // 少し上に、一番手前に表示
        transform.DOLocalMoveY(initialPosition.y + 50f, 0.2f); // 50ピクセル上に0.2秒で移動
        transform.SetAsLastSibling(); // 最前面に表示
    }

    // マウスカーソルがカードから離れたときに呼ばれるメソッド
    public void OnPointerExit(PointerEventData eventData)
    {
        // 本の位置と重なり順に戻す
        transform.DOLocalMoveY(initialPosition.y, 0.2f);
        transform.SetSiblingIndex(siblingIndex);
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
