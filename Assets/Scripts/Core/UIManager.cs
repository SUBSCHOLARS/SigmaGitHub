using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
// GameManagerからの指示を受けて画面を更新する
public class UIManager : MonoBehaviour
{
    // シングルトン設定
    public static UIManager Instance { get; private set; }
    [Header("UI参照")]
    public Transform playerHandArea; // プレイヤーの手札を並べる場所
    public Image fieldCardImage; // 場に出ているカードを表示するImage
    [Header("プレハブ")]
    public GameObject cardPrefab;
    private HandHoverDetector handHoverDetector;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (playerHandArea != null)
        {
            handHoverDetector = playerHandArea.GetComponent<HandHoverDetector>();
            if (handHoverDetector == null)
            {
                Debug.LogError("PlayerHandAreaにHandHoverDetectorコンポーネントがアタッチされていません!");
            }
        }
        else
        {
            Debug.LogError("UIManagerのplayerHandAreaがインスペクタで設定されていません。");
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
    // プレイヤーの手札を画面に表示するメソッド
    public void UpdatePlayerHandUI(List<CardData> hand)
    {
        // 1. まず手札を全削除してリセット
        foreach (Transform child in playerHandArea)
        {
            Destroy(child.gameObject);
        }
        // Detectorのリストもリセット
        handHoverDetector.cardsInHand.Clear();

        // 2. 新しい手札を生成
        foreach (CardData cardData in hand)
        {
            // プレハブをplayerHandAreaの子として生成
            GameObject newCardObj = Instantiate(cardPrefab, playerHandArea);
            // CardControllerを取得して、カード情報を設定
            CardController cardController = newCardObj.GetComponent<CardController>();
            cardController.Setup(cardData);
            // Detectorのリストに新しいカードを追加
            handHoverDetector.cardsInHand.Add(cardController);
        }

        playerHandArea.GetComponent<HandLayoutManager>().UpdateLayout();
    }
    // 場のカードを更新するメソッド
    public void UpdateFieldCardUI(CardData cardData)
    {
        if (cardData != null)
        {
            fieldCardImage.sprite = cardData.cardSprite;
            fieldCardImage.enabled = true;
        }
        else
        {
            // 念のため（場が空の場合など）
            fieldCardImage.enabled = false;
        }
    }
}
