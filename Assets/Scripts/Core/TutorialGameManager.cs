using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

public class TutorialGameManager : GameManager
{
    [Header("チュートリアルUI構成")]
    [SerializeField] private GameObject dogSide; // NavigatorDog_0
    [SerializeField] private GameObject dogFront; // NavigatorDogFront_0
    [SerializeField] private GameObject tutorialCanvas;
    // セリフ表示用エリア
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TextMeshProUGUI speechText;
    [SerializeField] private Button continueButton; // 画面全体のタップを検知する透明ボタンでも可

    [Header("入力UI")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private VirtualKeyboard virtualKeyboard; // InputFieldの代わりにKeyboard
    [SerializeField] private TextMeshProUGUI nameErrorText;

    [Header("タイプライター")]
    [SerializeField] private TypewriterEffect typewriter;

    [Header("チュートリアルの状態")]
    private int tutorialStep = 0;
    private bool isWaitingForClick = false;
    [Header("サウンド")]
    [SerializeField] private AudioClip dogNotice;
    [Header("ゲーム内UI")]
    [SerializeField] private GameObject roundText;
    [SerializeField] private GameObject scoreBoardPanel;
    [SerializeField] private GameObject currentTrendText;
    [SerializeField] private GameObject yourTrendText;
    [SerializeField] private GameObject statusPanel;

    private Player tutorialMaster=new Player(PlayerID.GameMaster, false, "TutorialMaster", 0);

    // ゲーム状態制御用
    private List<string> tutorialDeckOrder = new List<string>
    {
        // プレイヤーに引かせるカード: 簡単な数字合わせ用
        "Eye_5", "Gear_5", // 山札の上から順
        // CPUに引かせるカード
        "Mask_1", "Chain_1",
        // 以降はランダムで良いが、今回は固定
    };

    protected override void InitializeGame()
    {
        // GameManagerのStart()から呼ばれるが、何もしない（手動で制御するため）
        // ベースの初期化は行わず、チュートリアルコルーチンを開始
        StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        // 1. 初期状態設定
        dogSide.SetActive(true);
        dogFront.SetActive(false);
        speechBubble.SetActive(false);
        nameInputPanel.SetActive(false);
        roundText.SetActive(false);
        scoreBoardPanel.SetActive(false);
        currentTrendText.SetActive(false);
        yourTrendText.SetActive(false);
        statusPanel.SetActive(false);

        UIManager.Instance.SetPlayerControlsActive(false); // 操作ロック

        yield return new WaitForSeconds(1.0f);

        // 2. 犬が気づく
        // TODO: ここでちょっとしたアニメーションやSEを入れると良い
        dogSide.SetActive(false);
        SoundManager.Instance.PlaySound(dogNotice);
        dogFront.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        // 3. 自己紹介
        speechBubble.SetActive(true);
        yield return StartCoroutine(ShowDialogue("...おや？\n見かけない顔だね。新入りかい？"));
        yield return StartCoroutine(ShowDialogue("私の名前は...まあ、好きに呼んでくれ。\nこの端末のナビゲーターのようなものさ。"));

        // 4. 名前入力
        yield return StartCoroutine(ShowDialogue("君の名前を教えてくれないか？\n管理局のデータベースに登録する必要があるんだ。"));
        
        speechBubble.SetActive(false); // 一旦吹き出しを消す
        nameInputPanel.SetActive(true);
        virtualKeyboard.ResetInput(); // 入力リセット
        
        // キーボードのイベント登録
        bool nameSubmitted = false;
        System.Action<string> onNameSubmitAction = (name) => {
             // バリデーション: 英語5文字以内
            if (Regex.IsMatch(name, @"^[a-zA-Z]{1,5}$"))
            {
                PersistentDataManager.Instance.SetPlayerName(name);
                nameErrorText.text = "";
                nameSubmitted = true;
            }
            else
            {
                nameErrorText.text = "Error: Alpha only, max 5 chars.";
            }
        };
        virtualKeyboard.OnConfirm = onNameSubmitAction;

        // 名前入力待ち
        yield return new WaitUntil(() => nameSubmitted); 
        
        // イベント解除（重複防止）
        virtualKeyboard.OnConfirm = null;

        nameInputPanel.SetActive(false);
        speechBubble.SetActive(true);

        string pName = PersistentDataManager.Instance.PlayerName;
        yield return StartCoroutine(ShowDialogue($"なるほど... {pName} だね。\n悪くない名前だ。"));

        // 5. ゲーム開始準備
        yield return StartCoroutine(ShowDialogue("さて、早速だが「ゲーム」の時間だ。"));
        yield return StartCoroutine(ShowDialogue("この世界で生き残るための、唯一の手段を教えてやろう。"));

        roundText.SetActive(true);
        scoreBoardPanel.SetActive(true);
        currentTrendText.SetActive(true);
        yourTrendText.SetActive(true);
        statusPanel.SetActive(true);
        // ここで実際のゲーム初期化処理（2人対戦）
        SetupTutorialGame();

        yield return StartCoroutine(ShowDialogue("画面下にあるのが君の「社会的価値」...手札だ。\n中央にある数字が、管理局の定める「トレンド」だ。"));
        yield return StartCoroutine(ShowDialogue("君の目的は簡単だ。\n自分の価値（手札の合計）を、トレンドに適合させること。"));
        yield return StartCoroutine(ShowDialogue("これを **「マッチ」** と呼ぶ。\n体制に適合できない者は...どうなるか想像がつくだろう？"));

        // 6. ドローの練習
        // 現在の状態: P1手札(Eye_1, Mask_2 = 3), トレンド(Gear_10 = 10) -> 不一致
        yield return StartCoroutine(ShowDialogue("...今の君の価値は「3」だ。\nトレンドは「10」。全く足りていないな。"));
        yield return StartCoroutine(ShowDialogue("何、心配することはない。\n価値が足りないなら、外部から調達すればいい。"));
        yield return StartCoroutine(ShowDialogue("右上の山札からカードを引け。\nこれを **「ドロー」** と呼ぶ。"));

        isPlayerInputLocked = false; // ロック解除
        UIManager.Instance.SetPlayerControlsActive(true);
        tutorialStep = 1; // ドロー待ち

        int previousCount = players[0].hand.Count;
        yield return new WaitUntil(() => players[0].hand.Count > previousCount); // 手札が増えるのを待つ
        
        // カードを引いた後
        // P1手札: Eye_1, Mask_2, Gear_5 (Sum 8) -> まだ10ではない
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("ふむ、カードを引いたか。\n...だが、それでもまだ合計は「8」。トレンドの「10」には届かない。"));
        yield return StartCoroutine(ShowDialogue("こういう時は、手札を出してトレンドそのものを操作するんだ。"));
        yield return StartCoroutine(ShowDialogue("トレンドと同じ「絵柄」か「数字」のカードなら場に出せる。\n今引いた **「Gear_5」** を出してみろ。"));

        // 7. カードプレイの練習
        tutorialStep = 2; // プレイ待ち
        isPlayerInputLocked = false;
        
        int previousHandCount = players[0].hand.Count;
        yield return new WaitUntil(() => players[0].hand.Count < previousHandCount); // 手札が減るのを待つ
        
        // P1が出した: Gear_5 -> トレンド 5
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("そうだ。\nこれでトレンドは「5」に書き換わった。"));
        yield return StartCoroutine(ShowDialogue("カードを出せば、自分の手番は終了する。\n次は私の番だ。"));

