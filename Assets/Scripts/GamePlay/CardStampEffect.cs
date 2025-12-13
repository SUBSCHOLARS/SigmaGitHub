using UnityEngine;
using UnityEngine.UI;

public class CardStampEffect : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Image stampImage; // スタンプ画像
    [SerializeField] private Image approvedTextImage; // 「APPROVED」テキスト画像
    [Header("設定")]
    [SerializeField] private Color bribeStampColor=new Color(0.8f, 0.2f, 0.2f, 0.9f); // 赤いインク色
    public void StampBribe(Sprite inheritedIcon)
    {
        // 絵柄アイコンの表示
        if(stampImage !=null && inheritedIcon != null)
        {
            stampImage.gameObject.SetActive(true);
            stampImage.sprite = inheritedIcon;
            stampImage.color = bribeStampColor;
        }
        // 「APPROVED」テキストの表示
        if(approvedTextImage !=null)
        {
            approvedTextImage.gameObject.SetActive(true);
        }
    }
}
