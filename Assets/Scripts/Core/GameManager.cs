using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    // シングルトンの設定
    // 'Instance' を通じて他のスクリプトからGameManagerの機能にアクセスできる
    public static GameManager Instance { get; private set; }
    [Header("カードデータ")]
    public List<CardData> allCardDatabase;
    [Header("ゲームの状態")]
    public List<CardData> deck = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    // （デバッグ用）現在の場のカード
    [SerializeField] private CardData currentCardOnField;
    // 現在の「トレンド（場の数字）」
    [SerializeField] private int currentTrendValue = 0;

    // プレイヤーの管理（本実装）
    public List<Player> players = new List<Player>();
    private int currentPlayerIndex = 0;
    private bool isTurnClockwise = true; // ターン進行方向（Reject用）
    public bool isPlayerInputLocked = false; // 操作ロックようフラグ
    private Player gameMaster;
    // どの調査カードが使われたか記憶する変数
    private CardEffect pendingSurveyEffect = CardEffect.None;
    private int winningScore = 50;

    void Awake()
    {
        gameMaster = new Player(PlayerID.GameMaster, false, "GameMaster", 0);
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンを跨いでも消えない
        }
        else
        {
            Destroy(gameObject); // 既にインスタンスが存在する場合は破棄
        }
    }
    void Start()
    {
        // 3人対戦のセットアップ
        players.Clear();
        players.Add(new Player(PlayerID.Player, false, "Ian", 0)); // 0番目が人間
        players.Add(new Player(PlayerID.CPU, true, "CPU_1", 0));    // 1番目がCPU
        players.Add(new Player(PlayerID.CPU, true, "CPU_2", 0));    // 2番目がCPU
        // ゲーム開始時に山札を準備
        SetUpDeck();
        // 全プレイヤーにカードを配る
        foreach(Player player in players)
        {
            DrawCards(player.hand, 7);
        }
        // 最初の1枚を場に出す
        StartGame();
        // プレイヤー（0番目）の手札をUIに反映
        UIManager.Instance.UpdateAllHandVisuals();
    }

    // Update is called once per frame
    void Update()
    {

    }
    // 山札を初期化し、シャッフルするメソッド
    public void SetUpDeck()
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
        Assert.IsNotNull(deck, "デッキが空なのでシャッフルできません");
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

            // (デバッグログはコンソールが荒れるので、必要な方だけ残します)
            if (hand == players[0].hand)
            {
                Debug.Log("プレイヤーが引いたカード: " + drawnCard.cardName);
                // UIManager.Instance.UpdatePlayerHandUI(playerHand);
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
    public void StartGame()
    {
        // 山札から「効果なし(None)」のカードを「探す」

        int firstCardIndex = -1;
        for (int i = 0; i < deck.Count; i++)
        {
            if (deck[i].effect == CardEffect.None)
            {
                firstCardIndex = i;
                break;
            }
        }

        if (firstCardIndex != -1)
        {
            // 見つかった場合
            CardData firstCard = deck[firstCardIndex];
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
    }
    // カードを場（捨て札）に出す処理
    public void PlayCardToField(CardData card, Player player)
    {
        discardPile.Add(card);
        currentCardOnField = card;
        // メッセージを作成
        string playerName = player.playerName;
        string message = $"{DateTime.Now} [{playerName}] played [{card.cardName}]";
        // UIManagerにログ表示を依頼
        UIManager.Instance.AddLogMessage(message, card.cardIcon);
        // TODO: Bribeの場合の数字設定の処理を追加
        if (card.effect == CardEffect.Bribe ||
            card.effect == CardEffect.Censor ||
            card.effect==CardEffect.Interrogate)
        {
            // Bribe/Censor/Interrogateが出た直後は
            // currentTrendValueは「前のカード」の値を保持したまま。
            // これにより、CheckForMatchが誤作動なくなる。
        }
        else
        {
            // 場のトレンド（数字）を更新
            currentTrendValue = card.numberValue;
        }
        Debug.Log("場に " + card.cardName + " が出されました。現在のトレンド: " + currentTrendValue);
        UIManager.Instance.UpdateFieldPileUI(card);
        UIManager.Instance.UpdateAllHandVisuals();
        // TODO: ここで全プレイヤーのマッチ判定を呼び出す
    }
    // カードが出せるかを判定するメソッド
    public bool CanPlayCard(CardData cardToPlay)
    {
        // 1. cardToPlay.effect == CardEffect.Bribe (賄賂) なら true
        if (cardToPlay.effect == CardEffect.Bribe ||
            cardToPlay.effect == CardEffect.Censor ||
            cardToPlay.effect == CardEffect.Interrogate)
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
        return false;
    }
    // DrawButtonから呼ばれるメソッド
    public void PlayerDrawCard()
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
        UIManager.Instance.UpdateAllHandVisuals();
        UIManager.Instance.UpdateDeckVisual(deck.Count);

        // 4. マッチ判定（ドローした時にセルフマッチする可能性はあるよね...?）
        List<Player> roundWinners = CheckForMatch(humanPlayer);
        if (roundWinners.Count > 0)
        {
            // TODO: 勝利演出
            SetInputLock(true);
            Debug.Log($"セルフマッチ! {humanPlayer.playerName} が勝利!");
            return;
        }
        else
        {
            NextTurn();
        }
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

        UIManager.Instance.HideBribeSelectionUI();

        // ターンを次に回す。CPUではないことが保証されているので回して良い。
        NextTurn();
    }
    // プレイヤーの手札の合計値を計算するメソッド
    public int GetHandValue(List<CardData> hand)
    {
        int totalValue = 0;
        foreach (CardData card in hand)
        {
            totalValue += card.handValue;
        }
        return totalValue;
    }
    public void TryPlayCard(CardData cardToPlay)
    {
        // 1. 操作ロックをチェック
        if (isPlayerInputLocked)
        {
            return;
        }
        // 2. プレイヤーのターンかチェック
        if (players[currentPlayerIndex].isCPU)
        {
            Debug.LogWarning("現在はCPUのターンです。プレイヤーはカードを出せません。");
            return;
        }
        if (!CanPlayCard(cardToPlay))
        {
            Debug.Log("このカードは出せません: " + cardToPlay.cardName);
            // TODO: 出せない場合のフィードバックをUIに表示
            return;
        }
        // 3. カードを出せる場合の処理を続ける
        Player humanPlayer = players[currentPlayerIndex];
        humanPlayer.hand.Remove(cardToPlay);
        PlayCardToField(cardToPlay, humanPlayer); // UI更新もこの中で行われる

        // 4. マッチ判定
        List<Player> roundWinners = CheckForMatch(humanPlayer);
        if (roundWinners.Count > 0)
        {
            SetInputLock(true);
            Debug.Log($"セルフマッチ! {humanPlayer.playerName} が勝利!");
            return; // 勝利したのでターンを回さない
        }
        // 5. マッチしなかった場合、効果処理とターン送り
        // 操作をロックし、効果処理コルーチン開始
        SetInputLock(true);
        StartCoroutine(HandleCardEffectAndTransition(cardToPlay.effect));
    }
    // 効果なしでターンを終える時専用
    public void NextTurn()
    {
        StartCoroutine(TurnTransitionRoutine(CardEffect.None));
    }
    // 勝利演出　=> ポイント計算 => 次ラウンド準備の流れを管理
    private IEnumerator StartRoundEndSequence(List<Player> winners)
    {
        // 1. 勝利演出（UIに任せる）
        // 他のプレイヤーの手札も全て公開する
        UIManager.Instance.RevealAllHands();
        yield return StartCoroutine(UIManager.Instance.ShowWinnerAnimation(winners));

        // ポイント計算
        CalculatePoints(winners);
        UIManager.Instance.UpdateScoreboard(players); // スコアボードUIを更新

        // 3. 最終勝利判定
        Player overallWinner = CheckForOverallWinner();
        if (overallWinner != null)
        {
            // ゲーム終了
            yield return StartCoroutine(UIManager.Instance.ShowGameEndAnimation(overallWinner));
            Debug.Log($"最終勝者: {overallWinner.playerName}");
        }
        else
        {
            // 4. 次ラウンドへの移行準備
            yield return new WaitForSeconds(3.0f); // 結果表示
            StartNextRound();
        }
    }
    private IEnumerator HandleCardEffectAndTransition(CardEffect playedEffect)
    {
        // 1. カードを出した本人が実行する効果処理
        Player cardPlayer = players[currentPlayerIndex];
        if (playedEffect == CardEffect.Bribe)
        {
            if (cardPlayer.isCPU)
            {
                int chosenTrend = UnityEngine.Random.Range(1, 6); // AIはあとで賢くする
                currentTrendValue = chosenTrend;
                Debug.Log($"Bribe: CPUがトレンドを{currentTrendValue} に設定しました。");
                StartCoroutine(TurnTransitionRoutine(playedEffect));
            }
            else
            {
                // プレイヤーの入力待ち
                UIManager.Instance.ShowBribeSelectionUI();
                // // PlayerSelectBribeTrendが呼ばれるまで、このコルーチンはここで「待機」
                // (PlayerSelectBribeTrendがNextTurn()を呼ぶ)
                yield break; // コルーチンを終了し、ボタン入力を終了し、ボタン入力を待つ
            }
        }
        else if (playedEffect == CardEffect.Censor || playedEffect == CardEffect.Interrogate)
        {
            pendingSurveyEffect = playedEffect;
            if (cardPlayer.isCPU)
            {
                int targetIndex = (currentPlayerIndex == 1) ? 2 : 1;
                // CPUは即座に実行するが、見せるために少し待つ
                yield return new WaitForSeconds(1.0f);
                PlayerSelectTarget(targetIndex);
                yield break; // PlayerSelelctTargetがNextTurn()を呼ぶ
            }
            else
            {
                UIManager.Instance.ShowTargetSelectionUI();
                yield break; // PlayerSelectTargetがNextTurn()を呼ぶ
            }
        }
        else
        {
            // 2. ターン遷移（Bribe/Censor/Interrogate 以外の場合）
            // 以前のNextTurn(playedEffect)のロジックをここに持ってくる
            StartCoroutine(TurnTransitionRoutine(playedEffect));
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
            UIManager.Instance.UpdateAllHandVisuals();
        }
        // ターン開始
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
    // マッチ（勝利）判定を行うメソッド
    public List<Player> CheckForMatch(Player cardPlayer)
    {
        List<Player> winners = new List<Player>();
        // 1. トレンドライド（他人ドボン）のチェック
        foreach (Player player in players)
        {
            if (player == cardPlayer)
            {
                continue; // カードを出した人は他人ドボンの対象には論理的にならないので除く
            }
            if (GetHandValue(player.hand) == currentTrendValue)
            {
                // 手札0枚での「0」マッチは禁止
                if (player.hand.Count > 0 || currentTrendValue != 0)
                {
                    // 0-0マッチ禁止ルールを適用する。
                    if (player.hand.Count > 0 || currentTrendValue != 0)
                    {
                        Debug.Log($"トレンドライド! {player.id} が勝利!");
                        winners.Add(player);
                        // TODO: 勝利したplayerのポイントを加算
                    }
                }
            }
        }
        if (winners.Count > 0) return winners; // 他人ドボンが最優先
        // 2. セルフマッチ（自分ドボン）のチェック
        if (GetHandValue(cardPlayer.hand) == currentTrendValue)
        {
            if (cardPlayer.hand.Count > 0 || currentTrendValue != 0)
            {
                // Bribeでの上がり禁止チェック
                if (currentCardOnField.effect == CardEffect.Bribe)
                {
                    Debug.Log("Bribe（賄賂）では上がれません!");
                    return winners; // 空のリスト
                }

                Debug.Log($"セルフマッチ! {cardPlayer.id} が勝利!");
                // TODO: cardPlayerのポイントを加算
                winners.Add(cardPlayer);
            }
        }
        return winners; // 誰もマッチしなかった場合
    }
    // ポイント計算（仮実装）
    private void CalculatePoints(List<Player> winners)
    {
        foreach (Player winner in winners)
        {
            winner.totalPoints += 10; // 勝者は10クレジット
            Debug.Log($"{winner.playerName}が10クレジット獲得!");
        }
        // TODO: 敗者は手札の合計値分減点などの処理
    }
    // 総合勝利判定
    private Player CheckForOverallWinner()
    {
        foreach (Player player in players)
        {
            if (player.totalPoints >= winningScore)
            {
                return player;
            }
        }
        return null;
    }
    // 次のラウンドを開始する
    public void StartNextRound()
    {
        Debug.Log("--- 次のラウンドを開始します ---");

        // 1. 全員の手札をクリア
        foreach (Player player in players)
        {
            player.hand.Clear();
        }

        // 2. 山札と捨て札をリセット
        SetUpDeck(); // 山札の準備とシャッフル

        // 3. 全員んい7枚ずつ配る
        foreach (Player player in players)
        {
            DrawCards(player.hand, 7);
        }
        // 最初の1枚を場に出す
        StartGame(); // 既存のロジックを再利用

        // 5. UIをリセット・更新
        UIManager.Instance.UpdateAllHandVisuals();
        UIManager.Instance.HideBribeSelectionUI();
        UIManager.Instance.HideTargetSelectionUI();

        // 6. ターンをリセット
        currentPlayerIndex = 0;
        isTurnClockwise = true;

        // 7. 最初のプレイヤーのターンの開始
    }
    // CPUのターンを実行する（NextTurnから呼ばれる）
    private void ExecuteCPUTurn()
    {
        // CPUが考えているように見せるため、数秒後に実行する
        Invoke("CPUTurnLogic", UnityEngine.Random.Range(2f, 4.5f));
    }
    // CPUの思考ロジック本体
    private void CPUTurnLogic()
    {
        Player currentCPU = players[currentPlayerIndex];
        // 1. 出すカードを決める
        CardData cardToPlay = FindBestCardForCPU(currentCPU);

        // 2. 出せるカードがあった場合
        if (cardToPlay != null)
        {
            Debug.Log($"[CPU] {currentCPU.id} が {cardToPlay.cardName} をプレイ");
            currentCPU.hand.Remove(cardToPlay);
            PlayCardToField(cardToPlay, currentCPU);

            // 3. マッチ判定と次のターン
            List<Player> roundWinners = CheckForMatch(currentCPU);
            if (roundWinners.Count > 0)
            {
                SetInputLock(true);
                Debug.Log($"[CPU] {currentCPU.playerName} が勝利しました!");
                // TODO: 勝利演出
                return; // 勝利したらターンを回さない
            }
            // 効果処理コルーチンを呼ぶ
            StartCoroutine(HandleCardEffectAndTransition(cardToPlay.effect));
        }
        // 4. 出せるカードがなかった場合
        else
        {
            Debug.Log($"[CPU] {currentCPU.id} はカードを出せず、一枚引く");
            DrawCards(currentCPU.hand, 1);
            UIManager.Instance.UpdateAllHandVisuals(); // UI（CPUの手札枚数）を更新
            UIManager.Instance.UpdateDeckVisual(deck.Count);

            NextTurn(); // 効果なしで次のターンへ
        }
    }
    // CPUの「脳」（貪欲法）
    private CardData FindBestCardForCPU(Player cpu)
    {
        List<CardData> playableCards = new List<CardData>();

        // 1. 出せるカードを全てリストアップ
        foreach (CardData card in cpu.hand)
        {
            if (CanPlayCard(card))
            {
                playableCards.Add(card);
            }
        }
        if (playableCards.Count == 0)
        {
            return null; // 出せるカードがない
        }
        // 2. 貪欲法（Greedy Algorithm）で「最善」のカードを選ぶ
        // 優先度1: セルフマッチできるカード（Bribe以外）
        foreach (CardData card in playableCards)
        {
            if (card.effect == CardEffect.Bribe)
            {
                continue;
            }
            // もしこのカードを出したら...
            int futureTrend = card.numberValue;
            int futureHandValue = GetHandValue(cpu.hand) - card.handValue;

            if (futureHandValue == futureTrend && (cpu.hand.Count > 1 || futureHandValue != 0))
            {
                return card; // 勝利する
            }
        }
        // 優先度2: 手札コストの高いカード（15）を捨てる（Bribe, Censor, Interrogate）
        foreach (CardData card in playableCards)
        {
            if (card.handValue == 15)
            {
                return card; // 高コストカードを素早く手放す
            }
        }
        // 優先度3: 効果付きカード（Audit, Suspend, Reject）
        foreach (CardData card in playableCards)
        {
            if (card.effect == CardEffect.Audit ||
                card.effect == CardEffect.Suspend ||
                card.effect == CardEffect.Reject)
            {
                return card;
            }
        }
        // 優先度4: 残った出せるカード（数字カード）からランダムに1枚
        return playableCards[UnityEngine.Random.Range(0, playableCards.Count)];
    }
    public void PlayerSelectTarget(int targetPlayerIndex)
    {
        if (isPlayerInputLocked == false || targetPlayerIndex < 1 || targetPlayerIndex >= players.Count)
        {
            // 不正な呼び出し
            return;
        }
        UIManager.Instance.HideTargetSelectionUI();
        Player targetPlayer = players[targetPlayerIndex];

        // 記憶していた効果によって処理を分岐
        if (pendingSurveyEffect == CardEffect.Censor)
        {
            // Censor（ランダム1枚開示）のロジック
            if (targetPlayer.hand.Count == 0)
            {
                StartCoroutine(UIManager.Instance.ShowEffectResult("対象の手札は0枚です"));
            }
            else
            {
                CardData randomCard = targetPlayer.hand[UnityEngine.Random.Range(0, targetPlayer.hand.Count)];
                string resultMessage = $"{targetPlayer.playerName} の手札を検閲: [{randomCard.cardName}]";
                StartCoroutine(UIManager.Instance.ShowEffectResult(resultMessage));
            }
        }
        else if (pendingSurveyEffect == CardEffect.Interrogate)
        {
            // Interrogate（上下質問）のロジック
            int targetValue = GetHandValue(targetPlayer.hand);
            string resultMessage = (targetValue > 15) ? "合計値は15[以上]です" : "合計値は15未満です";
            StartCoroutine(UIManager.Instance.ShowEffectResult(resultMessage));
        }
        pendingSurveyEffect = CardEffect.None; // 記憶をリセット
        // 使ったカードはPlayCardToFieldの時点で捨て札に送られているのでOK
        NextTurn(); // ターン終了
    }
    // 入力ロックとUIを同期させる
    private void SetInputLock(bool isLocked)
    {
        isPlayerInputLocked = isLocked;
        if(UIManager.Instance!=null)
        {
            UIManager.Instance.SetPlayerControlsActive(!isLocked);
        }
    }
}
public enum PlayerID
{
    Player,
    CPU,
    GameMaster
}
