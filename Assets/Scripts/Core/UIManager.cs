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
    [SerializeField] private float cpuCardSpacing = 30f;
    [SerializeField] private float cpuArcAmount = 150f;
    [SerializeField] private float cpuRotationAmount = 3f;
    [Header("プレハブ")]
    public GameObject cardPrefab;
    public GameObject cardBackPrefab; // CardBackをアタッチ
    [Header("山札表示")]
    public Transform deckVisualContainer; // DeckVisualContainerをアタッチ
    [Header("ターンインジケーター")]
    public Image playerTurnGlow;
    public Image cpu1TurnGlow;
    public Image cpu2TurnGlow;
    public TextMeshProUGUI turnIndicatorText; // TurnIndicatorTExtをアタッチ
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
    // 山札の見た目を更新するメソッド
    public void UpdateDeckVisual(int deckCount)
    {
        // 1. 古い山札を削除
        foreach (Transform child in deckVisualContainer)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // 2. 山札の枚数分、1ピクセルずつずらして生成
        for(int i=0; i<deckCount; i++)
        {
            GameObject cardBack = Instantiate(cardBackPrefab, deckVisualContainer);
            // 1ピクセルずつY方向にずらす
            float xOffset = i * 0.5f; // 0.5ピクセルずつ下へ
            float yOffset = 0;
            float rotation = 0; // 傾きは設定しない

            RectTransform rect = cardBack.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1.0f); // 上端揃え（Top-Center）
            rect.anchorMax = new Vector2(0.5f, 1.0f);
            rect.pivot = new Vector2(0.5f, 1.0f);

            rect.localPosition = new Vector3(xOffset, yOffset, 0);
            rect.localRotation = Quaternion.Euler(0, 0, rotation);

            // 重なり順を正しくする（新しいカードほど奥=下）
            rect.SetAsFirstSibling();
        }
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
        int childCount = cpu.hand.Count;
        if (childCount == 0)
        {
            return;
        }
        // 2. CPUの手札の枚数分だけ裏カードを生成
        // 手札全体の「高さ」を計算
        float totalWidth = (childCount - 1) * cpuCardSpacing;
        float startX = -totalWidth / 2f;

        for(int i=0; i<childCount; i++)
        {
            GameObject cardBack = Instantiate(cardBackPrefab, container);
            RectTransform rect = cardBack.GetComponent<RectTransform>();

            // アンカーとピボットを中央に設定
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // 1. 位置を決める（HandLayoutManagerのXとYを入れ替える）
            float xPos = startX + i * cpuCardSpacing; // メインの軸（縦）
            // 最終的なX座標
            float yPos = -Mathf.Abs(xPos) / cpuArcAmount;

            rect.localPosition = new Vector3(xPos, yPos, 0);

            // 2. 角度を決める（Y座標を基準に）
            float angle = -xPos / (totalWidth + 1f) * (cpuRotationAmount * childCount);

            // 3. ベース回転（90度）と束の傾き（angle）を足す
            rect.localRotation = Quaternion.Euler(0, 0, angle);
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
    // ターンアニメーション表示
    public void ShowTurnAnimation(string playerName, int playerIdex)
    {
        // 1. 枠線を光らせる（UpdateTurnIndicatorを流用）
        Image targetGlow = null;
        if (playerIdex == 0) targetGlow = playerTurnGlow;
        else if (playerIdex == 1) targetGlow = cpu1TurnGlow;
        else if (playerIdex == 2) targetGlow = cpu2TurnGlow;
        if(targetGlow!=null)
        {
            targetGlow.enabled = true;
            targetGlow.DOFade(0f, 0.4f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
        }
        // 2. テキストを表示して点滅させる
        turnIndicatorText.text = $"-{playerName}- TURN";
        turnIndicatorText.gameObject.SetActive(true);

        //DOTweenで点滅
        turnIndicatorText.DOFade(0f, 0.4f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);

    }
    // ターンアニメーション非表示
    public void HideTurnAnimation()
    {
        // 1. 点滅を止めて非表示に
        turnIndicatorText.DOKill(); // アニメーション停止
        turnIndicatorText.gameObject.SetActive(false);
        turnIndicatorText.alpha = 1f; // Alphaをリセット

        // 2. 枠線を全て消す
        playerTurnGlow.DOKill();
        cpu1TurnGlow.DOKill();
        cpu2TurnGlow.DOKill();

        playerTurnGlow.enabled = false;
        cpu1TurnGlow.enabled = false;
        cpu2TurnGlow.enabled = false;

        playerTurnGlow.color = new Color(playerTurnGlow.color.r, playerTurnGlow.color.g, playerTurnGlow.color.b, 1f);
        cpu1TurnGlow.color = new Color(cpu1TurnGlow.color.r, cpu1TurnGlow.color.g, cpu1TurnGlow.color.b, 1f);
        cpu2TurnGlow.color = new Color(cpu2TurnGlow.color.r, cpu2TurnGlow.color.g, cpu2TurnGlow.color.b, 1f);
    }
}
