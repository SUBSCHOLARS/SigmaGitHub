using UnityEngine;

[CreateAssetMenu(fileName = "KeyBoardButton", menuName = "Scriptable Objects/KeyBoardButton")]
public class KeyBoardButton : ScriptableObject
{
    [Header("基本情報")]
    public string keyboardLetter;
    public Sprite keyboardSprite;
    public Color letterColor=Color.white;
}
