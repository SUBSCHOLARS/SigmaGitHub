using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    // シングルトンの設定
    // Instanceを通じて他のスクリプトからGameManagerの機能にアクセスできる
    public static GameManager Instance { get; private set; }
    [Header("カードデータ")]
    public List<CardData> allCardDatabase;
    public List<CardData> ideologyCardDatabase;
    [Header("Audio")]
    public AudioClip drawSound;
    public AudioClip playCardSound;
    public AudioClip winSound;
    public AudioClip trendRideSound;
    public AudioClip firstSetupSound;
    public AudioClip bulletDropSound;
    [SerializeField] private AudioClip dogNoticeSound;

    [Header("ゲームの状態")]
    public List<CardData> deck = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();

    [Header("データベース")]
    [SerializeField] private InquiryResponseDatabase inquiryResponseDatabase;
    // （デバッグ用）現在の場のカード
    private CardData currentCardOnField;
    // 現在の「トレンド（場の数字）」
    private int currentTrendValue = 0;

    // プレイヤーの管理（本実装）
    public List<Player> players = new List<Player>();
    protected int currentPlayerIndex = 0;
    private bool isTurnClockwise = true; // ターン進行方向（Reject用）
    public bool isPlayerInputLocked = false; // 操作ロック用のフラグ
    // ゲームの進行度合いを管理する整数フラグ
    private int gameProgressFlag = 0;
    protected bool isWaitingForWinConfirmation = false;
    private bool isNextPlayWild = false;
    private bool isWaitingForContinueClick = false;
    private Player gameMaster;
    // どの調査カードが使われたか記憶する変数
    private CardEffect pendingSurveyEffect = CardEffect.None;
    // Sigma Speak 用フラグ
    public bool sigmaSpeakActive = false;
    public bool sigmaSpeakUsedThisTurn = false;
    public int sigmaSpeakActivatorIndex = -1; // 発動者のインデックス（-1=未発動）
    // Memory Hole 用フラグ
    private bool pendingMemoryHole = false;
    public bool memoryHoleUsedThisTurn = false;
    // private int winningScore = 100; // 勝利に必要なスコア
    public int currentRound = 1; // 現在のラウンド
    protected Sprite initialSprite;
    private const int FIRST_DECK_DISTRIBUTION_COUNT=21;
    private int distributionCount=0;

    [SerializeField] private Sprite[] numberSprites;

    void Awake()
    {
        gameMaster = new Player(PlayerID.GameMaster, false, "GameMaster", 0, IdeologyType.None);
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoadは削除。シーン遷移ごとに新しいGameManagerを使用させる。
        }
        else
        {
            Destroy(gameObject); // 既にインスタンスが存在する場合は破棄
        }
    }
    protected virtual void Start()
    {
        InitializeGame();
    }

    public virtual void InitializeGame()
    {
        // 3人対戦のセットアップ
        players.Clear();
        if (PersistentDataManager.Instance != null)
            gameProgressFlag = PersistentDataManager.Instance.GameProgressFlag;
        UIManager.Instance.SetFirstShownGoalText();
        string pName = PersistentDataManager.Instance != null ? PersistentDataManager.Instance.PlayerName : "Ian";
        
        players.Add(new Player(PlayerID.Player, false, pName, 0, IdeologyType.None)); // 0番目が人間
        players.Add(new Player(PlayerID.CPU, true, "CPU_1", 0, IdeologyType.None));    // 1番目がCPU
        players.Add(new Player(PlayerID.CPU, true, "CPU_2", 0, IdeologyType.None));    // 2番目がCPU
        // ゲーム開始時にUIを初期化
        currentRound = 1;
        UIManager.Instance.UpdateRoundText(currentRound);
        UIManager.Instance.UpdateScoreboard(players); // 初期スコア(0)を表示
        // ゲーム開始時に山札を準備
        SetUpDeck();
        // カード配布の音を鳴らす
        SoundManager.Instance.PlaySound(firstSetupSound);
        // 全プレイヤーにカードを配る
        // ラウンドロビンロジックを使用する
        // つまり、一人に一枚ずつ渡すという動作を7回繰り返すということ
        for(int i=0; i<7; i++)
        {
            foreach(Player player in players) // 各プレイヤーに一枚ずつ
            {
                DrawCards(player.hand, 1);
                distributionCount++;
            }
        }
        // イデオロギーを判定して、イデオロギーカードを渡す
        DealIdeologyCard();
        // 最初の1枚を場に出す
        StartGame();
        // テンホウ（仮名称）ロジックを追加
        // ゲーム開始時のトレンドと手札が一致していないかをチェック
        // 最初はゲームマスターという存在が手札を場に出すため、この場合は全員がトレンドライドの判定を受けるという特殊な状況となる
        List<Player> initialWinners = CheckForTrendRide(gameMaster);
        if(initialWinners.Count>0)
        {
            // 誰かが勝利していた場合
            Debug.Log($"ゲーム開始時マッチ（テンホウ）が検出されました。ラウンド終了シーケンスに移行します。");
            // 即座に勝利シーケンスを起動
            // gameMasterをactionPlayerとして渡す
            StartCoroutine(StartRoundEndSequence(initialWinners, gameMaster, WinType.TrendRide));
            return; // リターンで最初のターンが開始するのを防ぐ
        }
        // ゴール条件を表示
        int remainingTurn = GetProgressFlag() switch
        {
            0 => 3,
            2 => 5,
            _ => 0 
        };
        UIManager.Instance.SetGoalTextDependOnProgress(remainingTurn); // 最初は3ラウンド
        // プレイヤー（0番目）の手札をUIに反映
        SortPlayerCardDataByNumber();
        UIManager.Instance.UpdateAllHandVisuals();
        UIManager.Instance.UpdateCurrentTrend(initialSprite, currentTrendValue);
    }
    // 山札を初期化し、シャッフルするメソッド
    public virtual void SetUpDeck()
    {
        deck.Clear();
        discardPile.Clear();
        // データベースから全てのカードを山札に追加
        deck.AddRange(allCardDatabase);
        ShuffleDeck();
        UIManager.Instance.UpdateDeckVisual(deck.Count);
    }
    // Fisher-Yatesアルゴリズムを使い、山札をシャッフルするメソッド
    public void ShuffleDeck()
    {
        Debug.Assert(deck != null, "デッキが空なのでシャッフルできません");
        for (int i = 0; i < deck.Count; i++)
        {
            int rand = UnityEngine.Random.Range(i, deck.Count);
            CardData temp = deck[rand];
            deck[rand] = deck[i];
            deck[i] = temp;
        }
        Debug.Log("山札をシャッフルしました。枚数: " + deck.Count);
    }
    // 指定した手札に、指定した枚数のカードを引くメソッド
    public void DrawCards(List<CardData> hand, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0)
            {
                // 山札が空の場合、捨て札をシャッフルして山札に戻す
                Debug.Log("山札が空です。捨て札をシャッフルして山札に戻します。");

                if (discardPile.Count > 0)
                {
                    deck.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleDeck();
                    // 捨て札を戻す処理
                    UIManager.Instance.UpdateDeckVisual(deck.Count);
                }
                else
                {
                    // 捨て札も空なら、もう引けないのでループを抜ける
                    Debug.LogWarning("山札も捨て札も空です。これ以上カードを引けません。");
                    break;
                }
            }

            // 山札の一番上のカードを手札に追加
            CardData drawnCard = deck[0];
            deck.RemoveAt(0);
            hand.Add(drawnCard);
            if(distributionCount>FIRST_DECK_DISTRIBUTION_COUNT)
            {
                SoundManager.Instance.PlaySound(drawSound);
            }

            // (デバッグログはコンソールが荒れるので、必要な文だけ表示)
            if (hand == players[0].hand)
            {
                Debug.Log("プレイヤーが引いたカード: " + drawnCard.cardName);
            }
        }
    }
    public List<CardData> GetPlayerHand()
    {
        if (players.Count > 0 && !players[0].isCPU)
        {
            return players[0].hand;
        }
        return new List<CardData>(); // 該当なし。
    } 
    // ゲームの開始（最初の1枚を場に出す）
    public virtual void StartGame()
    {
        // 山札から「効果なし(None)」のカードを「探す」
        int firstCardIndex = -1;
        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].effect == CardEffect.None && deck[i].ideologyType == IdeologyType.None) // イデオロギーカードは最初に出さないルール
            {
                firstCardIndex = i;
                break;
            }
        }

        if (firstCardIndex != -1)
        {
            // 見つかった場合
            CardData firstCard = deck[firstCardIndex];
            initialSprite= firstCard.rawSectorIcon;
            deck.RemoveAt(firstCardIndex); // 見つけた場所から削除
            PlayCardToField(firstCard, gameMaster); // 最初のカードを場に出す
            Debug.Log("ゲーム開始！最初のカード: " + firstCard.cardName);
        }
        else
        {
            // 1枚も見つからなかった場合（テスト中や、特殊な状況）
            Debug.LogError("山札に数字カードが1枚もありません。ゲームを開始できません。");

            // (もし山札が空なら、捨て札を戻してリトライする処理などもここ)
            if (deck.Count == 0 && discardPile.Count > 0)
            {
                Debug.Log("山札が空のため、捨て札を戻してリトライします。");
                deck.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck();
                StartGame(); // もう一度 StartGame を呼び出す
            }
        }
        UIManager.Instance.UpdateAllHandVisuals(); // ここで自動的にYourTrendも更新される
    }
    // カードを場（捨て札）に出す処理
    public void PlayCardToField(CardData card, Player player)
    {
        discardPile.Add(card);
        currentCardOnField = card;
        SoundManager.Instance.PlaySound(playCardSound);
        // メッセージを作成
        string playerName = player.playerName;
        string message = $"{DateTime.Now} [{playerName}] played [{card.cardName}]";
        // UIManagerにログ表示を依頼
        UIManager.Instance.AddLogMessage(message, card);
        if(card.effect==CardEffect.Censor||card.effect==CardEffect.Interrogate)
        {
           // 次のプレイがワイルドになる
            isNextPlayWild = true;
            Debug.Log("調査カードが出されました。次のプレイはワイルドになります。");
            // 特殊な場合の場のUI更新を行う
            currentTrendValue=card.numberValue;
            UIManager.Instance.UpdateCurrentTrendWhenSurvey(currentTrendValue);
            Debug.Log("場のUIを調査カード用に更新しました。");
        }
        else
        {
            // 場のトレンド（数字）を更新
            currentTrendValue = card.numberValue;
            isNextPlayWild = false;
            // 場のトレンドが更新されたのでUIに反映
            UIManager.Instance.UpdateCurrentTrend(card.rawSectorIcon, currentTrendValue);
            Debug.Log("場に " + card.cardName + " が出されました。現在のトレンド: " + currentTrendValue);
        }
        UIManager.Instance.UpdateFieldPileUI(card);
        SortPlayerCardDataByNumber();
    }
    // カードが出せるかを判定するメソッド
    public bool CanPlayCard(CardData cardToPlay)
    {
        // Thoughtcrime は絶対に場に出せない（詰み回避ルールも無効）
        if (cardToPlay.ideologyType == IdeologyType.Thoughtcrime)
            return false;

        // アクティブ系イデオロギーカードは通常プレイ不可（クリックで効果発動）
        if (cardToPlay.ideologyType == IdeologyType.SigmaSpeak ||
            cardToPlay.ideologyType == IdeologyType.MemoryHole)
            return false;

        // 詰み回避ルールを最優先でチェック
        if(isNextPlayWild)
        {
            return true;
        }
        // 1. cardToPlay.effect == CardEffect.Bribe (賄賂) なら true
        if (
            cardToPlay.effect == CardEffect.Censor ||
            cardToPlay.effect == CardEffect.Interrogate)
        {
            return true;
        }
        // 一旦ここにBribeの処理を切り分ける
        if(cardToPlay.effect==CardEffect.Bribe /* && cardToPlay.sector==currentCardOnField.sector */)
        {
            return true;
        }
        // 2. cardToPlay.sector == currentCardOnField.sector (色が同じ) なら true
        if (cardToPlay.sector == currentCardOnField.sector)
        {
            return true;
        }
        // 3. cardToPlay.effect == currentCardOnField.effect (効果が同じ) かつ effect != None なら true
        if (cardToPlay.effect != CardEffect.None && cardToPlay.effect == currentCardOnField.effect)
        {
            return true;
        }
        // 4. cardToPlay.numberValue == currentTrendValue (数字が同じ) かつ effect == None なら true
        if (cardToPlay.effect == CardEffect.None && cardToPlay.numberValue == currentTrendValue)
        {
            return true;
        }
        // 5. cardToPlay.numberValue == currentTrendValue（数字が同じ）かつffectがReject, Audit, Suspendのいずれかであればtrue
        if((cardToPlay.effect == CardEffect.Audit ||
            cardToPlay.effect == CardEffect.Reject ||
            cardToPlay.effect == CardEffect.Suspend) &&
            (cardToPlay.numberValue == currentTrendValue))
        {
            return true;
        }
        return false;
    }
    // DrawButtonから呼ばれるメソッド
    public virtual void PlayerDrawCard()
    {
        // 1. 操作ロックとターンをチェック
        if (isPlayerInputLocked)
        {
            return;
        }
        if (players[currentPlayerIndex].isCPU)
        {
            return;
        }
        // 押した瞬間にロック
        SetInputLock(true);

        Player humanPlayer = players[currentPlayerIndex];

        // 2. 1枚引く
        Debug.Log("プレイヤーが山札から1枚引きます。");
        DrawCards(humanPlayer.hand, 1);

        // 3. UIを全て更新
        SortPlayerCardDataByNumber();
        UIManager.Instance.UpdateAllHandVisuals();
        UIManager.Instance.UpdateDeckVisual(deck.Count);

        // 4. マッチ判定
        // トレンドライドを先にチェック
        List<Player> trendRideWinners = CheckForTrendRide(humanPlayer);
        if (trendRideWinners.Count > 0)
        {
            SetInputLock(true);
            StartCoroutine(StartRoundEndSequence(trendRideWinners, humanPlayer, WinType.TrendRide));
            return;
        }
        // セルフマッチをチェック
        if (CheckForSelfMatch(humanPlayer))
        {
            SetInputLock(true);
            isWaitingForWinConfirmation = true;
            // 勝利確認ボタンを表示
            UIManager.Instance.ShowWinButton(true);
            return; // ターン終了をせず、ボタン入力を待つ
        }
        // 勝利しなかった場合、ターンを次に回す
        NextTurn();
    }
    // Sigma Speak 発動ボタンから呼ばれるメソッド
    public void ActivateSigmaSpeak(int activatorIndex = -1)
    {
        int idx = activatorIndex < 0 ? currentPlayerIndex : activatorIndex;
        // プレイヤー発動時のみ入力ロック・CPU判定チェックを行う
        if (activatorIndex < 0 && (isPlayerInputLocked || players[idx].isCPU || sigmaSpeakUsedThisTurn))
            return;

        sigmaSpeakActive = true;
        sigmaSpeakUsedThisTurn = true;
        sigmaSpeakActivatorIndex = idx;
        Debug.Log($"[SigmaSpeak] {players[idx].playerName} が発動: 次の自ターンまで全効果カードを無効化");

        SortPlayerCardDataByNumber();
        UIManager.Instance.UpdateAllHandVisuals();
        UIManager.Instance.UpdateFieldPileUI(currentCardOnField);
    }

    // Memory Hole 効果を実行するメソッド（MemoryHolePanelControllerから呼ばれる）
    public void ExecuteMemoryHoleEffect(Player executor, Player target, CardData targetCard, CardData executorCard)
    {
        target.hand.Remove(targetCard);        // ターゲットが1枚失う（捨て）
        executor.hand.Remove(executorCard);    // 発動者が1枚渡す
        target.hand.Add(executorCard);         // ターゲットが発動者のカードを受け取る
        // targetCard は捨て牌（誰も受け取らない）
        Debug.Log($"[MemoryHole] {executor.playerName} が {target.playerName} の {targetCard.cardName} を捨て、{executorCard.cardName} を渡した");

        // ターゲット手札を震わせてフィードバックを与える
        Transform targetHandContainer = UIManager.Instance.GetHandContainerForPlayer(target);
        if (targetHandContainer != null)
        {
            targetHandContainer.DOShakePosition(0.5f, new Vector3(10f, 10f, 0), 20);
        }

        SortPlayerCardDataByNumber();
        UIManager.Instance.UpdateAllHandVisuals();
        if (!executor.isCPU) SetInputLock(false); // CPU発動時はターン遷移で解除
    }

    // Bribeの5つのボタンから呼ばれるメソッド
    public void PlayerSelectBribeTrend(int trend)
    {
        // 予期せぬ呼び出しをガード
        if (!isPlayerInputLocked)
        {
            return;
        }
        // プレイヤーのターンのみ
        if (players[currentPlayerIndex].isCPU)
        {
            return;
        }
        currentTrendValue = trend;
        Debug.Log($"Bribe: プレイヤーがトレンドを {currentTrendValue} に設定しました。");

        // 現在場に出ているBribeカードの絵柄を取得
        // PlayCardToFieldですでにcurrentCardOnFieldは更新されているはず
        // CardSector bribeSector=currentCardOnField.sector;
        // Debug.Log($"Bribe: {bribeSector} の数字 {currentTrendValue} を設定しました。");
        // その絵柄かつ指定した数字のカードデータを検索して取得
        // CardData targetCard=GetCardDataBySectorAndNumber(bribeSector, currentTrendValue);

        // 画像が見つかればそれをUIに渡す（見つからなければnull）
        // Sprite stampSprite=(targetCard != null) ? targetCard.cardIcon: null;
        // Sprite stampIcon=(targetCard!=null) ? targetCard.rawSectorIcon: null;

        Sprite stampSprite = GetNumberSprite(currentTrendValue);
        Sprite stampIcon = currentCardOnField.rawSectorIcon;

        // 場のトレンドが更新されたのでUIに反映（Bribe用）
        UIManager.Instance.UpdateCurrentTrendWhenBribe(stampSprite, stampIcon, currentTrendValue);

        UIManager.Instance.HideBribeSelectionUI();

        // ターンを次に回す。CPUではないことが保証されているので回して良い。
        NextTurn();
    }
    // プレイヤーの手札の合計値を計算するメソッド
    public (int totalValue, bool hasDoubleThink) GetHandValue(List<CardData> hand)
    {
        int totalValue = 0;
        bool hasDoubleThink = hand.Any(c => c.ideologyType == IdeologyType.DoubleThink);
        foreach (CardData card in hand)
        {
            totalValue += card.handValue;
        }
        return (totalValue, hasDoubleThink);
    }
    public virtual IEnumerator TryPlayCard(CardData cardToPlay)
    {
        // 1. 操作ロックをチェック
        if (isPlayerInputLocked)
        {
            yield break;
        }
        // 2. プレイヤーのターンかチェック
        if (players[currentPlayerIndex].isCPU)
        {
            Debug.LogWarning("現在はCPUのターンです。プレイヤーはカードを出せません。");
            yield break;
        }
        // 3. プレイヤーの手札に存在するカードのみプレイ可（CPUカードの誤クリック防止）
        if (!players[currentPlayerIndex].hand.Contains(cardToPlay)) yield break;
        if (!CanPlayCard(cardToPlay))
        {
            // アクティブ系イデオロギーカードはクリックで効果発動
            if (cardToPlay.ideologyType == IdeologyType.SigmaSpeak)
            {
                ActivateSigmaSpeak(); // 内部でguardチェック済み
                yield break;
            }
            if (cardToPlay.ideologyType == IdeologyType.MemoryHole && !memoryHoleUsedThisTurn)
            {
                SetInputLock(true);
                pendingMemoryHole = true;
                memoryHoleUsedThisTurn = true;
                UIManager.Instance.ShowTargetSelectionUI();
                yield break;
            }
            Debug.Log("このカードは出せません: " + cardToPlay.cardName);
            yield break;
        }
        // 3. カードを出せる場合の処理を続ける
        Player humanPlayer = players[currentPlayerIndex];
        humanPlayer.hand.Remove(cardToPlay);
        humanPlayer.revealedCards.Remove(cardToPlay);
        humanPlayer.interrogatedCards.Remove(cardToPlay);
        SetInputLock(true);
        yield return StartCoroutine(UIManager.Instance.ShowPlayerPlayCardAnimation(cardToPlay));
        PlayCardToField(cardToPlay, humanPlayer); // UI更新もこの中で行われる
        yield return StartCoroutine(UIManager.Instance.ShowPlayerHandGapFill());
        UIManager.Instance.UpdateAllHandVisuals(); // ここで自動的にYourTrendも更新される

        // 4. マッチ判定
        // トレンドライドを先にチェック
        List<Player> trendRideWinners = CheckForTrendRide(humanPlayer);
        if (trendRideWinners.Count > 0)
        {
            SetInputLock(true);
            Debug.Log($"トレンドライド {humanPlayer.playerName} の行動で勝利が発生");
            // 勝利シーケンスを開始（引数に「行動した人」を渡す）
            StartCoroutine(StartRoundEndSequence(trendRideWinners, humanPlayer, WinType.TrendRide));
            yield break; // 勝利したのでターンを回さない
        }
        // セルフマッチをチェック
        if(CheckForSelfMatch(humanPlayer))
        {
            SetInputLock(true);
            isWaitingForWinConfirmation = true;
            // 勝利確認ボタンを表示
            UIManager.Instance.ShowWinButton(true);
            yield break; // ターン終了をせず、ボタン入力を待つ
        }
        // 5. マッチしなかった場合、効果処理とターン送り
        // 操作をロックし、効果処理コルーチン開始
        SetInputLock(true);
        StartCoroutine(HandleCardEffectAndTransition(cardToPlay));
    }
    // 効果なしでターンを終える時専用
    public void NextTurn()
    {
        StartCoroutine(TurnTransitionRoutine(CardEffect.None));
    }
    // 勝利演出　=> ポイント計算 => 次ラウンド準備の流れを管理
    private IEnumerator StartRoundEndSequence(List<Player> winners, Player actionPlayer, WinType winType)
    {
        UIManager.Instance.ShowTerminalWindow(false);
        // 1. 勝利演出（UIに任せる）
        // 他のプレイヤーの手札も全て公開する
        UIManager.Instance.RevealAllHands();
        if (winType == WinType.TrendRide)
        {
            SoundManager.Instance.PlaySound(trendRideSound);

            // CPU が勝利した場合: 表情変化 + DogNotice SE + 膨張縮小アニメーション
            foreach (Player winner in winners)
            {
                if (winner.isCPU)
                {
                    int cpuIdx = players.IndexOf(winner);
                    UIManager.Instance.UpdateCPUFace(cpuIdx, CPUFaceState.TrendrideWin);
                    if (dogNoticeSound != null)
                        SoundManager.Instance.PlaySound(dogNoticeSound);
                    yield return StartCoroutine(
                        UIManager.Instance.PlayCPUTrendRideWinAnimation(cpuIdx));
                    yield return new WaitForSeconds(1.5f);
                }
                else
                {
                    if (dogNoticeSound != null)
                        SoundManager.Instance.PlaySound(dogNoticeSound);
                    UIManager.Instance.UpdateCPUFace(cpuPlayerIndex: 0, CPUFaceState.TrendrideLose);
                    UIManager.Instance.UpdateCPUFace(cpuPlayerIndex: 1, CPUFaceState.TrendrideLose);
                    yield return new WaitForSeconds(1.2f);
                }
            }

            // 2. トレンドライドであった場合、アラートを表示して待機
            UIManager.Instance.ShowTrendRideAlert(true, winners, actionPlayer);
            yield return StartCoroutine(WaitForContinueCLick());
            UIManager.Instance.ShowTrendRideAlert(false, null, null);
        }
        else
        {
             SoundManager.Instance.PlaySound(winSound);
        }
        // 3. 勝利者パネルを表示してクリックを待つ
        UIManager.Instance.ShowWinnerAnimation(true, winners, winType, currentTrendValue);
        yield return StartCoroutine(WaitForContinueCLick());
        UIManager.Instance.ShowWinnerAnimation(false, null, WinType.SelfMatch, 0);

        // 4. 各手札をここで見せる
        UIManager.Instance.ShowRevealAllHandsPanel(players);
        yield return StartCoroutine(WaitForContinueCLick());
        UIManager.Instance.HideRevealAllHandsPanel();

        // 5. ポイント計算（actionPlayerを渡して分岐）
        CalculatePoints(winners, actionPlayer);
        UIManager.Instance.UpdateScoreboard(players); // スコアボードUIを更新
        UIManager.Instance.UpdateAllCPUFaceExpressions(); // wins を反映した表情に戻す

        // 6. 最終勝利判定
        Player overallWinner = CheckForOverallWinner();
        if (overallWinner != null)
        {
            // ゲーム終了
            UIManager.Instance.ShowGameEndAnimation(true, overallWinner);
            yield return StartCoroutine(WaitForContinueCLick());
            UIManager.Instance.ShowGameEndAnimation(false, null);
            Debug.Log($"最終勝者: {overallWinner.playerName}");
            // ゲームを最初からリスタート -> 質問シーケンスへ
            // RestartGame();
            if(!overallWinner.isCPU)
            {
                // Thoughtcrime保持中の総合勝利 → 革命ルートへ分岐
                if (PlayerHasIdeologyInHand(overallWinner, IdeologyType.Thoughtcrime))
                {
                    Debug.Log("革命ルート突入: Thoughtcrime保持での総合勝利");
                    SceneManager.LoadSceneAsync("Revolution");
                }
                // 最終ステージ(Flag5)でThoughtcrime以外のイデオロギーカード保持 → Brainwash
                else if (GetProgressFlag() == 5 && overallWinner.hand.Any(c => c.isIdeologyCard))
                {
                    Debug.Log("Brainwashルート突入: Flag5でイデオロギーカード保持での総合勝利");
                    SceneManager.LoadSceneAsync("Brainwash");
                }
                // 中間ステージ(Flag0〜4)での総合勝利 → 次の尋問シーケンスへ
                else if (GetProgressFlag() < 5)
                {
                    Debug.Log($"中間勝利: Flag{GetProgressFlag()} クリア → Inquiryへ遷移");
                    SceneManager.LoadSceneAsync("Inquiry");
                }
                // Flag5でイデオロギーカードなし → 処刑エンド
                else
                {
                    SceneManager.LoadSceneAsync("Execution");
                }
            }
            else
            {
                // 最終ステージ(Flag5)でCPUが勝利 → 失格エンド
                if (GetProgressFlag() == 5)
                {
                    if(PlayerHasIdeologyInHand(players[0], IdeologyType.Thoughtcrime))
                    {
                        SceneManager.LoadSceneAsync("Execution");
                    }
                    Debug.Log("Disqualificationルート突入: Flag5でCPUが総合勝利");
                    SceneManager.LoadSceneAsync("Disqualification");
                }
                else
                {
                    StartNextRound();
                }
            }
        }
        else
        {
            StartNextRound();
        }
    }
    private IEnumerator HandleCardEffectAndTransition(CardData playedCard)
    {
        // Sigma Speak 有効中はカード効果をスキップして通常遷移
        if (sigmaSpeakActive && playedCard.effect != CardEffect.None)
        {
            Debug.Log($"[SigmaSpeak] {playedCard.cardName} の効果を無効化");
            StartCoroutine(TurnTransitionRoutine(CardEffect.None));
            yield break;
        }

        // 1. カードを出した本人が実行する効果処理
        Player cardPlayer = players[currentPlayerIndex];
        if (playedCard.effect == CardEffect.Bribe)
        {
            if (cardPlayer.isCPU)
            {
                // AIによる最適なトレンド選択
                int chosenTrend = FindBestTrendForBribe(cardPlayer);
                currentTrendValue = chosenTrend;
                Debug.Log($"[AI Bribe] {cardPlayer.playerName} selected Trend {currentTrendValue}");
                // CPUの場合も同様に画像を取得して反映
                Sprite stampSprite = GetNumberSprite(currentTrendValue);
                Sprite stampIcon = currentCardOnField.rawSectorIcon;
                // 場のトレンドが更新されたのでUIに反映
                UIManager.Instance.UpdateCurrentTrendWhenBribe(stampSprite, stampIcon, currentTrendValue);
                StartCoroutine(TurnTransitionRoutine(playedCard.effect));
            }
            else
            {
                // プレイヤーの入力待ち
                UIManager.Instance.ShowBribeSelectionUI();
                // PlayerSelectBribeTrendが呼ばれるまで、このコルーチンはここで「待機」
                // (PlayerSelectBribeTrendがNextTurn()を呼ぶ)
                yield break; // コルーチンを終了し、ボタン入力を終了し、ボタン入力を待つ
            }
        }
        else if (playedCard.effect == CardEffect.Censor || playedCard.effect == CardEffect.Interrogate)
        {
            pendingSurveyEffect = playedCard.effect;
            if (cardPlayer.isCPU)
            {
                List<Player> possibleTargets=new List<Player>();
                for(int i=0; i<players.Count; i++)
                {
                    if(i!=currentPlayerIndex)
                    {
                        possibleTargets.Add(players[i]);
                    }
                }
                Player targetPlayer=possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];
                Debug.Log($"[CPU] {cardPlayer.playerName}が{targetPlayer.playerName}をターゲットに選択");
                // UIManagerのアニメーションコルーチンを呼び出して待機
                if(playedCard.effect==CardEffect.Censor)
                {
                    yield return StartCoroutine(UIManager.Instance.ShowCensorAnimation(targetPlayer));
                }
                else // Interrogate
                {
                    yield return StartCoroutine(UIManager.Instance.ShowInterrogateAnimation(targetPlayer));
                }
                // アニメーションが終わったら次のターンへ
                StartCoroutine(TurnTransitionRoutine(playedCard.effect));
                yield break;
            }
            else // プレイヤーが使った場合
            {
                // ターゲット選択UIを表示
                UIManager.Instance.ShowTargetSelectionUI();
                yield break; // PlayerSelectTargetがNextTurn()を呼ぶ
            }
        }
        else
        {
            // 2. ターン遷移（Bribe/Censor/Interrogate 以外の場合）
            // 以前のNextTurn(playedEffect)のロジックをここに持ってくる
            StartCoroutine(TurnTransitionRoutine(playedCard.effect));
        }
    }
    // ターン遷移アニメーション用コルーチン
    private IEnumerator TurnTransitionRoutine(CardEffect playedEffect)
    {
        // 1. 操作をロック
        SetInputLock(true);
        // 2. ターン計算（効果処理）
        // 1. 効果処理（ターン計算の「前」）
        if (playedEffect == CardEffect.Reject)
        {
            isTurnClockwise = !isTurnClockwise;
            UIManager.Instance.ReverseOrNonReverseIndication(isTurnClockwise);
            Debug.Log("リバース!");
        }
        // 2. 次のプレイヤーを計算
        int skippledPlayers = 0;
        if (playedEffect == CardEffect.Suspend)
        {
            skippledPlayers = 1; // 1人スキップ
            Debug.Log("スキップ!");
        }
        for (int i = 0; i <= skippledPlayers; i++)
        {
            if (isTurnClockwise)
            {
                currentPlayerIndex++;
                if (currentPlayerIndex >= players.Count)
                {
                    currentPlayerIndex = 0; // 周回させる
                }
            }
            else
            {
                currentPlayerIndex--;
                if (currentPlayerIndex < 0)
                {
                    currentPlayerIndex = players.Count - 1; // 周回させる
                }
            }
        }
        Player targetPlayer = players[currentPlayerIndex];
        Debug.Log($"--- {players[currentPlayerIndex].id} のターン ---");

        // 3. アニメーション開始
        yield return StartCoroutine(UIManager.Instance.ShowTurnAnimation(targetPlayer.playerName, currentPlayerIndex));

        // 4. 効果処理（ターン計算の「後」）
        if (playedEffect == CardEffect.Audit)
        {
            // TODO: 回避（Audit返し）のロジック
            Debug.Log($"{targetPlayer.id} は2枚引く!");
            DrawCards(targetPlayer.hand, 2);
            SortPlayerCardDataByNumber();
            UIManager.Instance.UpdateAllHandVisuals();
        }
        // ターン開始
        // 発動者のターン開始時に SigmaSpeak を解除（CPU/プレイヤー共通）
        if (sigmaSpeakActive && currentPlayerIndex == sigmaSpeakActivatorIndex)
        {
            sigmaSpeakActive = false;
            sigmaSpeakUsedThisTurn = false;
            sigmaSpeakActivatorIndex = -1;

            SortPlayerCardDataByNumber();
            UIManager.Instance.UpdateAllHandVisuals();
            UIManager.Instance.UpdateFieldPileUI(currentCardOnField);
        }
        // プレイヤーのターン開始時に MemoryHole をリセット
        if (!targetPlayer.isCPU)
            memoryHoleUsedThisTurn = false;

        // 次の人がCPUなら、CPUの試行ルーチンを呼ぶ
        if (targetPlayer.isCPU)
        {
            ExecuteCPUTurn();
        }
        // それ以外ならプレイヤーのターンなのでロックを解除する
        else
        {
            SetInputLock(false);
        }
    }
    // 指定したプレイヤーが特定のイデオロギーカードを手札に持っているか判定するメソッド
    public bool PlayerHasIdeologyInHand(Player player, IdeologyType type)
    {
        return player.hand.Any(c => c.ideologyType == type);
    }
    // トレンドライド判定を行うメソッド
    protected List<Player> CheckForTrendRide(Player actionPlayer)
    {
        List<Player> winners = new List<Player>();
        foreach (Player player in players)
        {
            if (player == actionPlayer)
            {
                continue; // 行動者自身を除外
            }
            var (handValue, hasDoubleThink) = GetHandValue(player.hand);
            bool isDoublethinkMatch = hasDoubleThink && currentTrendValue - handValue == 1;
            if (handValue == currentTrendValue || isDoublethinkMatch)
            {
                // 0-0マッチ禁止ルール
                if (handValue != 0 || currentTrendValue != 0)
                {
                    Debug.Log($"トレンドライド: {player.playerName} が勝利条件を満たしました。");
                    winners.Add(player);
                }
            }
        }
        return winners;
    }
    // セルフマッチの確認を行うメソッド
    private bool CheckForSelfMatch(Player actionPlayer)
    {
        var (handValue, hasDoubleThink) = GetHandValue(actionPlayer.hand);
        bool isDoublethinkMatch = hasDoubleThink && currentTrendValue - handValue == 1;
        if (handValue == currentTrendValue || isDoublethinkMatch)
        {
            // 0-0マッチ禁止ルール
            if (handValue != 0 || currentTrendValue != 0)
            {
                // Bribeでの上がり禁止チェック
                if (currentCardOnField.effect == CardEffect.Bribe)
                {
                    Debug.Log($"Bribe(賄賂)では上がれません");
                    return false;
                }
                Debug.Log($"セルフマッチ: {actionPlayer.playerName} が勝利できます。");
                return true;
            }
        }
        return false;
    }
    
    // ポイント計算（仮実装）
    private void CalculatePoints(List<Player> winners, Player actionPlayer)
    {
        // セルフマッチ判定: 勝者が一人だけで、かつそれが行動者であった場合
        bool isSelfMatch = winners.Count == 1 && winners[0] == actionPlayer;
        if (isSelfMatch)
        {
            // セルフマッチ
            winners[0].totalPoints += 20; // セルフマッチは20クレジット
            winners[0].wins++;
            Debug.Log($"{winners[0].playerName} がセルフマッチで20クレジット獲得!");
        }
        else
        {
            // トレンドライド（複数可能
            foreach (Player winner in winners)
            {
                winner.totalPoints += 10; // 勝者は10クレジット
                winner.wins++;
                Debug.Log($"{winner.playerName}が10クレジット獲得!");
            }
        }
        // 敗者の処理（例: 手札の合計値だけ失点）
        foreach(Player player in players)
        {
            if(!winners.Contains(player)) // 勝者のリストに組まれていない場合
            {
                int penalty = GetHandValue(player.hand).totalValue;
                // マイナスカード導入後はこのロジックは要見直し
                player.totalPoints -= Mathf.Abs(penalty); // 合計値の絶対値分を失点
                Debug.Log($"{player.playerName} は {Mathf.Abs(penalty)} クレジット失点。");
            }
        }
    }
    // 総合勝利判定
    private Player CheckForOverallWinner()
    {
        foreach (Player player in players)
        {
            if (GetProgressFlag()==0 && player.wins > 0)
            {
                return player;
            }
            else if (GetProgressFlag()==1 && player.wins >= 3)
            {
                return player;
            }
            else if (GetProgressFlag()==2 && player.wins >= 2)
            {
                return player;
            }
            else if (GetProgressFlag()==3 && player.wins >= 5 && player.hand.Count <= 3)
            {
                return player;
            }
            else if (GetProgressFlag()==4 && player.wins >= 5 && player.hand.Any(c => c.sector == CardSector.Eye))
            {
                return player;
            }
            else if (GetProgressFlag()==5)
            {
                return player;
            }
        }
        return null;
    }
    // 次のラウンドを開始する
    public void StartNextRound()
    {
        switch (GetProgressFlag())
        {
            case 0:
                if (currentRound >= 3)
                {
                    SceneManager.LoadSceneAsync("DisqualificationBeforeIdeology");
                }
                else
                {
                    UIManager.Instance.SetGoalTextDependOnProgress(3-currentRound);
                }
                break;
            case 1:
                UIManager.Instance.SetGoalTextDependOnProgress(0);
                break;
            case 2:
                if (currentRound >= 5)
                {
                    SceneManager.LoadSceneAsync("DisqualificationBeforeIdeology");
                }
                else
                {
                    UIManager.Instance.SetGoalTextDependOnProgress(5 - currentRound);
                }
                break;
            case 3:
            case 4:
            case 5:
            default:
                UIManager.Instance.SetGoalTextDependOnProgress(0);
                break;
        }
        Debug.Log("--- 次のラウンドを開始します ---");
        currentRound++; // ラウンド数を増やす

        // Sigma Speak フラグをラウンド開始時にリセット
        sigmaSpeakActive = false;
        sigmaSpeakUsedThisTurn = false;
        sigmaSpeakActivatorIndex = -1;
        memoryHoleUsedThisTurn = false;
        // UI更新
        UIManager.Instance.UpdateRoundText(currentRound);

        // 1. 全員の手札をクリア
        foreach (Player player in players)
        {
            player.hand.Clear();
            player.revealedCards.Clear();     // ラウンド開始時に公開カードをクリア
            player.interrogatedCards.Clear(); // ラウンド開始時に尋問カードをクリア
        }

        // 2. 山札と捨て札をリセット
        SetUpDeck(); // 山札の準備とシャッフル

        // 3. 全員に7枚ずつ配る
        foreach (Player player in players)
        {
            DrawCards(player.hand, 7);
        }
        DealIdeologyCard(); // イデオロギーカードを配る

        // 5. UIをリセット・更新
        SortPlayerCardDataByNumber();
        UIManager.Instance.UpdateAllHandVisuals();
        UIManager.Instance.HideBribeSelectionUI();
        UIManager.Instance.HideTargetSelectionUI();
        UIManager.Instance.ResetLog();
        // 最初の1枚を場に出す
        StartGame(); // 既存のロジックを再利用

        // 6. ターンをリセット
        currentPlayerIndex = 0;
        isTurnClockwise = true;

        // 7. 最初のプレイヤーのターンの開始
        StartCoroutine(TurnTransitionRoutine(CardEffect.None));
    }
    public void RestartGame()
    {
        Debug.Log("--- 新しいゲームを開始します ---");
        // 1. スコアとラウンドをリセット
        foreach (Player player in players)
        {
            player.totalPoints = 0;
        }
        currentRound = 0; // StartNextRoundで+1されるので0に
        // 2. UIのスコア表示をリセット
        UIManager.Instance.UpdateScoreboard(players);
        // 3. 次のラウンド（最初のラウンド）を開始
        StartNextRound();
    }
    // CPUのターンを実行する（NextTurnから呼ばれる）
    protected virtual void ExecuteCPUTurn()
    {
        // CPUが考えているように見せるため、数秒後に実行する
        StartCoroutine(CPUTurnRoutine());
    }
    // CPUの思考ロジック本体
    private IEnumerator CPUTurnRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        Player currentCPU = players[currentPlayerIndex];

        // SigmaSpeak: 手札にあれば自動発動（フリーアクション）
        if (!sigmaSpeakUsedThisTurn && PlayerHasIdeologyInHand(currentCPU, IdeologyType.SigmaSpeak))
        {
            ActivateSigmaSpeak(currentPlayerIndex);
        }
        // MemoryHole: 手札にあれば自動実行してターン終了
        if (PlayerHasIdeologyInHand(currentCPU, IdeologyType.MemoryHole))
        {
            if (TryCPUExecuteMemoryHole(currentCPU)) yield break;
        }

        // 1. 出すカードを決める
        CardData cardToPlay = FindBestCardForCPU(currentCPU);

        // 2. 出せるカードがあった場合
        if (cardToPlay != null)
        {
            Debug.Log($"[CPU] {currentCPU.id} が {cardToPlay.cardName} をプレイ");
            int cardIndex = currentCPU.hand.IndexOf(cardToPlay);
            Debug.Log(cardIndex);
            currentCPU.hand.Remove(cardToPlay);
            yield return StartCoroutine(UIManager.Instance.ShowCPUPlayCardAnimation(currentPlayerIndex, cardToPlay, cardIndex));
            PlayCardToField(cardToPlay, currentCPU);
            yield return StartCoroutine(UIManager.Instance.ShowCPUHandGapFill(currentPlayerIndex));
            UIManager.Instance.UpdateAllHandVisuals(); // ここで自動的にYourTrendも更新される

            // 3. マッチ判定と次のターン（ロジックを分離）
            List<Player> trendRideWinners = CheckForTrendRide(currentCPU);
            if (trendRideWinners.Count > 0)
            {
                SetInputLock(true);
                Debug.Log($"[CPU] {currentCPU.playerName} が勝利しました!");
                StartCoroutine(StartRoundEndSequence(trendRideWinners, currentCPU, WinType.TrendRide));
                yield break; // 勝利したらターンを回さない
            }
            // CPUのセルフマッチ判定
            if(CheckForSelfMatch(currentCPU))
            {
                SetInputLock(true);
                List<Player> winners = new List<Player> { currentCPU };
                StartCoroutine(StartRoundEndSequence(winners, currentCPU, WinType.SelfMatch));
                yield break;
            }
            // 効果処理コルーチンを呼ぶ
            StartCoroutine(HandleCardEffectAndTransition(cardToPlay));
        }
        // 4. 出せるカードがなかった場合
        else
        {
            Debug.Log($"[CPU] {currentCPU.id} はカードを出せず、一枚引く");
            DrawCards(currentCPU.hand, 1);

            // CPUドロー時の勝利判定
            List<Player> trendRideWinners = CheckForTrendRide(currentCPU);
            if (trendRideWinners.Count > 0)
            {
                SetInputLock(true);
                StartCoroutine(StartRoundEndSequence(trendRideWinners, currentCPU, WinType.TrendRide));
                yield break;
            }
            if(CheckForSelfMatch(currentCPU))
            {
                SetInputLock(true);
                List<Player> winners = new List<Player> { currentCPU };
                StartCoroutine(StartRoundEndSequence(winners, currentCPU, WinType.SelfMatch));
                yield break;
            }
            SortPlayerCardDataByNumber();
            UIManager.Instance.UpdateAllHandVisuals(); // UI（CPUの手札枚数）を更新
            UIManager.Instance.UpdateDeckVisual(deck.Count);

            NextTurn(); // 効果なしで次のターンへ
        }
    }
    // CPU MemoryHole AI: 最適なターゲットとカードを選んで交換を実行
    private bool TryCPUExecuteMemoryHole(Player cpu)
    {
        // 渡すカード: MemoryHole以外の最も低handValueなカード
        CardData executorCard = cpu.hand
            .Where(c => c.ideologyType != IdeologyType.MemoryHole)
            .OrderBy(c => c.handValue)
            .FirstOrDefault();
        if (executorCard == null) return false;

        // ターゲット: 自分以外で手札合計が最も高いプレイヤー
        Player target = players
            .Where(p => p != cpu && p.id != PlayerID.GameMaster && p.hand.Count > 0)
            .OrderByDescending(p => GetHandValue(p.hand).totalValue)
            .FirstOrDefault();
        if (target == null) return false;

        // 奪うカード: 公開済みがあればその中の最高値、なければランダム
        CardData targetCard = target.revealedCards.Count > 0
            ? target.revealedCards.OrderByDescending(c => c.handValue).First()
            : target.hand[UnityEngine.Random.Range(0, target.hand.Count)];

        Debug.Log($"[CPU MemoryHole] {cpu.playerName} → {target.playerName}: {targetCard.cardName} を奪い、{executorCard.cardName} を渡す");
        StartCoroutine(CPUMemoryHoleRoutine(cpu, target, targetCard, executorCard));
        return true;
    }

    private IEnumerator CPUMemoryHoleRoutine(Player cpu, Player target, CardData targetCard, CardData executorCard)
    {
        yield return StartCoroutine(
            UIManager.Instance.ShowCPUMemoryHoleAnimation(cpu, target, executorCard, targetCard));
        ExecuteMemoryHoleEffect(cpu, target, targetCard, executorCard);
        NextTurn();
    }

    // AI Helper Methods

    // 自分から見て「見えていないカード」（山札 + 他人の手札）を取得
    private List<CardData> GetUnseenCards(Player me)
    {
        // 全カードのコピーを作成
        List<CardData> unseen = new List<CardData>(allCardDatabase);

        // 自分の手札を除く
        foreach(CardData card in me.hand)
        {
            unseen.Remove(card);
        }

        // 捨て札（場に出たカード）を除く
        foreach(CardData card in discardPile)
        {
            unseen.Remove(card);
        }

        // BureauBrother 保持者はすべてのプレイヤーの手札が見える
        if (PlayerHasIdeologyInHand(me, IdeologyType.BureauBrother))
        {
            foreach (Player p in players)
            {
                if (p == me) continue;
                foreach (CardData card in p.hand) unseen.Remove(card);
            }
        }

        return unseen;
    }

    // 指定したトレンドになった場合、他プレイヤーが即座にトレンドライド（勝利）するリスクを計算 (0.0 - 1.0)
    private float CalculateTrendRideRisk(int trendValue, Player me, List<CardData> unseenCards)
    {
        float maxRisk = 0f;
        
        foreach(Player player in players)
        {
            if(player == me || player.id == PlayerID.GameMaster) continue;
            
            // 相手の手札枚数
            int handCount = player.hand.Count;
            if(handCount == 0) continue;

            // 簡易ヒューリスティック: 手札枚数が少なく、かつトレンド値が低いほど危険
            float baseRisk = 0f;
            
            // トレンド値が「相手の手札枚数 * 6」以下なら作られる可能性がある
            if (trendValue <= handCount * 6)
            {
                if (trendValue <= 6 && handCount <= 2) baseRisk = 0.8f; // 非常に危険
                else if (trendValue <= 10 && handCount <= 3) baseRisk = 0.5f; // 注意
                else baseRisk = 0.2f; // 低リスク
            }
            
            if(baseRisk > maxRisk) maxRisk = baseRisk;
        }
        
        return maxRisk;
    }

    // 次のターンのセットアップ（自分の手札とトレンドのマッチ）をスコアリング
    private int CalculateSetupScore(int trendValue, Player me)
    {
        int score = 0;
        foreach(CardData card in me.hand)
        {
             // トレンドと同じ数字を持っていれば、次に出しやすい
             if(card.numberValue == trendValue)
             {
                 score += 10;
             }
        }
        return score;
    }

    // Bribe使用時に最適なトレンドを探す
    private int FindBestTrendForBribe(Player cpu)
    {
        List<CardData> unseenCards = GetUnseenCards(cpu);
        int bestTrend = 1;
        float bestScore = float.MinValue;

        for(int trend = 1; trend <= 7; trend++)
        {
            float score = 0f;
            
            // 1. リスク評価 (相手にTrend Rideされるか)
            float risk = CalculateTrendRideRisk(trend, cpu, unseenCards);
            score -= risk * 100f; // 自爆回避優先
            
            // 2. 攻撃評価 (自分の手札で次に勝てるか)
            int myHandSum = GetHandValue(cpu.hand).totalValue; // Bribe使用済み（この時点ではまだdiscardPileに入っていないかも？いやHandleCardEffect呼び出し前にRemove済み）
                                                    // 念の為呼び出し元を確認すると、RunEffectの前にPlayCardToField等は終わっている
            
            int valDiff = myHandSum - trend;
            bool canWinNextTurn = false;
            foreach(CardData card in cpu.hand)
            {
                // 次に出すカード(card) == (残りの手札(HandSum) - Trend) つまり HandSum - card == Trend
                // 移項して HandSum - Trend == card.HandValue ?
                // 違う。勝利条件: (HandSum - cardVal) == Trend
                // つまり cardVal == HandSum - Trend
                
                if(card.handValue == valDiff)
                {
                    canWinNextTurn = true; // 次ターンで上がれる
                    break;
                }
            }
            
            if(canWinNextTurn) score += 200f; // リーチ
            
            // 3. 将来性 (数字出しできるか)
            score += CalculateSetupScore(trend, cpu);

            if(score > bestScore)
            {
                bestScore = score;
                bestTrend = trend;
            }
        }
        return bestTrend;
    }

    // CPUの「脳」（確率・リスク評価ベース）
    private CardData FindBestCardForCPU(Player cpu)
    {
        List<CardData> playableCards = new List<CardData>();

        foreach (CardData card in cpu.hand)
        {
            if (CanPlayCard(card)) playableCards.Add(card);
        }
        if (playableCards.Count == 0) return null;

        List<CardData> unseenCards = GetUnseenCards(cpu);
        
        CardData bestCard = null;
        float bestScore = float.MinValue;

        foreach (CardData card in playableCards)
        {
            float score = 0f;

            // 1. 即勝利（Self Match）チェック (最優先)
            var (futureHandValue, hasDoublethink) = GetHandValue(cpu.hand);
            futureHandValue -= card.handValue;
            int futureTrend = (card.effect == CardEffect.Bribe) ? 0 : card.numberValue; 
            
            // 数字出しでの勝利確定
            if (card.effect != CardEffect.Bribe && futureHandValue == futureTrend && (cpu.hand.Count > 1 || futureHandValue != 0))
            {
                return card; // 即決
            }

            // 2. リスク評価 (Trend Rideされるリスク)
            if (card.effect != CardEffect.Bribe) 
            {
                float risk = CalculateTrendRideRisk(futureTrend, cpu, unseenCards);
                score -= risk * 50f;
            }

            // 3. コスト評価 (高コストカードの処理)
            // 手札に高コストを残すと不利なので早めに出すと加点
            score += card.handValue * 1.5f; 

            // 4. 特殊カードの評価
            if (card.effect == CardEffect.Bribe) score += 25f; // Bribeは強い
            if (card.effect == CardEffect.Suspend) score += 15f; 
            if (card.effect == CardEffect.Reject) score += 10f; 
            if (card.effect == CardEffect.Censor || card.effect == CardEffect.Interrogate) score += 5f; 

            // 5. セットアップ (自分の手札の他カードと合うか)
            if (card.effect == CardEffect.None) 
            {
                 // このカードを出した「後」の場（FutureTrend）に対して、
                 // 残りの手札で数字出し（Match）できるカードがあるか？
                 foreach(CardData remaining in cpu.hand)
                 {
                     if(remaining == card) continue;
                     if(remaining.numberValue == futureTrend)
                     {
                         score += 10f; // コンボがつながる
                         break;
                     }
                 }
            }

            // Debug.Log($"[AI] Eval: {card.cardName}, Score: {score}");

            if (score > bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }
        return bestCard;
    }
    public void PlayerSelectTarget(int targetPlayerIndex)
    {
        if (isPlayerInputLocked == false || targetPlayerIndex < 1 || targetPlayerIndex >= players.Count || targetPlayerIndex == currentPlayerIndex)
        {
            // 不正な呼び出し
            Debug.LogWarning("不正なターゲットです");
            return;
        }
        UIManager.Instance.HideTargetSelectionUI();

        // Memory Hole のターゲット選択
        if (pendingMemoryHole)
        {
            pendingMemoryHole = false;
            UIManager.Instance.ShowMemoryHolePanel(players[targetPlayerIndex], players[0]);
            return;
        }

        // アニメーションとターン遷移を行うコルーチンを起動
        StartCoroutine(SurveyTargetAndEndTurn(targetPlayerIndex));
    }
    // PlayerSelectTargetから呼ばれるコルーチン
    private IEnumerator SurveyTargetAndEndTurn(int targetPlayerIndex)
    {
        Player targetPlayer=players[targetPlayerIndex];
        CardEffect effect=pendingSurveyEffect;
        pendingSurveyEffect=CardEffect.None; // 記憶をリセット
        // UIManagerのアニメーションコルーチンを呼び出して待機
        if(effect==CardEffect.Censor)
        {
            yield return StartCoroutine(UIManager.Instance.ShowCensorAnimation(targetPlayer));
        }
        else // Interrogate
        {
            yield return StartCoroutine(UIManager.Instance.ShowInterrogateAnimation(targetPlayer));
        }
        // アニメーションが終わったら次のターンへ
        StartCoroutine(TurnTransitionRoutine(CardEffect.None));
    }
    // 入力ロックとUIを同期させる
    private void SetInputLock(bool isLocked)
    {
        isPlayerInputLocked = isLocked;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPlayerControlsActive(!isLocked);
        }
    }
    // 勝利確認ボタンによって呼ばれるメソッド
    public virtual void PlayerConfirmWin()
    {
        // 待機中以外は無視
        if (!isWaitingForWinConfirmation)
        {
            return;
        }
        Player humanPlayer = players[0];
        isWaitingForWinConfirmation = false;
        UIManager.Instance.ShowWinButton(false);
        SetInputLock(true);
        Debug.Log($"セルフマッチ! {humanPlayer.playerName} が勝利!");
        // 勝利シーケンスを開始（引数に「行動した人」を渡す）
        List<Player> roundWinners = new List<Player> { humanPlayer };
        StartCoroutine(StartRoundEndSequence(roundWinners, humanPlayer, WinType.SelfMatch));
    }
    // UIManagerのボタンから呼ばれるメソッド
    public void OnContinueClicked()
    {
        isWaitingForContinueClick = true;
    }
    // クリック待ちを行う汎用コルーチン
    public IEnumerator WaitForContinueCLick()
    {
        // 1. クリックを促すUIを表示
        UIManager.Instance.ShowContinueButton(true);
        // 2. フラグをリセット
        isWaitingForContinueClick = false;
        // 3. フラグが立つまで待機
        while (!isWaitingForContinueClick)
        {
            yield return null; // 次のフレームまで待つ
        }
        // 4. UIを非表示
        UIManager.Instance.ShowContinueButton(false);
    }
    // 絵柄と数字を指定して、データベースから該当するカードデータを探すメソッド
    public CardData GetCardDataBySectorAndNumber(CardSector sector, int number)
    {
        foreach(CardData data in allCardDatabase)
        {
            // 効果なしカード（数字カード）で、かつセクターと数字が一致するものを探す
            if(data.effect==CardEffect.None && data.sector==sector&&data.numberValue==number)
            {
                return data;
            }
        }
        return null; // 見つからなかった場合
    }
    // Bribeの改修として、渡された数字から数字のスプライトのみを返す軽量なメソッドを作成する
    public Sprite GetNumberSprite(int trend)
    {
        switch(trend)
        {
            case 0: return numberSprites[0];
            case 1: return numberSprites[1];
            case 2: return numberSprites[2];
            case 3: return numberSprites[3];
            case 4: return numberSprites[4];
            case 5: return numberSprites[5];
            case 6: return numberSprites[6];
            case 7: return numberSprites[7];
            case 8: return numberSprites[8];
            case 9: return numberSprites[9];
            case 10: return numberSprites[10];
            case 11: return numberSprites[11];
            case 12: return numberSprites[12];
            default: return null;
        }
    }
    // 現在のゲーム進行フラグを取得するメソッド
    public int GetProgressFlag()
    {
        return gameProgressFlag;
    }
    // ゲーム進行フラグを設定するメソッド
    public void SetProgressFlag(int flag)
    {
        gameProgressFlag = flag;
    }
    public IEnumerator ExecutionEndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UIManager.Instance.EnableExecutionObject();
        SoundManager.Instance.PlaySound(bulletDropSound);
        yield return new WaitForSeconds(1.5f); // 演出の長さに応じて調整
        UIManager.Instance.ShowBlackOut();
    }
