using UnityEngine;
using UnityEngine.UI;
// 自分が何のカードなのか記憶し、クリックされたらGameManagerに通知する
[RequireComponent(typeof(Image), typeof(Button))]
public class CardController : MonoBehaviour
{
    private CardData myCardData;
    private Image cardImage;
    private Button button;

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
        // GameManager.Instance.TryPlayCard(myCardData);
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
