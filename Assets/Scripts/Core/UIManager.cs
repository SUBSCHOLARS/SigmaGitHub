using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using TMPro;
// GameManagerからの指示を受けて画面を更新する
public class UIManager : MonoBehaviour
{
    // シングルトン設定
    public static UIManager Instance { get; private set; }
    [Header("UI参照")]
    //public Transform playerHandArea; // プレイヤーの手札を並べる場所
    public Transform playerHandContainer;
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
    public TextMeshProUGUI turnIndicatorText; // TurnIndicatorTextをアタッチ
    [Header("エフェクトUI")]
    public GameObject bribeSelectionPanel; // BribeSelectionPanelをアタッチ
    public GameObject targetSelectionPanel; // TargetSelectionPanelをアタッチ
    public TextMeshProUGUI effectResultText; // 結果表示用テキスト
    [Header("検閲・尋問UI")]
    public GameObject surveyPanel; // SurveyPanelをアタッチ
    public TextMeshProUGUI surveyTitleText; // SurveyTitleTextをアタッチ
    public Transform surveyCardDisplayArea; // SuveryCardDisplayAreaをアタッチ
    public TextMeshProUGUI surveyResultValueText; // SurveyResultValueTextをアタッチ
    private HandHoverDetector handHoverDetector;
    [Header("ログ")]
    public Transform logContentArea;
    public GameObject logMessagePrefab;
    public ScrollRect logScrollRect; // ログのスクロールビュー
    [Header("操作UI")]
    public Button drawButton; // DrawButton
    private Image playerHandRaycaster; // Player_HandContainerのImage（透明な壁）
    [Header("勝利演出")]
    public GameObject winnerPanel;
    public TextMeshProUGUI winnerText;
    public GameObject winButton;
    public CanvasGroup winButtonCanvasGroup; // 点滅アニメーション用
    public GameObject trendRideAlertPanel;
    public TextMeshProUGUI trendRideAlertText;
    private Sequence winButtonAnimation; // アニメーション制御用
    [Header("ゲーム情報")]
    public TextMeshProUGUI roundText; // RoundTextをアタッチ
    public TextMeshProUGUI playerScoreText; // PlayerScoreTextをアタッチ
    public TextMeshProUGUI cpu1ScoreText; // CPU1ScoreTextをアタッチ
    public TextMeshProUGUI cpu2ScoreText; // CPU2ScoreTextをアタッチ
    public TextMeshProUGUI currentTrendText; // CurrentTrendTextをアタッチ
    public TextMeshProUGUI yourTrendText; // YourTrendTextをアタッチ
    [Header("汎用")]
    public GameObject continueButton;

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
        if (playerHandContainer != null)
        {
            handHoverDetector = playerHandContainer.GetComponent<HandHoverDetector>();
            playerHandRaycaster = playerHandContainer.GetComponent<Image>();
            if (handHoverDetector == null)
            {
                Debug.LogError("Player_HandContainerにHandHoverDetectorコンポーネントがアタッチされていません!");
            }
            if(playerHandRaycaster==null)
            {
                Debug.LogError("Player_HandCongainerにImage（透明な壁）がありません!");
            }
        }
        else
        {
            Debug.LogError("UIManagerのplayerHandAreaがインスペクタで設定されていません。");
        }
        bribeSelectionPanel?.SetActive(false);
        targetSelectionPanel?.SetActive(false);
        winnerPanel?.SetActive(false);
        trendRideAlertPanel?.SetActive(false);
        continueButton?.SetActive(false);
        surveyPanel?.SetActive(false);
        surveyResultValueText?.gameObject.SetActive(false);
        // 勝利確認ボタンの初期設定
        // CanvasGroupを取得
        winButtonCanvasGroup = winButton?.GetComponent<CanvasGroup>();
        winButton?.SetActive(false);
    }
    public void ShowBribeSelectionUI()
    {
        bribeSelectionPanel.SetActive(true);
    }
    public void HideBribeSelectionUI()
    {
        bribeSelectionPanel.SetActive(false);
    }
    public void ShowTargetSelectionUI()
    {
        targetSelectionPanel.SetActive(true);
    }
    public void HideTargetSelectionUI()
    {
        targetSelectionPanel.SetActive(false);
    }
    // プレイヤーの操作UIの有効/無効を切り替える
    public void SetPlayerControlsActive(bool isActive)
    {
        // 手札の「透明な壁」の検知をON/OFF
        if (playerHandRaycaster != null)
        {
            playerHandRaycaster.raycastTarget = isActive;
        }
        // ドローボタンの操作可否をON/OFF
        if(drawButton!=null)
        {
            drawButton.interactable = isActive;
            // もし非アクティブにする際、ボタンがホバーで光ったままなら今日背的に戻す
            if (!isActive && drawButton.animator != null)
            {
                // ボタンのハイライト状態を強制的にNormalに戻す
                drawButton.animator.Play("Normal");
            }
            // 手札のホバー検出もON/OFF
            if (handHoverDetector != null)
            {
                handHoverDetector.enabled = isActive;
                if (!isActive)
                {
                    // 非アクティブにする際、ホバー中だったカードを元に戻す
                    handHoverDetector.OnPointerExit(null);
                }
            }
        }
    }
    // 検閲（Censor）のアニメーションコルーチン
    public IEnumerator ShowCensorAnimation(Player targetPlayer)
    {
        // 1. 準備
        surveyTitleText.text = "CENSOR";
        surveyPanel.SetActive(true);

        CardData randomCard = null;
        if(targetPlayer.hand.Count>0)
        {
            // ターゲットの手札からランダムに一枚選ぶ
            randomCard=targetPlayer.hand[Random.Range(0, targetPlayer.hand.Count)];
        }
        // ターゲットの手札を震わせる
        Transform targetHand=GetHandContainerForPlayer(targetPlayer);
        if(targetHand!=null)
        {
            // 0.5秒間、強さ10、振動数20で震わせる
            targetHand.DOShakePosition(0.5f, new Vector3(10f, 10f, 0), 20);
        }
        // 2. 演出（ターゲットの手札を振るわせるなど）
        yield return new WaitForSeconds(0.5f); // 演出のためのタメ
        if(randomCard==null)
        {
            // ログと結果表示
            string msg=$"{targetPlayer.playerName}の手札は0枚です。";
            AddLogMessage(msg, null);
            StartCoroutine(ShowEffectResult(msg));
        }
        else
        {
            // 3. カードを表向きに生成
            GameObject cardObj=Instantiate(cardPrefab, surveyCardDisplayArea);
            cardObj.GetComponent<CardController>().Setup(randomCard);
            // マウス操作を無効化
            cardObj.GetComponent<Image>().raycastTarget=false;
            // 4. ログ
            string msg=$"{targetPlayer.playerName}の手札[{randomCard.cardName}]を検閲";
            AddLogMessage(msg, null); // TODO: 検閲アイコンを渡す
        }
        // 5. 表示
        yield return new WaitForSeconds(2.5f);
        // 6. クリーンアップ
        foreach(Transform child in surveyCardDisplayArea)
        {
            Destroy(child.gameObject);
        }
        surveyPanel.SetActive(false);
    }
    // 尋問（Interrogate）のアニメーションコルーチン
    public IEnumerator ShowInterrogateAnimation(Player targetPlayer)
    {
        // 1. 準備
        surveyTitleText.text="INTERROGATE";
        surveyPanel.SetActive(true);
        int maxVal=int.MinValue;
        bool isHandEmpty=true;
        if(targetPlayer.hand.Count>0)
        {
            isHandEmpty=false;
            // 最大価値のカードを探す
            foreach(CardData card in targetPlayer.hand)
            {
                if(card.handValue>maxVal)
                {
                    maxVal=card.handValue;
                }
            }
        }
        // ターゲットの手札を震わせる
        Transform targetHand=GetHandContainerForPlayer(targetPlayer);
        if(targetHand!=null)
        {
            // 0.5秒間、強さ10、振動数20で震わせる
            targetHand.DOShakePosition(0.5f, new Vector3(10f, 10f, 0), 20);
        }
        // 2. 演出
        yield return new WaitForSeconds(0.5f); // 演出のためのタメ
        string msg;
        if(isHandEmpty)
        {
            // ログと結果表示
            msg=$"{targetPlayer.playerName}の手札は0枚です";
            surveyResultValueText.text="HAND: 0";
            surveyResultValueText.gameObject.SetActive(true);
        }
        else
        {
            // 3. 最大価値のカードの数価を表示
            surveyResultValueText.text=$"MAX VALUE: \n{maxVal}";
            surveyResultValueText.gameObject.SetActive(true);
            // 4. ログ表示
            msg=$"{targetPlayer.playerName}の最大の手札価値は[{maxVal}]です";
        }
        AddLogMessage(msg, null);
        // 5. 表示
        yield return new WaitForSeconds(2.5f);
        // 6. クリーンアップ
        surveyResultValueText.gameObject.SetActive(false);
        surveyPanel.SetActive(false);
    }
    // 結果を一定時間表示するコルーチンも追加
    public IEnumerator ShowEffectResult(string message)
    {
        effectResultText.text = message;
        effectResultText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.0f); // 2秒間表示
        effectResultText.gameObject.SetActive(false);
    }
    public void AddLogMessage(string message, Sprite icon)
    {
        // 1. PrefabをLogContentAreaの子として生成
        GameObject logEntry = Instantiate(logMessagePrefab, logContentArea);
        // 2. IconとTextを設定
        // Findは非推奨だが、Prefabが単純なため使用
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
        StartCoroutine(ScrollToBottom());
    }
    // ログを一番下にスクロールさせるコルーチン
    public IEnumerator ScrollToBottom()
    {
        // 1フレーム待機して、レイアウトが更新されるのを待つ
        yield return new WaitForEndOfFrame();
        if (logScrollRect != null)
        {
            // verticalNormalizedPositionはoが一番下
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    // ログをリセットするメソッド
    public void ResetLog()
    {
        List<Transform> oldLogs = new List<Transform>();
        if (logContentArea == null)
        {
            return;
        }
        foreach (Transform child in logContentArea)
        {
            oldLogs.Add(child);
        }
        foreach(Transform child in oldLogs)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // 必要であれば「Round X Start」のようなログをAddLogMessageで追加
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
        // 表示する枚数の上限を設定
        int visualCardCount = Mathf.Min(deckCount, 70);
        // 2. 山札の枚数分、1ピクセルずつずらして生成
        for(int i=0; i<visualCardCount; i++)
        {
            GameObject cardBack = Instantiate(cardBackPrefab, deckVisualContainer);
            // 1ピクセルずつY方向にずらす
            float xOffset = i * 0.15f; // 0.15ピクセルずつ下へ
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
        // 手札を破棄する前に、ホバー検出器の参照をリセット
        if(handHoverDetector!=null)
        {
            handHoverDetector.ResetHover();
        }
        // 1. まず手札を全削除してリセット
        // イテレート中にリストを変更するとエラーになるため、
        // 最初に破棄する対象をリストアップする
        List<Transform> oldCards = new List<Transform>();
        foreach (Transform child in playerHandContainer)
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
            // プレハブをplayerHandContainerの子として生成
            GameObject newCardObj = Instantiate(cardPrefab, playerHandContainer);
            // CardControllerを取得して、カード情報を設定
            CardController cardController = newCardObj.GetComponent<CardController>();
            cardController.Setup(cardData);
            // Detectorのリストに新しいカードを追加
            handHoverDetector.cardsInHand.Add(cardController);
        }

        // レイアウトの更新
        // この時点でplayerHandContainer.childCountは6（新しい手札の枚数）になっている
        playerHandContainer.GetComponent<HandLayoutManager>().UpdateLayout();

        // プレイヤーの手札合計値を計算して表示
        if(yourTrendText!=null)
        {
            // GameManagerに計算を依頼
            int handValue = GameManager.Instance.GetHandValue(playerHand);
            yourTrendText.text = $"HAND: {handValue}";
        }

        // 3. CPUの手札更新(裏向きで更新)
        List<Player> players = GameManager.Instance.players;
        if (players.Count >= 3) // 3人以上いるか確認
        {
            // [1]番目がCPU1、[2]番目がCPU2だと仮定
            // 通常の裏向き更新を呼ぶ
            UpdateCPUHandVisuals(players[1], cpu1HandContainer, false, null);
            UpdateCPUHandVisuals(players[2], cpu2HandContainer, false, null);
        }
    }
    // CPUの手札ビジュアルを生成するメソッド
    public void UpdateCPUHandVisuals(Player cpu, Transform container, bool reveal, List<CardData> handData)
    {
        List<Transform> oldCards = new List<Transform>();
        // 1. 古いカードバックを全て削除
        foreach (Transform child in container)
        {
            oldCards.Add(child);
        }
        foreach (Transform child in oldCards)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // revealフラグに応じて、枚数を手札データから取るか、CPUの手札数から取るか変更
        int childCount = (reveal && handData != null) ? handData.Count : cpu.hand.Count;
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
            GameObject cardObj;
            if (reveal && handData != null)
            {
                // 表向きで生成
                cardObj = Instantiate(cardPrefab, container);
                CardController cardController = cardObj.GetComponent<CardController>();
                // カードデータを設定
                cardController.Setup(handData[i]);
            }
            else
            {
                cardObj = Instantiate(cardBackPrefab, container);
            }
            RectTransform rect = cardObj.GetComponent<RectTransform>();

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
    public IEnumerator ShowTurnAnimation(string playerName, int playerIndex)
    {
        // 1. 枠線を光らせる（UpdateTurnIndicatorを流用）
        Image targetGlow = null;
        if (playerIndex == 0) targetGlow = playerTurnGlow;
        else if (playerIndex == 1) targetGlow = cpu1TurnGlow;
        else if (playerIndex == 2) targetGlow = cpu2TurnGlow;

        Sequence glowSequence = DOTween.Sequence();
        if (targetGlow != null)
        {
            targetGlow.enabled = true;
            // 枠線の色をキャッシュ（色情報が失われないように）
            Color glowColor = targetGlow.color;
            glowSequence.AppendCallback(() => targetGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f)) // 瞬時に非表示
                        .AppendInterval(0.3f) // 0.3秒待機
                        .AppendCallback(() => targetGlow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 1f)) // 瞬時に表示
                        .AppendInterval(0.3f) // 0.3秒待機
                        .SetLoops(3); // ループ

        }
        if (turnIndicatorText != null)
        {
            // 2. テキストを表示して点滅させる
            turnIndicatorText.DOKill();
            turnIndicatorText.enabled = true;
            turnIndicatorText.text = $"-{playerName}- TURN";

            //DOTweenで点滅
            Sequence textSequence = DOTween.Sequence();
            textSequence.AppendCallback(() => turnIndicatorText.alpha = 0f) // 瞬時に非表示
                        .AppendInterval(0.3f) // 0.3秒待機
                        .AppendCallback(() => turnIndicatorText.alpha = 1f) // 瞬時に表示
                        .AppendInterval(0.3f) // 0.3秒待機
                        .SetLoops(3); // ループ

            // アニメーションの完了を待つ
            yield return textSequence.WaitForCompletion();
        }
        HideTurnAnimation();

    }
    // ターンアニメーション非表示
    public void HideTurnAnimation()
    {
        if (turnIndicatorText != null)
        {
            // 1. 点滅を止めて非表示に
            turnIndicatorText.DOKill(); // アニメーション停止
            turnIndicatorText.enabled = false;
            turnIndicatorText.alpha = 1f; // Alphaをリセット
        }

        // 2. 枠線を全て消す
        if (playerTurnGlow != null)
        {
            playerTurnGlow.DOKill();
            playerTurnGlow.enabled = false;
            playerTurnGlow.color = new Color(playerTurnGlow.color.r, playerTurnGlow.color.g, playerTurnGlow.color.b, 1f);
        }
        if (cpu1TurnGlow != null)
        {
            cpu1TurnGlow.DOKill();
            cpu1TurnGlow.enabled = false;
            cpu1TurnGlow.color = new Color(cpu1TurnGlow.color.r, cpu1TurnGlow.color.g, cpu1TurnGlow.color.b, 1f);
        }
        if (cpu2TurnGlow != null)
        {
            cpu2TurnGlow.DOKill();
            cpu2TurnGlow.enabled = false;
            cpu2TurnGlow.color = new Color(cpu2TurnGlow.color.r, cpu2TurnGlow.color.g, cpu2TurnGlow.color.b, 1f);
        }
    }
    // 勝利演出の本体
    public void ShowWinnerAnimation(bool show, List<Player> winners, WinType winType, int winningHandValue)
    {
        if (winnerPanel == null)
        {
            return;
        }
        if (show)
        {
            string winnerNames = "";
            foreach (Player player in winners)
            {
                winnerNames += player.playerName + "\n"; // 複数勝利対応
            }
            // 表示する内容をリッチにする
            string winReason = "";
            if (winType == WinType.TrendRide)
            {
                winReason = "TREND RIDE";
            }
            else
            {
                winReason = "SELF MATCH";
            }
            winnerText.text = $"{winReason}\n" +
                            $"WINNER: {winnerNames}\n" +
                            $"HAND VALUE: {winningHandValue}";
            winnerPanel.SetActive(true);
        }
        else
        {
            winnerPanel.SetActive(false);
        }
    }
    // 全員の手札を公開する（CPUの手札を表にする）
    public void RevealAllHands()
    {
        // TODO: UpdateCPUHandVisualsを改造し、
        // vardBackPrefabではなく、cardPrefabを使い、
        // CPUの手札を全て表向きに表示する処理を実装する
        List<Player> players = GameManager.Instance.players;
        if(players.Count>=3)
        {
            // UpdateCPUHandVisualsをreveal=trueで呼び出す
            UpdateCPUHandVisuals(players[1], cpu1HandContainer, true, players[1].hand);
            UpdateCPUHandVisuals(players[2], cpu2HandContainer, true, players[2].hand);
        }
        Debug.Log("全員の手札公開!");
    }
    // スコアボード更新（ダミー）
    public void UpdateScoreboard(List<Player> players)
    {
        // TODO: スコアボードUIに各プレイヤーのtotalPointsを反映する
        if (players.Count >= 3)
        {
            playerScoreText.text = $"P1 [{players[0].playerName}]: \n{players[0].totalPoints} CR";
            cpu1ScoreText.text = $"P2 [{players[1].playerName}]: \n{players[1].totalPoints} CR";
            cpu2ScoreText.text = $"P3 [{players[2].playerName}]: \n{players[2].totalPoints} CR";
        }
        Debug.Log($"スコア更新: P1({players[0].totalPoints}), P2({players[1].totalPoints}), P3({players[2].totalPoints})");
    }
    // ラウンド数更新メソッド
    public void UpdateRoundText(int round)
    {
        if(roundText!=null)
        {
            roundText.text = $"ROUND {round}";
        }
    }
    // ゲーム終了演出
    public void ShowGameEndAnimation(bool show, Player winner)
    {
        if (winnerPanel == null)
        {
            return;
        }
        if (show)
        {
            winnerText.text = $"OVERALL WINNER:\n{winner.playerName}";
            winnerPanel.SetActive(true);
        }
        else
        {
            winnerPanel.SetActive(false);
        }
    }
    // 場のトレンドを更新するメソッド
    public void UpdateCurrentTrend(int trendValue)
    {
        if (currentTrendText != null)
        {
            currentTrendText.text = $"TREND: {trendValue}";
        }
    }
    // 勝利確認ボタンを表示/非表示にするメソッド
    public void ShowWinButton(bool show)
    {
        if (winButton != null)
        {
            // 既存のアニメーションを停止
            winButtonAnimation?.Kill();
            winButton.SetActive(show);
            if (show)
            {
                // レトロゲーム風の点滅アニメーション
                // TODO: ピコピコ音追加
                // CanvasGroupのAlpha（透明度）を1.0 => 0 => 1.0と往復させる
                if (winButtonCanvasGroup != null)
                {
                    winButtonCanvasGroup.alpha = 1f;
                    winButtonAnimation = DOTween.Sequence()
                        .Append(winButtonCanvasGroup.DOFade(0f, 0.1f).SetEase(Ease.InOutQuad))
                        .Append(winButtonCanvasGroup.DOFade(1f, 0.1f).SetEase(Ease.InOutQuad))
                        .SetLoops(-1); // 無限ループ
                }
            }
            else
            {
                // 非表示にする際はアルファ値を元に戻す
                if (winButtonCanvasGroup != null)
                {
                    winButtonCanvasGroup.alpha = 1f;
                }
            }
        }
    }
    // WinButtonがクリックされたときの処理
    public void OnWinButtonPress()
    {
        GameManager.Instance.PlayerConfirmWin();
    }
    // トレンドライドアラートを表示するメソッド
    public void ShowTrendRideAlert(bool show, List<Player> winners, Player actionPlayer)
    {
        if (trendRideAlertPanel == null)
        {
            return;
        }
        if (show)
        {
            string winnerNames = "";
            foreach (Player player in winners)
            {
                winnerNames += player.playerName + " ";
            }
            trendRideAlertText.text = $"--- TREND RIDE ---\n{actionPlayer.playerName}'s action causes\n{winnerNames}to WIN!";
            // TODO: ピコピコ音追加
            trendRideAlertPanel.SetActive(true);
        }
        else
        {
            trendRideAlertPanel.SetActive(false);
        }
    }
    // ターゲットプレイヤーのHand Container Transformを取得するヘルパーメソッド
    private Transform GetHandContainerForPlayer(Player targetPlayer)
    {
        // プレイヤーIDで判別
        if(targetPlayer.id==PlayerID.Player)
        {
            return playerHandContainer;
        }
        // CPUの場合はGameManagerのリストのインデックスで判別
        // プレイヤーが0番目、CPU1が1番目、CPU2が2番目と仮定
        if(GameManager.Instance.players.Count>2)
        {
            if(targetPlayer==GameManager.Instance.players[1])
            {
                return cpu1HandContainer;
            }
            else if(targetPlayer==GameManager.Instance.players[2])
            {
                return cpu2HandContainer;
            }
        }
        Debug.Log("GetHandContainerForPlayer: 該当するHand Containerが見つかりませんでした");
        return null;
    }
    // 汎用的なクリック待ちUIの表示
    public void ShowContinueButton(bool show)
    {
        continueButton?.SetActive(show);
    }
    // continueButtonオブジェクトのButtonコンポーネントから呼ばれる
    public void OnContinuePromptClick()
    {
        GameManager.Instance.OnContinueClicked();
    }
}
