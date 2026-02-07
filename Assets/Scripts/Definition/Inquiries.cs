using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Inquiries", menuName = "Scriptable Objects/Inquiries")]
public class Inquiries : ScriptableObject
{
    [Header("基本情報")]
    [TextArea(3, 10)]
    public string title;
    public bool hasPhoto;
    public GameObject answer;
    public List<Sprite> photos;
}
