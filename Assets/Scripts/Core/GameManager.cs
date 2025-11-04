using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

    void Awake()
    {
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
        players.Add(new Player(PlayerID.Player, false)); // 0番目が人間
        players.Add(new Player(PlayerID.CPU, true));    // 1番目がCPU
        players.Add(new Player(PlayerID.CPU, true));    // 2番目がCPU
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
    }
    // Fisher-Yatesアルゴリズムを使い、山札をシャッフルするメソッド
    public void ShuffleDeck()
    {
        Assert.IsNotNull(deck, "デッキが空なのでシャッフルできません");
        for (int i = 0; i < deck.Count; i++)
        {
            int rand = Random.Range(i, deck.Count);
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
            PlayCardToField(firstCard, PlayerID.GameMaster); // 最初のカードを場に出す
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
    public void PlayCardToField(CardData card, PlayerID player)
    {
        discardPile.Add(card);
        currentCardOnField = card;
        // メッセージを作成
        string playerName = player.ToString();
        string message = $"[{playerName}] played [{card.cardName}]";
        // UIManagerにログ表示を依頼
        UIManager.Instance.AddLogMessage(message, card.cardIcon);
        // TODO: Bribeの場合の数字設定の処理を追加
        if (card.effect == CardEffect.Bribe)
        {
            Debug.Log("ワイルドカードが場に出されました。プレイヤーは数字を宣言してください。");
            // TODO: プレイヤーに数字を選ばせるUIを実装
            // 仮に5を選んだとする
            currentTrendValue = 5;
        }
        else
        {
            // 場のトレンド（数字）を更新
            currentTrendValue = card.numberValue;
        }
        Debug.Log("場に " + card.cardName + " が出されました。現在のトレンド: " + currentTrendValue);
        UIManager.Instance.UpdateFieldPileUI(card);
        // TODO: ここで全プレイヤーのマッチ判定を呼び出す
    }
    // カードが出せるかを判定するメソッド
    public bool CanPlayCard(CardData cardToPlay)
    {
        // 1. cardToPlay.effect == CardEffect.Bribe (賄賂) なら true
        if (cardToPlay.effect == CardEffect.Bribe)
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
        if (cardToPlay.effect == CardEffect.None && currentCardOnField.effect == CardEffect.None && cardToPlay.numberValue == currentTrendValue)
        {
            return true;
        }
        // 5. 調査カード (Censor, Interrogate) は場には出せない (効果使用のみ)
        if (cardToPlay.effect == CardEffect.Censor || cardToPlay.effect == CardEffect.Interrogate)
        {
            return false;
        }
        return false;
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
        // プレイヤーのターンかチェック
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
        // カードを出せる場合の処理を続ける
        Player humanPlayer = players[currentPlayerIndex];
        humanPlayer.hand.Remove(cardToPlay);
        PlayCardToField(cardToPlay, humanPlayer.id);

        // UIを更新
        UIManager.Instance.UpdateAllHandVisuals(); ;

        // TODO: マッチ判定
        // TODO: CPUのターンを呼び出す
        if (!CheckForMatch(humanPlayer))
        {
            NextTurn();
        }
        else
        {
            // TODO: 勝利演出
        }
    }
    // ターンを次のプレイヤーに進めるメソッド
    public void NextTurn()
    {
        // TODO: ここでSuspend（スキップ）やReject（リバース）の処理を実装
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
        Debug.Log($"--- {players[currentPlayerIndex].id} のターン ---");
        // 次の人がCPUなら、CPUの試行ルーチンを呼ぶ
        if (players[currentPlayerIndex].isCPU)
        {
            // ExecuteCPUTurn();
        }
    }
    // マッチ（勝利）判定を行うメソッド
    public bool CheckForMatch(Player cardPlayer)
    {
        // 1. トレンドライド（他人ドボン）のチェック
        bool trendRideWin = false;
        foreach (Player player in players)
        {
            if (player == cardPlayer) continue; // カードを出した人は他人ドボンの対象には論理的にならないので除く
            if (GetHandValue(player.hand) == currentTrendValue)
            {
                // 手札0枚での「0」マッチは禁止
                if (player.hand.Count > 0 || currentTrendValue != 0)
                {
                    Debug.Log($"トレンドライド! {player.id} が勝利!");
                    trendRideWin = true;
                    // TODO: 勝利したplayerのポイントを加算
                }
            }
            if (trendRideWin) return true; // 他人ドボンが最優先

            // 2. セルフマッチ（自分ドボン）のチェック
            if (GetHandValue(cardPlayer.hand) == currentTrendValue)
            {
                if (cardPlayer.hand.Count > 0 || currentTrendValue != 0)
                {
                    // Bribeでの上がり禁止チェック
                    if (currentCardOnField.effect == CardEffect.Bribe)
                    {
                        Debug.Log("Bribe（賄賂）では上がれません!");
                        return false;
                    }

                    Debug.Log($"セルフマッチ! {cardPlayer.id} が勝利!");
                    // TODO: cardPlayerのポイントを加算
                    return true;
                }
            }
        }
        return false; // 誰もマッチしなかった場合
    }
}
public enum PlayerID
{
    Player,
    CPU,
    GameMaster
}
