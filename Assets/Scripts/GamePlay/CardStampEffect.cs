using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CardStampEffect : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Image stampImage; // スタンプ画像
    [SerializeField] private GameObject approvedTextObj; // 「APPROVED」テキスト画像
    public void ActivateStamp(Sprite targetCardSprite)
    {
        // 絵柄アイコンの表示
        if(stampImage !=null && targetCardSprite != null)
        {
            stampImage.gameObject.SetActive(true);
            stampImage.sprite = targetCardSprite;

            // 画像のアスペクト比を維持する
            stampImage.preserveAspect = true;
        }
        // 「APPROVED」テキストの表示
        if(approvedTextObj !=null)
        {
            approvedTextObj.SetActive(true);
        }
        // 演出
        // 一旦サイズを大きくして、縮小させながら揺らす
        transform.localScale= Vector3.one * 1.5f;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        // 角度を少しランダムにずらす
        float randomAngle=Random.Range(-15f, 15f);
        transform.localRotation=Quaternion.Euler(0, 0, randomAngle);
    }
    // スタンプを消す
    public void ResetStamp()
    {
        if(stampImage!=null)
        {
            stampImage.gameObject.SetActive(false);
        }
        if(approvedTextObj!=null)
        {
            approvedTextObj.SetActive(false);
        }
        // 回転とサイズを戻す
        transform.localRotation=Quaternion.identity;
        transform.localScale=Vector3.one;
    }
}
