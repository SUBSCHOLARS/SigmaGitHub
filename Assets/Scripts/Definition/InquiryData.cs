using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InquiryData", menuName = "Scriptable Objects/InquiryData")]
public class InquiryData : ScriptableObject
{
    [Header("識別子（集計用）")]
    public string questionID;
    [Header("質問設定")]
    [TextArea(3, 10)]
    public string questionText; // 質問文
    public Sprite photo; //画像演出用
    [Header("選択肢")]
    public List<InquiryChoice> choices; // ボタンのリスト（可変）
}

[System.Serializable]
public struct InquiryChoice
{
    public string buttonLabel; // ボタンに表示する文字
    public IdeologyType ideologyType; // どの思想に紐づくか
    public float pressDuration;
    public InquiryData nextInquiry; // 次に表示する質問

}

public enum IdeologyType
{
    None,
    DoubleThink,
    MemoryHole,
    SigmaSpeak,
    BureauBrother,
    Thoughtcrime
}
