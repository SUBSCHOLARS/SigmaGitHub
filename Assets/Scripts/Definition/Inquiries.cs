using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Inquiries", menuName = "Scriptable Objects/Inquiries")]
public class Inquiries : ScriptableObject
{
    [Header("基本情報")]
    public string title;
    public bool hasPhoto;
    public List<Button> answers;
    public List<Sprite> photos;
}
