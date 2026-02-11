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
    [Header("ターミナルUI")]
    [SerializeField] private GameObject terminalWindow; // TerminalWindowパネル
    [SerializeField] private GameObject unreadBadge; // 未読バッジ
    [SerializeField] private TextMeshProUGUI terminalLogText; // ログを表示する1つの巨大なテキスト
    [SerializeField] private ScrollRect terminalScrollRect; // Scroll View
    [SerializeField] private TMP_InputField commandInput; // コマンド入力欄
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
    [Header("絵柄・数字表示")]
    [SerializeField] private Image sectorIcon; // 絵柄アイコン表示用
    [Header("検閲・尋問カードを出した際のUI")]
    [SerializeField] private Sprite errorSprite; // ?の絵柄アイコン
    [Header("汎用")]
    public GameObject continueButton;
    [Header("スタンプエフェクト制御")]
    public CardStampEffect fieldTopStampEffect;
    [Header("各手札を見せるUI")]
    [SerializeField] private GameObject revealAllHandsPanel;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [Header("オーディオ")]
    [SerializeField] private AudioClip nextButtonSound;
    [Header("ゲームごとのゴール表示テキスト")]
    [SerializeField] private TextMeshProUGUI gameGoalText;
    [Header("処刑エンド用オブジェクト")]
    [SerializeField] private GameObject bloodObject;
    [SerializeField] private Image blackOut;
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
        {
            Debug.LogError("UIManagerのplayerHandAreaがインスペクタで設定されていません。");
        }
        
        // CPUの手札エリアにもHoverDetectorを仕込む
        SetupCPUHandHover(cpu1HandContainer);
        SetupCPUHandHover(cpu2HandContainer);

        if(bribeSelectionPanel!=null)
        {
            bribeSelectionPanel.SetActive(false);
        }
        if(targetSelectionPanel!=null)
        {
            targetSelectionPanel.SetActive(false);
        }
        if(winnerPanel!=null)
        {
            winnerPanel.SetActive(false);
        }
        if(trendRideAlertPanel!=null)
        {
            trendRideAlertPanel.SetActive(false);
        }
        if(continueButton!=null)
        {
            continueButton.SetActive(false);
        }
        if(surveyPanel!=null)
        {
            surveyPanel.SetActive(false);
        }
        if(surveyResultValueText!=null)
        {
            surveyResultValueText.gameObject.SetActive(false);
        }
        if(terminalWindow!=null)
        {
            terminalWindow.SetActive(false);
        }
        if(unreadBadge!=null)
        {
            unreadBadge.SetActive(false);
        }
        if(revealAllHandsPanel!=null)
            {
                revealAllHandsPanel.SetActive(false);
            }
        terminalLogText.text=""; // ログを空にする
        // 勝利確認ボタンの初期設定
        // CanvasGroupを取得
        if(winButton!=null)
        {
            winButtonCanvasGroup = winButton.GetComponent<CanvasGroup>();
            winButton.SetActive(false);
        }
        // 起動時にシステムメッセージを入れてみる
        AddLogMessage("--- SYSTEM BOOT SEQQUENCE INITIATED ---", null);
        AddLogMessage("--- WELCOME TO SIGMA TERMINAL ---", null);
    }
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
            // もし非アクティブにする際、ボタンがホバーで光ったままなら強制的に戻す
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
        if(targetPlayer.hand.Count > 0)
        {
            // ターゲットの手札からランダムに一枚選ぶ
            randomCard = targetPlayer.hand[Random.Range(0, targetPlayer.hand.Count)];
            // 公開リストに追加（永続化）
            if(!targetPlayer.revealedCards.Contains(randomCard))
            {
                 targetPlayer.revealedCards.Add(randomCard);
            }
        }

        // ターゲットの手札を震わせる
        Transform targetHand = GetHandContainerForPlayer(targetPlayer);
        if(targetHand != null)
        {
            targetHand.DOShakePosition(0.5f, new Vector3(10f, 10f, 0), 20);
        }
        
        yield return new WaitForSeconds(0.5f); // 演出のためのタメ

        if(randomCard == null)
        {
            string msg = $"{targetPlayer.playerName}の手札は0枚です。";
            AddLogMessage(msg, null);
            StartCoroutine(ShowEffectResult(msg));
            yield return new WaitForSeconds(2.0f);
        }
        else
        {
            // 3. カードを裏向きで生成
            GameObject cardObj = Instantiate(cardBackPrefab, surveyCardDisplayArea);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale = Vector3.one * 1.5f; // 少し大きく
            
            // マウス操作を無効化
            if(cardObj.GetComponent<Image>() != null) cardObj.GetComponent<Image>().raycastTarget = false;

            // 4. フリップアニメーション (裏 -> 表)
            float flipDuration = 0.4f;
            
            // Step 1: 90度まで回転（閉じる）
            yield return cardObj.transform.DORotate(new Vector3(0, 90, 0), flipDuration).SetEase(Ease.InBack).WaitForCompletion();

            // Step 2: オブジェクト差し替え（裏 -> 表）
            Destroy(cardObj);
            cardObj = Instantiate(cardPrefab, surveyCardDisplayArea);
            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localScale = Vector3.one * 1.5f;
            cardObj.transform.localRotation = Quaternion.Euler(0, -90, 0); // 逆向きからスタート
            
            cardObj.GetComponent<CardController>().Setup(randomCard);
            if(cardObj.GetComponent<Image>() != null) cardObj.GetComponent<Image>().raycastTarget = false;

            // Step 3: 0度に戻す（開く）
            // 注意: 新しいcardObjに対してTweenをかける
            yield return cardObj.transform.DORotate(Vector3.zero, flipDuration).SetEase(Ease.OutBack).WaitForCompletion();
            
            // ログ
            string msg = $"{targetPlayer.playerName}の手札[{randomCard.cardName}]を検閲";
            AddLogMessage(msg, null);
            
            yield return new WaitForSeconds(1.5f); // 結果を見せる時間
            
            // 手札の表示を更新（ここでCPUの手札が表になる）
            UpdateAllHandVisuals(); 
        }

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
            surveyResultValueText.text=$"MAX VALUE: {maxVal}";
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
    private string GetSectorIconName(CardSector sector, CardEffect effect)
    {
        // Sprite Assetで設定したアイコン名を返す
        switch(effect)
        {
            case CardEffect.Bribe: return "Refined_Silence_Added_CardSectorAtlas_4";
            case CardEffect.Audit: return "Refined_Silence_Added_CardSectorAtlas_5";
            case CardEffect.Censor: return "Refined_Silence_Added_CardSectorAtlas_6";
            case CardEffect.Interrogate: return "Refined_Silence_Added_CardSectorAtlas_7";
            case CardEffect.Reject: return "Refined_Silence_Added_CardSectorAtlas_8";
            case CardEffect.Suspend: return "Refined_Silence_Added_CardSectorAtlas_9";
            case CardEffect.Silence: return "Refined_Silence_Added_CardSectorAtlas_10";
        }
        switch(sector)
        {
            case CardSector.Eye: return "Refined_Silence_Added_CardSectorAtlas_0";
            case CardSector.Chain: return "Refined_Silence_Added_CardSectorAtlas_1";
            case CardSector.Gear: return "Refined_Silence_Added_CardSectorAtlas_2";
            case CardSector.Mask: return "Refined_Silence_Added_CardSectorAtlas_3";
            default: return "";
        }
    }
    public void AddLogMessage(string message, CardData cardInfo=null)
    {
        // 1. テキストを追記する（HTMLタグで色付けも可能）
        // 時刻を追加
        string timeStr=System.DateTime.Now.ToString("HH:mm:ss");
        string newLine=$"\n<color=#FFFFFF>[{timeStr}]</color>";
        // カード情報がある場合は。色付きテキストの代わりにスプライトを埋め込む
        if(cardInfo!=null)
        {
            string iconName=GetSectorIconName(cardInfo.sector, cardInfo.effect);
            // スプライトタグを埋め込む
            string spriteTag=$"<sprite name=\"{iconName}\">";
            newLine+=$"{spriteTag} {message}";
        }
        else
        {
            newLine+=message;
        }
        terminalLogText.text+=newLine;
        // 2. ウィンドウが開いているか閉じているかで挙動を変える
        if(terminalWindow.activeSelf)
        {
            // ウィンドウが開いている場合、自動で一番下にスクロール
            StartCoroutine(ForceScrollToBottom());
        }
        else
        {
            // ウィンドウが閉じている場合、未読バッジを表示
            unreadBadge.SetActive(true);
        }
    }
    // TerminalButton(LOGボタン)のOnClickに割り当てる
    public void ToggleTerminal()
    {
        bool isActive=!terminalWindow.activeSelf;
        terminalWindow.SetActive(isActive);
        if(isActive)
        {
            // 開いた瞬間
            unreadBadge.SetActive(false); // バッジを消す
            StartCoroutine(ForceScrollToBottom()); // スクロール位置をリセット

            // コマンド入力欄にフォーカスを合わせる（UX向上）
            if(commandInput!=null)
            {
                commandInput.ActivateInputField();
            }
        }
    }
    // ログスクロールを一番下に移動するコルーチン
    public IEnumerator ForceScrollToBottom()
    {
        // 1. まず1フレーム待ってレイアウト更新を完了させる
        yield return new WaitForEndOfFrame();
        // 2. レイアウトの強制更新
        // TextMeshProの文字数による「折り返し（改行）」計算をここで確定させる
        Canvas.ForceUpdateCanvases();
         // 4. 完全に計算が終わった状態で、一番下にスクロール
         if(terminalScrollRect!=null)
        {
            terminalScrollRect.verticalNormalizedPosition=0f;
        }
    }
    // ログをリセットするメソッド
    public void ResetLog()
    {
        terminalLogText.text="";
        AddLogMessage("--- NEW ROUND STARTED", null);
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
    
    void OnDestroy()
    {
        // シーン遷移時に確実にTweenを殺す
        if(winButtonAnimation != null && winButtonAnimation.IsActive())
        {
            winButtonAnimation.Kill();
        }
        // 他の全てのTweenもこのGameObjectに関連するものは殺す
        DOTween.Kill(this.transform);
        DOTween.Kill(this.gameObject);
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
        else if(players.Count==2)
        {
            // [1]番目はDOGとなっている。
            UpdateCPUHandVisuals(players[1], cpu1HandContainer, false, null);
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
        
        // 表示するカードのリストを決定
        List<CardData> targetHand = (handData != null) ? handData : cpu.hand;
        int childCount = targetHand.Count;

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
            CardData currentCard = targetHand[i]; // 現在のカードデータ
            
            // 全公開モード、もしくはこのカードが公開済みリストに含まれているか
            bool isRevealed = reveal || cpu.revealedCards.Contains(currentCard);

            if (isRevealed)
            {
                // 表向きで生成
                cardObj = Instantiate(cardPrefab, container);
                CardController cardController = cardObj.GetComponent<CardController>();
                // カードデータを設定
                cardController.Setup(currentCard);
                // "EYE" アイコン的なものを追加しても良いが、一旦表向きにするだけにする
                // Colorを変えて少し強調する
                if(!reveal && cpu.revealedCards.Contains(currentCard))
                {
                     // 公開されたカードは少し赤みがかった色にする（警告色）
                     Image img = cardObj.GetComponent<Image>();
                     if(img != null) img.color = new Color(1f, 0.8f, 0.8f);
                }
            }
            else
            {
                cardObj = Instantiate(cardBackPrefab, container);
            }
            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if(rect == null) continue;

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

        // CPU手札コンテナのサイズを調整（Raycast用）
        RectTransform containerRect = container.GetComponent<RectTransform>();
        if(containerRect != null)
        {
            float width = totalWidth + 150f; // カード幅分+余白
            float height = 250f; // カード高さ+Arc分
            containerRect.sizeDelta = new Vector2(width, height);
        }

        // HandHoverDetectorのリスト更新
        HandHoverDetector detector = container.GetComponent<HandHoverDetector>();
        if(detector != null)
        {
            detector.cardsInHand.Clear(); // 一旦クリア
            // コンテナ内の全てのCardControllerを探して登録
            // (RevealedのカードのみがCardControllerを持っている前提)
            foreach(Transform child in container)
            {
                CardController cc = child.GetComponent<CardController>();
                if(cc != null)
                {
                    detector.cardsInHand.Add(cc);
                }
            }
        }
    }

    // CPUの手札エリアにHoverDetector等をセットアップするヘルパー
    private void SetupCPUHandHover(Transform container)
    {
        if(container == null) return;
        
        // Image（RaycastTarget用）があるか確認、なければ追加
        Image img = container.GetComponent<Image>();
        if(img == null)
        {
            img = container.gameObject.AddComponent<Image>();
            img.color = Color.clear; // 透明にする
        }
        
        // HandHoverDetectorがあるか確認、なければ追加
        HandHoverDetector detector = container.GetComponent<HandHoverDetector>();
        if(detector == null)
        {
            detector = container.gameObject.AddComponent<HandHoverDetector>();
        }
        
        // CPUの手札なのでクリックは無効化、でもホバーは有効化
        detector.isInteractionEnabled = false;
        
        // カメラ参照などの初期化が必要ならStartで走るが、AddComponent直後なのでOK
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
            // ここでスタンプ判定を行う
            if(fieldTopStampEffect!=null)
            {
                if(!(cardData.effect==CardEffect.Bribe))
                {
                    fieldTopStampEffect.ResetStamp();
                }
            }
        }
        else
        {
            // 該当カードがなければ非表示
            if(fieldTopStampEffect!=null)
            {
                fieldTopStampEffect.ResetStamp();
            }
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
            string winReason;
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
            
            // ポップアップアニメーション
            winnerPanel.transform.localScale = Vector3.zero;
            winnerPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            // テキストの色をピカピカさせる
            winnerText.DOColor(Color.yellow, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            winnerText.DOKill(); // アニメーション停止
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
            playerScoreText.text = $"P1 [{players[0].playerName}]: \n{players[0].totalPoints} CR / {players[0].wins} Wins";
            cpu1ScoreText.text = $"P2 [{players[1].playerName}]: \n{players[1].totalPoints} CR / {players[1].wins} Wins";
            cpu2ScoreText.text = $"P3 [{players[2].playerName}]: \n{players[2].totalPoints} CR / {players[2].wins} Wins";
            Debug.Log($"スコア更新: P1({players[0].totalPoints}), P2({players[1].totalPoints}), P3({players[2].totalPoints})");
        }
        else if(players.Count==2)
        {
            playerScoreText.text = $"P1 [{players[0].playerName}]: \n{players[0].totalPoints} CR / {players[0].wins} Wins";
            cpu1ScoreText.text = $"P2 [{players[1].playerName}]: \n{players[1].totalPoints} CR / {players[1].wins} Wins";
            Debug.Log($"スコア更新: P1({players[0].totalPoints}), P2({players[1].totalPoints})");
        }
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
    public void UpdateCurrentTrend(Sprite icon, int trendValue)
    {
        if (currentTrendText != null && sectorIcon != null)
        {
            currentTrendText.text = $"TREND: {trendValue}";
            sectorIcon.sprite = icon;
        }
    }
    // Bribe用の場のトレンド更新&スタンプエフェクトメソッド
    public void UpdateCurrentTrendWhenBribe(Sprite targetCardSprite, Sprite targetCardIcon, int trendValue)
    {
        // 1. 画面端のトレンド表示を更新
        // ここでは、Bribeカード自体のアイコンではなく、
        // 「Bribeによって変化した結果のコード（例: Gear 7）」のアイコンを表示するのがわかりやすい
        if(currentTrendText != null && sectorIcon != null && targetCardIcon != null)
        {
            currentTrendText.text = $"TREND: {trendValue}";
            sectorIcon.preserveAspect = true; // アスペクト比を維持
            sectorIcon.sprite = targetCardIcon;
        }
        // 2. 場のカードの上にスタンプを押す
        if(fieldTopStampEffect != null && targetCardSprite != null)
        {
            // 検索したカードを渡す
            fieldTopStampEffect.ActivateStamp(targetCardSprite);
        }
    }
    // Censor/Interrogate用の場のトレンド更新メソッド
    public void UpdateCurrentTrendWhenSurvey()
    {
        if (currentTrendText != null && sectorIcon != null)
        {
            // トレンド値も不明にする
            currentTrendText.text = $"TREND: ERROR";
            // ?アイコンに変更
            sectorIcon.sprite = errorSprite;
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
                        .SetLoops(-1) // 無限ループ
                        .SetLink(winButton.gameObject); // GameObjectが破棄されたらTweenも破棄
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
        SoundManager.Instance.PlaySound(nextButtonSound);
        GameManager.Instance.OnContinueClicked();
    }
    // ログのパネルの表示
    public void ShowTerminalWindow(bool show)
    {
        terminalWindow.SetActive(show);
    }

    // 全員の手札を公開するパネルを表示
    public void ShowRevealAllHandsPanel(List<Player> players)
    {
        if (revealAllHandsPanel == null) return;
        revealAllHandsPanel.SetActive(true);

        // 既存の子要素を削除
        foreach (Transform child in revealAllHandsPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // レイアウトグループの確認/追加（必要に応じて）
        VerticalLayoutGroup vlg = revealAllHandsPanel.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = revealAllHandsPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = 30;
            vlg.childAlignment = TextAnchor.MiddleCenter;
        }

        foreach (Player p in players)
        {
            CreateHandRevealRow(p, revealAllHandsPanel.transform);
        }
        
        // 続けるボタンのガイドを表示するログを追加しても良い
        AddLogMessage("[SYSTEM] 全プレイヤーの手札を公開します。確認したらクリックしてください。", null);
    }

    private void CreateHandRevealRow(Player p, Transform parent)
    {
        GameObject row = new GameObject($"Row_{p.playerName}");
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // 名前テキスト（SurveyTitleTextをテンプレートにする）
        if (playerNameText != null)
        {
            GameObject textObj = Instantiate(playerNameText.gameObject, row.transform);
            // テンプレートのRectTransform設定によっては巨大になるのでリセット
            // RectTransform rt = textObj.GetComponent<RectTransform>();
            // rt.sizeDelta = new Vector2(250, 50);
            
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = p.playerName;
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            textObj.SetActive(true);
        }

        // 手札カード
        foreach (CardData card in p.hand)
        {
            GameObject cardObj = Instantiate(cardPrefab, row.transform);
            cardObj.transform.localScale = Vector3.one * 0.8f; // 縮小表示
            CardController cc = cardObj.GetComponent<CardController>();
            cc.Setup(card);
            // Raycast無効
            if (cardObj.GetComponent<Image>() != null) cardObj.GetComponent<Image>().raycastTarget = false;
            
            // LayoutElementでサイズ確保（LayoutGroup用）
            // Prefabのサイズを取得
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            LayoutElement le = cardObj.AddComponent<LayoutElement>();
            le.preferredWidth = rt.sizeDelta.x * 0.8f;
            le.preferredHeight = rt.sizeDelta.y * 0.8f;
        }
    }

    public void HideRevealAllHandsPanel()
    {
        if (revealAllHandsPanel != null)
        {
            revealAllHandsPanel.SetActive(false);
        }
    }
    public void SetGoalTextDependOnProgress(int remainingTurns)
    {
        if(GameManager.Instance.GetProgressFlag()==0)
        {
            if(remainingTurns==1)
            {
                gameGoalText.text=$"条件: <color=red>{remainingTurns}</color>ラウンド以内に1回勝利する。";
            }
            else
            {
                gameGoalText.text=$"条件: {remainingTurns}ラウンド以内に1回勝利する。";
            }
        }
        else if(GameManager.Instance.GetProgressFlag()==1)
        {
            gameGoalText.text="条件: 2回勝利する。";
        }
        else if(GameManager.Instance.GetProgressFlag()==2)
        {
            gameGoalText.text="条件: 3回勝利する。";
        }
         else
        {
            gameGoalText.text="----------------";
        }
    }
    public void EnableExecutionObject()
    {
        if(bloodObject!=null)
        {
            bloodObject.SetActive(true);
        }
    }
    public void ShowBlackOut()
    {
        if(blackOut!=null)
        {
            blackOut.gameObject.SetActive(true);
        }
    }
}