using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

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
        yield return StartCoroutine(ShowDialogue("よし、登録は完了した。\n早速だが、この世界の「ルール」を教えよう。"));
        yield return StartCoroutine(ShowDialogue("準備をするから、少し待っていてくれ。"));

        // ここで実際のゲーム初期化処理（2人対戦）
        SetupTutorialGame();

        yield return StartCoroutine(ShowDialogue("画面下にあるのが君の「社会的価値」、つまり手札だ。\n真ん中にある数字が「トレンド」だ。"));
        yield return StartCoroutine(ShowDialogue("君の目的は、手札の合計値をトレンドに合わせることだ。\nこれを「マッチ」と呼ぶ。"));

        // 6. ドローの練習
        // 矢印などで強調表示するとベスト
        yield return StartCoroutine(ShowDialogue("まずは手札を増やす必要がある。\n右下の「DRAW」ボタンを押して、カードを引いてみてくれ。"));
        
        isPlayerInputLocked = false; // ロック解除
        UIManager.Instance.SetPlayerControlsActive(true);
        tutorialStep = 1; // ドロー待ち状態へ

        yield return new WaitUntil(() => tutorialStep == 2); // PlayerDrawCardで2になるのを待つ

        // 7. カードプレイの練習
        isPlayerInputLocked = true; // 一旦ロック
        yield return StartCoroutine(ShowDialogue("いいぞ。手札が増えたな。\n当然、合計値も変わったはずだ。"));
        yield return StartCoroutine(ShowDialogue("次はカードを出してみよう。\nトレンドと同じ「絵柄」か「数字」のカードなら出せるぞ。"));
        
        isPlayerInputLocked = false;
        yield return new WaitUntil(() => tutorialStep == 3); // TryPlayCardで3になるのを待つ
        
        // 8. 勝利
        isPlayerInputLocked = true;
        yield return StartCoroutine(ShowDialogue("見事だ。\nカードを出すと、そのカードの数字が新しいトレンドになる。"));
        yield return StartCoroutine(ShowDialogue("こうしてトレンドを操作し、自分の手札合計と一致させるんだ。\nこれを「セルフマッチ」と呼ぶ。"));
        yield return StartCoroutine(ShowDialogue("基本はこれだけだ。\nあとは実戦で覚えるといい。"));
        
        // ここから先は自由に遊ばせるか、ロビーに戻す
        yield return StartCoroutine(ShowDialogue("さあ、生き残るために思考し続けろ。\n...幸運を祈るよ。"));

        // ロビーに戻るボタンなどを表示
        UIManager.Instance.ShowGameEndAnimation(true, players[0]); // 仮の勝利演出
    }

    private void SetupTutorialGame()
    {
        players.Clear();
        string pName = PersistentDataManager.Instance.PlayerName;
        
        // 2人対戦（プレイヤー vs CPU）
        players.Add(new Player(PlayerID.Player, false, pName, 0));
        players.Add(new Player(PlayerID.CPU, true, "CPU_1", 0));
        
        currentRound = 1;
        UIManager.Instance.UpdateRoundText(currentRound);
        UIManager.Instance.UpdateScoreboard(players);

        // チュートリアル用固定デッキ
        // 実際にはCardDatabaseから検索して意図した順序でStackする処理が必要
        // 簡略化のため、通常Deck生成後に中身を入れ替える（あるいはID指定で生成）
        base.SetUpDeck(); 
        
        // とりあえず配る
        foreach(Player player in players)
        {
            DrawCards(player.hand, 4); // 少なめに配る
        }
        
        // 初期トレンド設定（例えば5にする）
        // StartGame()の代わりに手動で場に出す
        // CardData startCard = ...;
        // PlayCardToField(startCard, ...);
        base.StartGame(); // ランダムスタート（後で調整）
        
        UIManager.Instance.UpdateAllHandVisuals();
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
            if(Input.GetMouseButtonDown(0))
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

    public override void PlayerDrawCard()
    {
        if (tutorialStep == 1)
        {
            base.PlayerDrawCard(); // 実際に引く
            tutorialStep = 2; // 次へ
        }
    }

    public override void TryPlayCard(CardData cardToPlay)
    {
        if (tutorialStep == 2)
        {
             if (CanPlayCard(cardToPlay))
             {
                 base.TryPlayCard(cardToPlay);
                 tutorialStep = 3;
             }
        }
    }
}
