using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Sigma Game/CardData")]
public class CardData : ScriptableObject
{
    [Header("基本情報")]
    public string cardName; // カード名
    public Sprite cardSprite; // 画像
    public Sprite cardIcon; // ログ表示用の小さなアイコン
    [Header("ルール情報")]
    public CardSector sector; // スート（アイ、チェーン、ギア、マスク）
    public CardEffect effect; // 特殊効果（None, Suspend, Reject, Audit, Bribe, Censor, Interrogate）
    [Header("数値")]
    public int numberValue; // 場に出た時の「トレンド」数値（0, 1-5, 10, 12）
    public int handValue; // 手札にある時の「合計値」（0, 1-5, 10, 12, 15）
}
