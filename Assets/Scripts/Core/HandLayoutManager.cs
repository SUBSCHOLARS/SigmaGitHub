using UnityEngine;

public class HandLayoutManager : MonoBehaviour
{
    [Header("レイアウト設定")]
    public float cardSpacing = 50f; // カードの間隔
    public float arcAmount = 200f; // 手札の弧の強さ
    public float rotationAmount = 5f; // カードの傾き

    // 手札のレイアウトを更新するメソッド
    public void UpdateLayout()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        // 手札全体の幅を計算
        float totalWidth = (childCount - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for(int i=0; i<childCount; i++)
        {
            Transform card = transform.GetChild(i);

            // 1. 位置を決める
            float xPos = startX + i * cardSpacing;
            float yPos = -Mathf.Abs(xPos) / arcAmount; // x=0で一番高くなる放物線
            card.localPosition = new Vector3(xPos, yPos, 0);

            // 2. 角度を決める
            float angle = -xPos / (totalWidth + 1f) * (rotationAmount * childCount);
            card.localRotation=Quaternion.Euler(0,0,angle);
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
