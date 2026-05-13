using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class AbstractCardController : MonoBehaviour
{
    public CardData myCardData;
    public Image cardImage;

    public Vector3 initialPosition; // 元の位置を記憶
    public int siblingIndex; // 本の重なり順を記憶
    public bool isHovered = false; // 現在ホバー中かどうかの判定
    public bool isPlayerOwned = false; // プレイヤー所有カードのみ true
    public CardData cardData => myCardData;

    public abstract void Setup(CardData data);
    public abstract void HandleClick();
    public abstract void SetSigmaSpeakMode(bool active);
    public abstract void SetHover(bool hover);
}
