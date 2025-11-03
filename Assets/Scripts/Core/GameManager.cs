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

    // プレイヤーの管理（仮）
    // 将来的にはAIや複数プレイヤーに対応
    private List<CardData> playerHand = new List<CardData>();
    private List<CardData> cpuHand = new List<CardData>();

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
        // ゲーム開始時に山札を準備
        SetUpDeck();
        // （テスト用）プレイヤーとCPUにカードを配る
        DrawCards(playerHand, 7);
        DrawCards(cpuHand, 7);
        // （テスト用）最初の1枚を馬に出す
        StartGame();
        UIManager.Instance.UpdatePlayerHandUI(playerHand);
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
            if (hand == playerHand)
            {
                Debug.Log("プレイヤーが引いたカード: " + drawnCard.cardName);
                // UIManager.Instance.UpdatePlayerHandUI(playerHand);
            }
        }
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
            PlayCardToField(firstCard);
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
    public void PlayCardToField(CardData card)
    {
        discardPile.Add(card);
        currentCardOnField = card;
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
        UIManager.Instance.UpdateFieldCardUI(card);
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
        if(cardToPlay.effect==CardEffect.Censor||cardToPlay.effect==CardEffect.Interrogate)
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
        if (!CanPlayCard(cardToPlay))
        {
            Debug.Log("このカードは出せません: " + cardToPlay.cardName);
            // TODDO: 出せない場合のフィードバックをUIに表示
            return;
        }
        // カードを出せる場合の処理を続ける
        playerHand.Remove(cardToPlay);
        PlayCardToField(cardToPlay);

        // UIを更新
        UIManager.Instance.UpdatePlayerHandUI(playerHand);

        // TODO: マッチ判定
        // TODO: COUのターンを呼び出す
    }
}
