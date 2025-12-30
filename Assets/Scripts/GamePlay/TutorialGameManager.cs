using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGameManager : GameManager
{
    private int tutorialStep = 0;

    // チュートリアル用の固定デッキなどを設定
    public override void SetUpDeck()
    {
        // ここで意図的な積み込みを行う
        // base.SetUpDeck(); // 通常のシャッフルはしない
        
        deck.Clear();
        discardPile.Clear();
        
        // 例: プレイヤーに勝たせるためのカード構成
        // 実装時はCardDatabaseから特定のカードを検索してAddする
        // 今回はダミーでランダムに追加するが、実際は固定する
        deck.AddRange(allCardDatabase); 
    }

    public override void StartGame()
    {
        base.StartGame();
        ShowDogMessage("やあ、調子はどうだ？私の名前は...まあ、好きに呼べ。\nまずは基本的なルールを教えよう。");
        ShowDogMessage("画面下にあるのが君の「社会的価値」、つまり手札だ。\n真ん中にある数字が「トレンド」だ。");
        ShowDogMessage("君の目的は、手札の合計値をトレンドに合わせることだ。");
        ShowDogMessage("さあ、まずはカードを一枚引いてみろ。「DRAW」ボタンを押すんだ。");
        tutorialStep = 1;
    }

    public override void PlayerDrawCard()
    {
        if (tutorialStep == 1)
        {
            base.PlayerDrawCard();
            ShowDogMessage("よし、いいぞ。カードを引くと手札が増える。\n当然、合計値も変わるわけだ。");
            ShowDogMessage("次は手札からカードを出してみよう。\nトレンドと同じ色か数字のカードなら出せるぞ。");
            tutorialStep = 2;
        }
        else
        {
             if(tutorialStep > 1)
             {
                 base.PlayerDrawCard(); // チュートリアル後半なら自由
             }
             else
             {
                 ShowDogMessage("今はカードを引く時じゃない。");
             }
        }
    }

    public override void TryPlayCard(CardData cardToPlay)
    {
        if (tutorialStep == 2)
        {
            if (CanPlayCard(cardToPlay))
            {
                base.TryPlayCard(cardToPlay);
                ShowDogMessage("見事だ。\nカードを出すと、そのカードの数字が新しいトレンドになる。");
                ShowDogMessage("こうしてトレンドを操作し、自分の手札合計と一致させるんだ。\nこれを「セルフマッチ」と呼ぶ。");
                tutorialStep = 3;
            }
            else
            {
                ShowDogMessage("そのカードは出せないぞ。ルールを思い出せ。\n同じ色か、同じ数字だ。");
            }
        }
        else
        {
            base.TryPlayCard(cardToPlay);
        }
    }
    
    protected override void ExecuteCPUTurn()
    {
        // チュートリアルではCPUは接待プレイをする、あるいは何もしない
        // ここではランダムに動作させるが、本来はスクリプト通りに動かす
        base.ExecuteCPUTurn();
    }

    private void ShowDogMessage(string message)
    {
        // 犬のアイコンがあればそれを渡すことでリッチになる
        UIManager.Instance.AddLogMessage($"<color=yellow>[犬]</color> {message}");
    }
}