        // CPU (DOG) のターン
        CallCPUTurnAction();
        
        // CPUがGear_3を出して、トレンドが3になるのを待つ
        yield return new WaitUntil(() => tutorialStep == 4); 

        // 8. 勝利 (Self Match)
        // 現在: P1手札(Eye_1, Mask_2 = 3), トレンド(3) -> 一致！
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("私がカードを出したことで、トレンドは「3」になったな。"));
        yield return StartCoroutine(ShowDialogue("...気づいたか？\n君の残りの手札を見てみろ。"));
        yield return StartCoroutine(ShowDialogue("1と2...合計は「3」。\n今のトレンドと完全に一致している。"));
        yield return StartCoroutine(ShowDialogue("この状態でカードを出せば、君は勝利する。\nこれを **「セルフマッチ」** と呼ぶ。"));
        yield return StartCoroutine(ShowDialogue("さあ、どちらでもいい。カードを出して証明してみせろ。\n君がこの世界に適合できる人間であることを。"));

        isPlayerInputLocked = false;
        // マッチ判定はGameManager側で行われ、勝利演出が出るはず
        // プレイヤーが最後のカードを出して勝利するのを待つ
        yield return new WaitUntil(() => isWaitingForWinConfirmation || players[0].hand.Count == 0); 

         // 9. エンディング
        yield return new WaitForSeconds(2.0f); // 勝利演出の余韻
        yield return StartCoroutine(ShowDialogue("...見事だ。\nそれがこのゲーム...『ΣIGMA』の基本だ。"));
        yield return StartCoroutine(ShowDialogue("トレンドを見極め、利用し、そして最後には出し抜く。\nそれができなければ、君も前の被験者たちと同じ末路を辿るだろう。"));
        yield return StartCoroutine(ShowDialogue("...ああ、言い忘れていたな。"));
        yield return StartCoroutine(ShowDialogue("このアプリケーションは、なぜかアンインストールできないらしい。\n...君が「適合者」として認められるまで、逃げ場はないということさ。"));
        
        yield return StartCoroutine(ShowDialogue("健闘を祈るよ。\n...せいぜい、私を楽しませてくれ。"));

        // ロビーに戻るボタンなどを表示
        // UIManager.Instance.ShowGameEndAnimation(true, players[0]); // 仮の勝利演出
    }

    private void SetupTutorialGame()
    {
        players.Clear();
        string pName = PersistentDataManager.Instance.PlayerName;
        
        // 2人対戦（プレイヤー vs CPU）
        players.Add(new Player(PlayerID.Player, false, pName, 0));
        players.Add(new Player(PlayerID.CPU, true, "DOG", 0));
        
        currentRound = 1;
        UIManager.Instance.UpdateRoundText(currentRound);
        UIManager.Instance.UpdateScoreboard(players);

        // デッキ構築
        SetUpDeck();

        // 手札のリグ（固定）
        // P1 (Player): Eye_1, Mask_2 (Sum 3) -> Trend 10に対して無力
        // P2 (Dog): Gear_3 (Sum 3) -> Trend 10に対して無力だが、後で使う
        
        players[0].hand.Clear();
        players[0].hand.Add(GetCard("Eye_1"));
        players[0].hand.Add(GetCard("Mask_2"));
        
        players[1].hand.Clear();
        players[1].hand.Add(GetCard("Gear_3"));

        // フィールド初期化: Gear_10 (Trend 10)
        // StartGameを呼ぶとdeck[0]が使われてしまうので、手動でセットする
        CardData startCard = GetCard("Gear_10");
        // デッキに含まれているなら削除しておく（重複防止）
        deck.Remove(startCard); 
        
        PlayCardToField(startCard, tutorialMaster);
        initialSprite = startCard.rawSectorIcon;

        UIManager.Instance.UpdateAllHandVisuals();
    }
    public override void SetUpDeck()
    {
       deck.Clear();
       discardPile.Clear();
       
       // リグデッキ構築 (Stackなので下から順にAddするイメージだが、List[0]が上に来るように管理しているので注意)
       // GameManagerのDrawCardsは deck[0] を引く (RemoveAt(0))
       // よって、deck[0] が次に引くカード。
       
       // 必要なカードリスト
       string[] riggedDeckList = {
           // --- 以下、山札（上から順） ---
           "Gear_5",  // プレイヤーが最初に引くカード (Step 1)
           "Reject_10", // 予備
           "Audit_10", // 予備
           // ... その他適当なカード
       };
       
       List<CardData> riggedDeck = new List<CardData>();
       foreach(string name in riggedDeckList)
       {
           CardData c = GetCard(name);
           if(c != null) riggedDeck.Add(c);
           else Debug.LogError($"Card not found: {name}");
       }
       
       // 残りはランダムに埋める
       foreach(var card in allCardDatabase)
       {
           if(!riggedDeck.Contains(card)) riggedDeck.Add(card);
       }
       
       deck = riggedDeck;
       UIManager.Instance.UpdateDeckVisual(deck.Count);
       CardData firstCard = deck[0];
       initialSprite= firstCard.rawSectorIcon;
       deck.RemoveAt(0);
       PlayCardToField(firstCard, tutorialMaster);
       Debug.Log("ゲーム開始！最初のカード: " + firstCard.cardName);
    }

    // デッキから特定のカードを検索して取得するヘルパー
    private CardData GetCard(string cardName)
    {
        return allCardDatabase.Find(c => c.cardName == cardName);
    }

    // ---------------------------------------------------------
    // 会話システム
    // ---------------------------------------------------------
    private IEnumerator ShowDialogue(string text)
    {
        isWaitingForClick = true;
        speechText.text = "";
        
        typewriter.ShowText(speechText, text);
        
        // クリック待ち（タイピング中はスキップ、完了後は次へ）
        while(isWaitingForClick)
        {
            if(Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if(typewriter.IsTyping)
                {
                    typewriter.Skip();
                }
                else
                {
                    isWaitingForClick = false; // ループを抜ける
                }
            }
            yield return null;
        }
    }

    // ---------------------------------------------------------
    // ゲームロジックのオーバーライド
    // ---------------------------------------------------------

    // ---------------------------------------------------------
    // CPUロジックのオーバーライド（勝手に動かないようにする）
    // ---------------------------------------------------------
    protected override void ExecuteCPUTurn()
    {
        // チュートリアルでは何もしない
        // 明示的にCallCPUTurnActionから呼ぶか、ここでの処理を空にする
        Debug.Log("CPU Turn blocked by Tutorial Manager");
    }

    public void CallCPUTurnAction() // チュートリアル進行側から呼ぶための公開メソッド
    {
        StartCoroutine(TutorialCPUTurn());
    }

    private IEnumerator TutorialCPUTurn()
    {
        // CPUの演出ターン（DOGの手番）
        Player dog = players[1];
        yield return new WaitForSeconds(1.0f);
        
        // 決め打ちでカードを出す（Gear_3を想定）
        // 手札から探す
        CardData cardToPlay = dog.hand.Find(c => c.cardName == "Gear_3");
        if(cardToPlay == null) cardToPlay = dog.hand[0]; // 万が一のためのフォールバック

        yield return StartCoroutine(ShowDialogue("私の番だ。見ていろ。"));
        yield return StartCoroutine(ShowDialogue("「トレンド」に合わせる...これこそが優秀な市民の振る舞いだ。"));

        dog.hand.Remove(cardToPlay);
        PlayCardToField(cardToPlay, dog);

        // CPUターンが終わったらプレイヤーのターンへ
        // NextTurn()はPlayCardToField内で呼ばれるが、trendRideチェック等が入るので
        // ここは通常通り流して良いが、TutorialStepを進める必要がある
        tutorialStep = 4; // 勝利フェーズへ
    }

    // ---------------------------------------------------------
    // ゲームロジックのオーバーライド
    // ---------------------------------------------------------

    public override void PlayerDrawCard()
    {
        if (tutorialStep == 1) // ドローフェーズのみ許可
        {
            base.PlayerDrawCard(); 
            // 次のステップへ即座には行かず、コルーチン側で検知させるか、ここでフラグを立てる
            // ここではフラグ管理をTutorialSequence側で行うため、そのまま
        }
    }

    public override void TryPlayCard(CardData cardToPlay)
    {
        if (tutorialStep == 2) // プレイフェーズ
        {
             if (CanPlayCard(cardToPlay))
             {
                 base.TryPlayCard(cardToPlay);
             }
        }
        else if(tutorialStep == 4) // 勝利フェーズ
        {
            if (CanPlayCard(cardToPlay))
            {
                 // ここでセルフマッチになるはず
                 base.TryPlayCard(cardToPlay);
            }
        }
    }
}
