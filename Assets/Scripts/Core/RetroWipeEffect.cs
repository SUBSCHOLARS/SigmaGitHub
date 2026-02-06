using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class RetroWipeEffect : MonoBehaviour
{
    [Header("UI設定")]
    // 塗り潰し用の黒いImage
    [SerializeField] private Image maskImage;
    // 画面を何分割して描画するか
    [SerializeField] private int steps=20;
    [Header("タイミング設定")]
    // 完了までの目安時間
    [SerializeField] private float totalDuration=2.0f;
    // 読み込み速度にムラを作るか
    [SerializeField] private bool useRandomJitter=true;
    [Header("オーディオ設定")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip loadingSound;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 画面設定の強制
        if(maskImage!=null)
        {
            maskImage.type=Image.Type.Filled;
            maskImage.fillMethod=Image.FillMethod.Vertical;
            maskImage.fillOrigin=(int)Image.OriginVertical.Top;
            maskImage.fillAmount=1.0f;
        }

        // 処理開始
        StartCoroutine(AnimateWipe());
    }

    public virtual IEnumerator AnimateWipe()
    {
        // 音声再生開始
        if(audioSource!=null && loadingSound!=null)
        {
            audioSource.clip=loadingSound;
            audioSource.loop=true;
        }
        float currentStep=0;
        // 1ステップあたりの減算量（例: 20分割なら1/20=0.05ずつ減らす）
        float stepAmount=1.0f/steps;

        // 1ステップあたりの基本待機時間
        float baseWaitTime=totalDuration/steps;

        while(currentStep < steps)
        {
            // 1. 待機処理（レトロ感）
            // ランダムにすることで「重い処理が入った」ようなムラを出す
            float waitTime=baseWaitTime;
            if(useRandomJitter)
            {
                // 早く進んだり、少し詰まったりする演出
                waitTime=baseWaitTime*Random.Range(0.2f, 0.6f);
            }
            // 2. 描画更新（ガクッと減らす）
            currentStep++;
            yield return new WaitForSeconds(waitTime);
            // 指定時間待機（Updateを使わないため、ここが処理の切れ目になります）
            maskImage.fillAmount=1.0f-(currentStep*stepAmount);
            audioSource.Play();
        }
         // 3. 完了処理
        // fillAmountを完全にゼロとする
        maskImage.fillAmount=0.0f;

        if(audioSource!=null)
        {
            audioSource.Stop();
        }
        Deactivate();
    }
    public virtual void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
