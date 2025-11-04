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
    [Header("CPUの手札表示")]
    public Transform cpu1HandContainer; // CPU1_HandDisplayをアタッチ
    public Transform cpu2HandContainer; // CPU2_HandDisplayをアタッチ
    [Header("CPUの手札表示パラメータ")]
    [SerializeField] private float xFactor = 8f;
    [SerializeField] private float yFactor = -2f;
    [SerializeField] private float rotationAngle = -15f;
    [SerializeField] private float rotationAngleFactor = 4f;
    [Header("プレハブ")]
    public GameObject cardPrefab;
    public GameObject cardBackPrefab; // CardBackをアタッチ
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
    public void UpdateAllHandVisuals()
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
        List<CardData> playerHand = GameManager.Instance.GetPlayerHand();

        // 2. 新しい手札を生成
        foreach (CardData cardData in playerHand)
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

        // 3. CPUの手札更新
        List<Player> players = GameManager.Instance.players;
        if (players.Count >= 3) // 3人以上いるか確認
        {
            // [1]番目がCPU1、[2]番目がCPU2だと仮定
            UpdateCPUHandVisuals(players[1], cpu1HandContainer);
            UpdateCPUHandVisuals(players[2], cpu2HandContainer);
        }
    }
    // CPUの手札ビジュアルを生成するメソッド
    public void UpdateCPUHandVisuals(Player cpu, Transform container)
    {
        // 1. 古いカードバックを全て削除
        foreach (Transform child in container)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // 2. CPUの手札の枚数分だけ裏カードを生成
        for(int i=0; i<cpu.hand.Count; i++)
        {
            // cardBackPrefabをcontainerの子として生成
            GameObject cardBack = Instantiate(cardBackPrefab, container);
            // 3. 重ねて食べ寝るためのずらすと傾きを設定
            float xOffset = i * xFactor; // xFactorずつずらす（正の値で右、負の値で左）
            float yOffset = i * yFactor; // yFactorずつずらす（正の値で上、負の値で下）
            float rotation = rotationAngle + (i * rotationAngleFactor); // rotationAngleから傾きを少しずつ変える

            // RectTransformを取得してアンカーを中央に設定
            RectTransform rect = cardBack.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // 位置と傾きを設定
            rect.localPosition = new Vector3(xOffset, yOffset, 0);
            rect.localRotation = Quaternion.Euler(0, 0, rotation);
        }
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