#if UNITY_EDITOR
    [ContextMenu("Debug: Test TrendRide Animation")]
    private void Debug_TestTrendRideAnimation()
    {
        if (!Application.isPlaying) return;
        Player cpu = players?.Find(p => p.isCPU);
        if (cpu == null) return;
        StartCoroutine(Debug_TrendRideAnimationSequence(cpu));
    }

    private IEnumerator Debug_TrendRideAnimationSequence(Player cpu)
    {
        int cpuIdx = players.IndexOf(cpu);
        UIManager.Instance.UpdateCPUFace(cpuIdx, CPUFaceState.TrendrideWin);
        if (dogNoticeSound != null)
            SoundManager.Instance.PlaySound(dogNoticeSound);
        yield return StartCoroutine(UIManager.Instance.PlayCPUTrendRideWinAnimation(cpuIdx));
        yield return new WaitForSeconds(1.2f);
        UIManager.Instance.ShowTrendRideAlert(true, new List<Player>{cpu}, players[0]);
        yield return StartCoroutine(WaitForContinueCLick());
        UIManager.Instance.ShowTrendRideAlert(false, null, null);

    }
#endif
    // デバッグ用初期化メソッド
    public static void InitializeForDebug(int debugFlag)
    {
        if(Instance==null)
        {
            GameObject gameObject=new GameObject("GameManager");
            Instance=gameObject.AddComponent<GameManager>();
            DontDestroyOnLoad(gameObject);
        }
        Instance.gameProgressFlag=debugFlag;
    }
    private void DealIdeologyCard()
    {
        if(inquiryResponseDatabase.confirmedIdeology != IdeologyType.None)
        {
            players[0].ideologyType = inquiryResponseDatabase.confirmedIdeology;
            switch(players[0].ideologyType)
            {
                case IdeologyType.DoubleThink:
                    players[0].hand.Add(ideologyCardDatabase.First(c => c.cardName == "DoubleThink"));
                    break;
                case IdeologyType.MemoryHole:
                    players[0].hand.Add(ideologyCardDatabase.First(c => c.cardName == "MemoryHole"));
                    break;
                case IdeologyType.SigmaSpeak:
                    players[0].hand.Add(ideologyCardDatabase.First(c => c.cardName == "SigmaSpeak"));
                    break;
                case IdeologyType.BureauBrother:
                    players[0].hand.Add(ideologyCardDatabase.First(c => c.cardName == "BureauBrother"));
                    break;
                case IdeologyType.Thoughtcrime:
                    players[0].hand.Add(ideologyCardDatabase.First(c => c.cardName == "Thoughtcrime"));
                    break;
                default:
                    break;
            }

            // Flag5 のとき、CPU にもイデオロギーカードをランダムに配布（プレイヤーと重複しない）
            if (GetProgressFlag() >= 5)
            {
                List<IdeologyType> pool = new List<IdeologyType>
                {
                    IdeologyType.SigmaSpeak,
                    IdeologyType.MemoryHole,
                    IdeologyType.BureauBrother,
                    IdeologyType.DoubleThink,
                };
                pool.Remove(players[0].ideologyType); // プレイヤーと重複しない

                for (int i = 1; i < players.Count; i++)
                {
                    if (pool.Count == 0) break;
                    int pick = UnityEngine.Random.Range(0, pool.Count);
                    IdeologyType chosen = pool[pick];
                    pool.RemoveAt(pick);

                    CardData card = ideologyCardDatabase.FirstOrDefault(c => c.ideologyType == chosen);
                    if (card != null)
                    {
                        players[i].hand.Add(card);
                        Debug.Log($"[DealIdeologyCard] {players[i].playerName} に {card.cardName} を配布");
                    }
                }
            }

            SortPlayerCardDataByNumber();
            UIManager.Instance.UpdateAllHandVisuals();
        }
        else
        {
            Debug.Log("イデオロギーカードは配られませんでした");
        }
    }
    private void SortPlayerCardDataByNumber()
    {
        foreach(Player player in players)
        {
            player.hand.Sort((c1, c2) => c2.numberValue - c1.numberValue);
        }
    }
}
public enum PlayerID
{
    Player,
    CPU,
    GameMaster
}