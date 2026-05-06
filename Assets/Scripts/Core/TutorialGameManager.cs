using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// GameManagerを継承
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
    [SerializeField] private AudioClip nextButtonSound;
    [Header("ゲーム内UI")]
    [SerializeField] private GameObject roundText;
    [SerializeField] private GameObject scoreBoardPanel;
    [SerializeField] private GameObject currentTrendText;
    [SerializeField] private GameObject yourTrendText;
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private GameObject winButton;
    [SerializeField] private GameObject cardExplanation;
    [SerializeField] private GameObject fieldPileVisual;
    [SerializeField] private GameObject deckVisualContainer;
    [SerializeField] private GameObject playerHandArea;
    public static bool isTutorialFinish=false;
    private InputActionProperty pressAction;

    private Player tutorialMaster=new Player(PlayerID.GameMaster, false, "TutorialMaster", 0, IdeologyType.None);

    public override void InitializeGame()
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

        yield return new WaitForSeconds(2.5f);

        // 2. 犬が気づく
        // TODO: ここでちょっとしたアニメーションやSEを入れると良い
        dogSide.SetActive(false);
        SoundManager.Instance.PlaySound(dogNotice);
        dogFront.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        // 3. 自己紹介
        speechBubble.SetActive(true);
        yield return StartCoroutine(ShowDialogue("..."));
        yield return StartCoroutine(ShowDialogue("やあ。\n『ΣIGMA』にようこそ。"));
        yield return StartCoroutine(ShowDialogue("私の名前は...まあ、好きに呼んでくれ。\nこのソフトのナビゲーターのようなものさ。"));

        // 4. 名前入力
        yield return StartCoroutine(ShowDialogue("君の名前を教えてくれないか？\nデータベースに登録する必要があるんだ。"));
        
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
        yield return StartCoroutine(ShowDialogue("おっと。"));
        yield return StartCoroutine(ShowDialogue("もちろんこの名前は悪用なんてしないさ。\nただゲームのユーザー名にするだけだよ。"));

        // 5. ゲーム開始準備
        // ロードマップに従い、世界観（皮肉）と「ダウンロード」設定を反映
        yield return StartCoroutine(ShowDialogue("...\n早速ゲームを始めようじゃないか。"));

        IsGameUIShown(true);
        
        // ここで実際のゲーム初期化処理（2人対戦）
        SetupTutorialGame();

        yield return StartCoroutine(ShowDialogue("画面下にあるのが君の「社会的価値」...手札となる。\n中央にある数字が、「トレンド」だ。"));
        yield return StartCoroutine(ShowDialogue("トランプみたいにスートが4つあってそれぞれ\n「目」,「仮面」,「鎖」,「歯車」\nになっている。"));
        yield return StartCoroutine(ShowDialogue("本当は7枚ずつ、3人でやるんだけど、\nあくまでお試しだから簡単にやるよ。"));
        yield return StartCoroutine(ShowDialogue("このゲームのルールは単純さ。"));
        yield return StartCoroutine(ShowDialogue("「トレンドに迎合すること」。それだけだよ。"));

        // 6. ドローの練習 (Turn 1)
        // 状態: P1手札[Eye_2] (Sum 2), Field[Mask_6] (Trend 6) -> 不一致
        yield return StartCoroutine(ShowDialogue("...今の君の価値は「2」。トレンドは「6」。\n全く一致していない。これではダメだね。"));
        yield return StartCoroutine(ShowDialogue("手札の中に、場のトレンドと同じ数字か、同じスートを持つカードもない。\nこういう時は「ドロー」するしかない。"));
        yield return StartCoroutine(ShowDialogue("左上の山札からカードを引くんだ。\n...ただし、引いたらそのターンは何もできずに終わる。"));

        isPlayerInputLocked = false; // ロック解除
        UIManager.Instance.SetPlayerControlsActive(true);
        tutorialStep = 1; // ドロー待ち

        // 手番がCPUに移るのを待つ（プレイヤーがドローするとNextTurn経由でCPUターンになる）
        yield return new WaitUntil(() => players[currentPlayerIndex].isCPU); 

        // 7. CPUプレイ (Turn 2)
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("カードを引いてターン終了...。\n次は私の番だね。"));
        
        // CPUアクション実行
        CallCPUTurnAction();
        
        // CPUがMask_2を出して、トレンドが2になるのを待つ。
        // CPUのカードプレイ後、NextTurnで再びプレイヤー(index 0)のターンになるはず。
        // yield return new WaitUntil(() => players[currentPlayerIndex] == players[0]);

        // 8. 勝利 (Turn 3: Self Match)
        // 状態: P1手札[Eye_2, Gear_2] (Sum 4). Trend 2.
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("私が「Mask_2」を出したことで、トレンドは「2」に変わった。"));
        yield return StartCoroutine(ShowDialogue("では君の手札をよく見てくれ。「2」と「2」...合計「4」だ。"));
        yield return StartCoroutine(ShowDialogue("ここで例えば「Eye_2」を出せばどうなる?\nトレンドは「2」になる。君の手元に残る「Gear_2」の価値も「2」。"));
        yield return StartCoroutine(ShowDialogue("自分が出したカードによる新しいトレンドと、残った手札の合計値が一致する。\nこれを 「セルフマッチ」 と呼ぶ。"));
        yield return StartCoroutine(ShowDialogue("セルフマッチをするなら、手札の合計値を偶数にしておくといいだろうね。"));
        yield return StartCoroutine(ShowDialogue("これこそがこのゲームのゴール。\nさあ、カードを出してマッチしてみせろ。"));

        tutorialStep = 3; // 勝利プレイ待ち
        isPlayerInputLocked = false;
        
        yield return new WaitUntil(() => tutorialStep == 4); 
        SetupTutorialGameAgain();

        yield return StartCoroutine(ShowDialogue("さて、華々しく勝利してお祝い..といきたいところだけど。"));
        yield return StartCoroutine(ShowDialogue("もう一つ説明することがあるんだ。"));
        yield return StartCoroutine(ShowDialogue("さっきの説明を聞いていて思ったかもしれないけど。"));
        yield return StartCoroutine(ShowDialogue("他人が上書きしたトレンドの値が\n偶然自分の手札の合計値と同じになる。"));
        yield return StartCoroutine(ShowDialogue("なんて状況も考えられるよね。"));
        yield return StartCoroutine(ShowDialogue("勿論これも立派な「勝利」になる。\n「トレンドライド」って言うんだ。"));
        yield return StartCoroutine(ShowDialogue("しかもトレンドライドはセルフマッチよりも優先される。"));
        yield return StartCoroutine(ShowDialogue("たまには狙ってみても\nいいかもね。"));
        yield return StartCoroutine(ShowDialogue("じゃあMask_3が出せそうだから出してみようか。"));

        UIManager.Instance.SetPlayerControlsActive(true);
        isPlayerInputLocked = false;
        yield return new WaitUntil(() => players[currentPlayerIndex].isCPU);
        
        isPlayerInputLocked = true;
        // yield return StartCoroutine(TutorialCPUTurn());
        CallCPUTurnAction();

        yield return StartCoroutine(ShowDialogue("ちょうどこんな感じさ。"));

        isPlayerInputLocked = false;
        // マッチ判定はGameManager側で行われ、勝利演出が出るはず
        // プレイヤーが最後のカードを出して勝利するのを待つ
        yield return new WaitUntil(() => tutorialStep==5); 

         // 9. エンディング
        yield return new WaitForSeconds(2.0f); // 勝利演出の余韻
        yield return StartCoroutine(ShowDialogue("どうだったかな？。\nこれがこのゲーム...「ΣIGMA」の基本になる。"));
        yield return StartCoroutine(ShowDialogue("これからはレベルごとに提示されるクリアの条件を満たしていって、ゴールを目指してくれ。"));

        IsGameUIShown(false);
        fieldPileVisual.SetActive(false);
        deckVisualContainer.SetActive(false);
        playerHandArea.SetActive(false);
        SoundManager.Instance.PlaySound(nextButtonSound);
        cardExplanation.SetActive(true);

        yield return StartCoroutine(ShowDialogue("カードは全部で48枚で7種類ある。"));

        cardExplanation.SetActive(false);
        
        yield return StartCoroutine(ShowDialogue("まあこんなところかな？\n他にもいろいろなカードがあるけど、\n取り敢えず「セルフマッチ」と「トレンドライド」だけ覚えておいてくれ。"));
        yield return StartCoroutine(ShowDialogue("あとはやっていくうちに覚えられるさ。"));
        yield return new WaitForSeconds(1f);

        // ロビーに戻るボタンなどを表示
        // UIManager.Instance.ShowGameEndAnimation(true, players[0]); // 仮の勝利演出
        SoundManager.Instance.PlaySound(nextButtonSound);
        isTutorialFinish=true;
        if (PersistentDataManager.Instance != null)
            PersistentDataManager.Instance.SetTutorialFinished(true);
        SceneManager.LoadSceneAsync("Lobby");
    }
    private void IsGameUIShown(bool shown)
    {
        roundText.SetActive(shown);
        scoreBoardPanel.SetActive(shown);
        currentTrendText.SetActive(shown);
        yourTrendText.SetActive(shown);
        statusPanel.SetActive(shown);
    }

    private void SetupTutorialGame()
    {
        players.Clear();
        string pName = PersistentDataManager.Instance.PlayerName;
        
        // 2人対戦（プレイヤー vs CPU）
        players.Add(new Player(PlayerID.Player, false, pName, 0, IdeologyType.None));
        players.Add(new Player(PlayerID.CPU, true, "DOG", 0, IdeologyType.None));
        
        currentRound = 1;
        UIManager.Instance.UpdateRoundText(currentRound);
        UIManager.Instance.UpdateScoreboard(players);

        // デッキ構築
        SetUpDeck();

        // 手札のリグ（固定）- ロードマップ変更（最大値6に対応、ペア勝利シナリオ）
        // P1 (Player): Eye_2 (Sum 2) -> Trend 6 (Mask_6) に合わない
        // P2 (Dog): Mask_2 (Sum 2) -> FieldのMask_6にSuitが合う
        
        players[0].hand.Clear();
        players[0].hand.Add(GetCard("Eye_2"));
        
        players[1].hand.Clear();
        players[1].hand.Add(GetCard("Mask_2")); // 後で出す用

        // フィールド初期化: Mask_6 (Trend 6)
        CardData startCard = GetCard("Mask_6");

        deck.Remove(startCard); 
        PlayCardToField(startCard, tutorialMaster);
        UIManager.Instance.UpdateAllHandVisuals(); // ここで自動的にYourTrendも更新される
        initialSprite = startCard.rawSectorIcon;
        Debug.Log("ゲーム開始！最初のカード: " + startCard.cardName);

        UIManager.Instance.UpdateAllHandVisuals();
    }
    private void SetupTutorialGameAgain()
    {
        players.Clear();
        string pName = PersistentDataManager.Instance.PlayerName;
        
        // 2人対戦（プレイヤー vs CPU）
        players.Add(new Player(PlayerID.Player, false, pName, 0, IdeologyType.None));
        players.Add(new Player(PlayerID.CPU, true, "DOG", 0, IdeologyType.None));
        
        // 状態リセット
        currentPlayerIndex = 0; // プレイヤーから開始
        tutorialStep = 4; // 明示的にセット
        
        currentRound = 1;
        UIManager.Instance.UpdateRoundText(currentRound);
        UIManager.Instance.UpdateScoreboard(players);

        // デッキ構築
        SetUpDeck();

        // 手札のリグ（固定）- ロードマップ変更（最大値6に対応、ペア勝利シナリオ）
        // P1 (Player): Eye_2 (Sum 2) -> Trend 6 (Mask_6) に合わない
        // P2 (Dog): Mask_2 (Sum 2) -> FieldのMask_6にSuitが合う
        
        players[0].hand.Clear();
        players[0].hand.Add(GetCard("Eye_2"));
        players[0].hand.Add(GetCard("Mask_3"));
        
        players[1].hand.Clear();
        players[1].hand.Add(GetCard("Mask_2")); // 後で出す用

        // フィールド初期化: Mask_6 (Trend 6)
        CardData startCard = GetCard("Mask_6");

        deck.Remove(startCard); 
        PlayCardToField(startCard, tutorialMaster);
        initialSprite = startCard.rawSectorIcon;
        Debug.Log("ゲーム開始！最初のカード: " + startCard.cardName);

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
           "Gear_2",  // プレイヤーが最初に引くカード (Step 1)
           "Chain_Reject", // 予備
           "Mask_Audit", // 予備
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
            if((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) 
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                || (Keyboard.current != null && Keyboard.current.numpadEnterKey.wasPressedThisFrame))
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
    // CPUロジックのオーバーライド（勝手に動かないようにする）
    // ---------------------------------------------------------
    protected override void ExecuteCPUTurn()
    {
        // チュートリアルでは何もしない（GameManagerからの自動呼び出しを無視）
        // CallCPUTurnAction() によって手動で動かす
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
        
        // 決め打ちでカードを出す（Mask_2を想定）
        // 手札から探す
        CardData cardToPlay = dog.hand.Find(c => c.cardName == "Mask_2");
        if(cardToPlay == null) cardToPlay = dog.hand[0]; 

        dog.hand.Remove(cardToPlay);
        PlayCardToField(cardToPlay, dog);
        UIManager.Instance.UpdateAllHandVisuals(); // ここで自動的にYourTrendも更新される
        List<Player> trendRideWinners=CheckForTrendRide(dog);
        if(trendRideWinners.Count>0)
        {
            UIManager.Instance.ShowTrendRideAlert(true, trendRideWinners, dog);
            yield return StartCoroutine(WaitForContinueCLick());
            UIManager.Instance.ShowTrendRideAlert(false, null, null);
            tutorialStep=5;
            yield break;
        }
        else
        {
            NextTurn();
        }
        // PlayCardToField内でNextTurnが呼ばれ、再度ExecuteCPUTurnが呼ばれるが、
        // Overrideしているので何も起きず、制御はここ（TutorialSequenceのWaitUntil）に戻る。
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
    public override void PlayerConfirmWin()
    {
        tutorialStep=4;
        Debug.Log(tutorialStep);
        winButton.SetActive(false);
    }

    public override IEnumerator TryPlayCard(CardData cardToPlay)
    {
        if (tutorialStep == 2)
        {
             if (CanPlayCard(cardToPlay))
             {
                 yield return base.TryPlayCard(cardToPlay);
                 // 勝利フェーズではないので、ここでtutorialStepが進むわけではない
                 // 本来ならここでCPUターンへ行くはずだが、TutorialSequenceが管理する
             }
        }
        else if(tutorialStep == 3) // 勝利プレイ
        {
            if (CanPlayCard(cardToPlay))
            {
                 // ここでセルフマッチになるはず
                 yield return base.TryPlayCard(cardToPlay);
            }
        }
        else if(tutorialStep==4) // トレンドライドプレイ
        {
            if(CanPlayCard(cardToPlay))
            {
                yield return base.TryPlayCard(cardToPlay);
            }
            else
            {
                Debug.Log($"プレイ不可: {cardToPlay.cardName}. Step: {tutorialStep}");
            }
        }
        else
        {
            Debug.Log($"TryPlayCard Ignored. Step: {tutorialStep}");
        }
        yield return null;
    }
}