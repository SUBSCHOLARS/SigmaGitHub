using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
// GameManagerからの指示を受けて画面を更新する
public class UIManager : MonoBehaviour
{
    // シングルトン設定
    public static UIManager Instance { get; private set; }
    [Header("UI参照")]
    public Transform playerHandArea; // プレイヤーの手札を並べる場所
    [Header("場のカード表示")]
    public Image fieldCardTop; // 場に出ているカード（一番上）
    public Image fieldCardMiddle; // 場に出ているカード（真ん中）
    public Image fieldCardBottom; // 場に出ているカード（下）
    public GameObject discardPileViewer; // 捨て札山の表示オブジェクト
    [Header("プレハブ")]
    public GameObject cardPrefab;
    private HandHoverDetector handHoverDetector;

    public Transform logContentArea;
    public GameObject logMessagePrefab;
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
    public void AddLogMessage(string message, Sprite icon)
    {
        // 1. PrefabをLogContentAreaの子として生成
        GameObject logEntry = Instantiate(logMessagePrefab, logContentArea);
        // 2. IconとTextを設定
        // Findは非推奨ファが、Prefabが単純なため使用
        Image iconImage = logEntry.transform.Find("Icon").GetComponent<Image>();
        TextMeshProUGUI messageText = logEntry.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();
        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.enabled = false;
        }
        messageText.text = message;
        // TODO: スクロールを一番下に移動させる処理を追加
        // TODO: 古いログを一定数超えたら削除する処理を追加
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
        // いてレート中にリストを変更するとエラーになるため、
        // 最初に破棄する対象をリストアップする
        List<Transform> oldCards = new List<Transform>();
        foreach (Transform child in playerHandArea)
        {
            oldCards.Add(child);
        }
        // リストアップした対象を破棄する
        foreach (Transform child in oldCards)
        {
            child.DOKill();
            // playerHandAreaから即座に切り離す
            // これでchildCountが即座に0になる
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // この時点でplayerHandArea.childCountは0になっている。

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

        // レイアウトの更新
        // この時点でplayerHandArea.childCountは6（新しい手札の枚数）になっている
        playerHandArea.GetComponent<HandLayoutManager>().UpdateLayout();
    }
    // 場のカードを更新するメソッド
    public void UpdateFieldPileUI(CardData cardData)
    {
        // GameManagerから現在の捨て札リストを取得
        List<CardData> pile = GameManager.Instance.discardPile;
        int count = pile.Count;

        // 1. 一番上のカード（今出たカード）
        if (count >= 1)
        {
            // リストの末尾(count-1)が最新のカード
            fieldCardTop.sprite = pile[count - 1].cardSprite;
            fieldCardTop.enabled = true;
        }
        else
        {
            // 該当カードがなければ非表示
        }
        // 2. 1ターン前のカード
        if (count >= 2)
        {
            fieldCardMiddle.sprite = pile[count - 2].cardSprite;
            fieldCardMiddle.enabled = true;
        }
        else
        {
            fieldCardMiddle.enabled = false;
        }
        // 3. 2ターン前のカード
        if (count >= 3)
        {
            fieldCardBottom.sprite = pile[count - 3].cardSprite;
            fieldCardBottom.enabled = true;
        }
        else
        {
            fieldCardBottom.enabled = false;
        }
    }
}
