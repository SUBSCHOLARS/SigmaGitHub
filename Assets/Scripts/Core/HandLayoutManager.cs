using UnityEngine;
using TMPro;

public class HandLayoutManager : MonoBehaviour
{
    [Header("レイアウト設定")]
    public float cardSpacing = 50f; // カードの間隔
    public float arcAmount = 200f; // 手札の弧の強さ
    public int maxCardsInRow = 8;
    public float cardSpacingInRow = 30f;

    // 手札のレイアウトを更新するメソッド
    public void UpdateLayout()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        // 手札全体の幅を計算
        float totalWidth = (maxCardsInRow - 1 ) * cardSpacing;
        float startX = -totalWidth / 2f;

        for(int i=0; i<childCount; i++)
        {
            Transform card = transform.GetChild(i);
            if(card.gameObject.GetComponent<TextMeshProUGUI>())
            {
                continue; // TextMeshProUGUIはレイアウトしない
            }

            // 1. 位置を決める
            float xPos = startX + i%maxCardsInRow * cardSpacing;
            float yPos = -i/maxCardsInRow * cardSpacingInRow - Mathf.Abs(xPos) / arcAmount; // x=0で一番高くなる放物線
            card.localPosition = new Vector3(xPos, yPos, 0);
        }
    }
}
