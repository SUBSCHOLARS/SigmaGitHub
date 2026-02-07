using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InquiryResponseDatabase", menuName = "Scriptable Objects/InquiryResponseDatabase")]
public class InquiryResponseDatabase : ScriptableObject
{
    [System.Serializable]
    public struct ResponseEntry
    {
        public string qID;
        public string selectedLabel;
        public IdeologyType ideologyType;
    }
    [Header("回答履歴")]
    public List<ResponseEntry> responses=new List<ResponseEntry>();
    [Header("現在のステータス")]
    public IdeologyType confirmedIdeology=IdeologyType.None;

    public void Clear()
    {
        responses.Clear();
        confirmedIdeology=IdeologyType.None;
    }

    public void RecordResponse(string id, InquiryChoice choice)
    {
        responses.Add(new ResponseEntry {qID=id, selectedLabel=choice.buttonLabel, ideologyType=choice.ideologyType});

        if (confirmedIdeology==IdeologyType.None && choice.ideologyType != IdeologyType.None)
        {
            confirmedIdeology=choice.ideologyType;
            Debug.Log($"思想が確定しました: {confirmedIdeology}");
        }
    }
}
