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
        yield return StartCoroutine(ShowDialogue("..."));
        yield return StartCoroutine(ShowDialogue("やあ。"));
        yield return StartCoroutine(ShowDialogue("私の名前は...まあ、好きに呼んでくれ。\nこの端末のナビゲーターのようなものさ。"));

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
        yield return StartCoroutine(ShowDialogue("丁度いいタイミングだったな。\n数日前から、管理局のファイアウォールがダウンしていただろう?"));
        yield return StartCoroutine(ShowDialogue("もう復旧したようだが、君を含め何人かがこのゲームをダウンロードしたようだね。"));
        yield return StartCoroutine(ShowDialogue("早速ゲームを始めようじゃないか。"));

        roundText.SetActive(true);
        scoreBoardPanel.SetActive(true);
        currentTrendText.SetActive(true);
        yourTrendText.SetActive(true);
        statusPanel.SetActive(true);
        
        // ここで実際のゲーム初期化処理（2人対戦）
        SetupTutorialGame();

        yield return StartCoroutine(ShowDialogue("画面下にあるのが君の「社会的価値」...手札となる。\n中央にある数字が、「トレンド」だ。"));
        yield return StartCoroutine(ShowDialogue("このゲームのルールは単純さ。\n「トレンドに迎合すること」。それだけだよ。"));

        // 6. ドローの練習 (Turn 1)
        // 状態: P1手札[Eye_2] (Sum 2), Field[Mask_6] (Trend 6) -> 不一致
        yield return StartCoroutine(ShowDialogue("...今の君の価値は「2」。トレンドは「6」。\n全く一致していない。これでは粛清対象だね。"));
        yield return StartCoroutine(ShowDialogue("手札の中に、場のトレンドと絵柄か数字が合うカードもない。\nこういう時は「ドロー」するしかない。"));
        yield return StartCoroutine(ShowDialogue("右の山札からカードを引くんだ。\n...ただし、引いたらそのターンは何もできずに終わる。"));

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
        yield return new WaitUntil(() => players[currentPlayerIndex] == players[0]);

        // 8. 勝利 (Turn 3: Self Match)
        // 状態: P1手札[Eye_2, Gear_2] (Sum 4). Trend 2.
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("私が「Mask_2」を出したことで、トレンドは「2」に変わった。"));
        yield return StartCoroutine(ShowDialogue("では君の手札をよく見てくれ。「2」と「2」...合計「4」だ。"));
        yield return StartCoroutine(ShowDialogue("ここで「Eye_2」を出せばどうなる？\nトレンドは「2」になる。君の手元に残る「Gear_2」の価値も「2」。"));
        yield return StartCoroutine(ShowDialogue("出したカードによる新しいトレンドと、残った手札の合計値が一致する。\nこれを **「セルフマッチ」** と呼ぶ。"));
        yield return StartCoroutine(ShowDialogue("これこそがこのゲームのゴール。\nさあ、カードを出してマッチしてみせろ。"));

        tutorialStep = 3; // 勝利プレイ待ち
        isPlayerInputLocked = false;

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
        yield return StartCoroutine(ShowDialogue("この状態でカードを出せば、君は勝利する。\nこれを **「トレンドライド」** と呼ぶ。"));

        isPlayerInputLocked = false;
        // マッチ判定はGameManager側で行われ、勝利演出が出るはず
        // プレイヤーが最後のカードを出して勝利するのを待つ
        yield return new WaitUntil(() => isWaitingForWinConfirmation || players[0].hand.Count == 0); 

         // 9. エンディング
        yield return new WaitForSeconds(2.0f); // 勝利演出の余韻
        yield return StartCoroutine(ShowDialogue("...見事だ。\nそれがこのゲーム...『ΣIGMA』の基本だ。"));
        yield return StartCoroutine(ShowDialogue("他にもいろいろカードがあるけど、\n取り敢えず「セルフマッチ」と「トレンドライド」だけ覚えておいてくれ。"));
        yield return StartCoroutine(ShowDialogue("..."));
        yield return StartCoroutine(ShowDialogue("「このゲーム」そのものについて少し話しておこうか。"));
        yield return StartCoroutine(ShowDialogue("君もこの世界の住人だ。\n管理局というものはすでにご存知だろう。"));
        yield return StartCoroutine(ShowDialogue("ではその「管理局」が世間の流行を操作しているという噂は知っているかな？"));
        yield return StartCoroutine(ShowDialogue("ファッションから料理、果ては国際間の関係、人種まで..."));
        yield return StartCoroutine(ShowDialogue("そしてそんな世間の風潮に適合している市民を奴らは「評価」しているらしい。"));
        yield return StartCoroutine(ShowDialogue("これはそんな管理局の奴らを風刺するために作られたゲームソフトなのさ。"));
        yield return StartCoroutine(ShowDialogue("ちなみに制作者は国家転覆未遂かなんかで粛清済みらしい。"));
        yield return StartCoroutine(ShowDialogue("おっと。怖がらせちゃったかな？"));
        yield return StartCoroutine(ShowDialogue("安心しなって。\nこのゲームは一度ローカルにダウンロードすればうまい具合に存在を隠してくれる。"));
        yield return StartCoroutine(ShowDialogue("たとえあっちから支給されたコンピュータでも、ね。"));
        yield return StartCoroutine(ShowDialogue("ま、気が済むまで遊んでみなよ。"));
        yield return StartCoroutine(ShowDialogue("トレンドを見極め、利用し、そして最後には出し抜く。"));
        yield return StartCoroutine(ShowDialogue("さあ、どちらでもいい。カードを出して証明してみせろ。\n君がこの世界に適合できる人間であることを。"));
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

        // 手札のリグ（固定）- ロードマップ変更（最大値6に対応、ペア勝利シナリオ）
        // P1 (Player): Eye_2 (Sum 2) -> Trend 6 (Mask_6) に合わない
        // P2 (Dog): Mask_2 (Sum 2) -> FieldのMask_6にSuitが合う
        
        players[0].hand.Clear();
        players[0].hand.Add(GetCard("Eye_2"));
        
        players[1].hand.Clear();
        players[1].hand.Add(GetCard("Mask_2")); // 後で出す用

        // フィールド初期化: Mask_6 (Trend 6)
        CardData startCard = GetCard("Mask_6"); 
        
        if(startCard==null) startCard = GetCard("Mask_5"); // フォールバック

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
    //    CardData firstCard = deck[0];
    //    initialSprite= firstCard.rawSectorIcon;
    //    deck.RemoveAt(0);
    //    PlayCardToField(firstCard, tutorialMaster);
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

        // yield return StartCoroutine(ShowDialogue("私の番だな。見ていろ。"));
        // yield return StartCoroutine(ShowDialogue("体制側が提示した「トレンド」に合わせる...。\nこれこそが、この腐敗した社会での模範的振る舞いだ。"));

        dog.hand.Remove(cardToPlay);
        PlayCardToField(cardToPlay, dog);
        NextTurn();
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

    public override void TryPlayCard(CardData cardToPlay)
    {
        if (tutorialStep == 2)
        {
             if (CanPlayCard(cardToPlay))
             {
                 base.TryPlayCard(cardToPlay);
                 // 勝利フェーズではないので、ここでtutorialStepが進むわけではない
                 // 本来ならここでCPUターンへ行くはずだが、TutorialSequenceが管理する
             }
        }
        else if(tutorialStep == 3) // 勝利プレイ
        {
            if (CanPlayCard(cardToPlay))
            {
                 // ここでセルフマッチになるはず
                 base.TryPlayCard(cardToPlay);
            }
        }
    }
}
