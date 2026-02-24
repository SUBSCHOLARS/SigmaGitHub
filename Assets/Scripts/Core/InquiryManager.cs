using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InquiryManager : MonoBehaviour
{
    [SerializeField] private InquiryResponseDatabase db;
    [SerializeField] private InquiryData currentData;
    [SerializeField] private InquiryData[] eachOfFirstInquiries;

    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI questionTextUI;
    [SerializeField] private Transform imageContainer;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    [Header("マスク用オブジェクト")]
    [SerializeField] private GameObject maskObject;
    private RetroWipeEffect wipeEffect;
    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource=GetComponent<AudioSource>();
        wipeEffect=maskObject.GetComponent<RetroWipeEffect>();
        switch (GameManager.Instance.GetProgressFlag())
        {
            case 0:
                DisplayInquiry(eachOfFirstInquiries[0]);
                break;
            case 1:
                DisplayInquiry(eachOfFirstInquiries[1]);
                break;
            case 2:
                DisplayInquiry(eachOfFirstInquiries[2]);
                break;
            case 3:
                DisplayInquiry(eachOfFirstInquiries[3]);
                break;
            case 4:
                DisplayInquiry(eachOfFirstInquiries[4]);
                break;
            case 5:
                DisplayInquiry(eachOfFirstInquiries[5]);
                break;
            default:
                Debug.LogError("不明なゲーム進行フラグ: " + GameManager.Instance.GetProgressFlag());
                break;
        }
    }

    public void DisplayInquiry(InquiryData data)
    {
        if(data==null)
        {
            FinishInquiry();
            return;
        }
        
        currentData=data;
        questionTextUI.text=data.questionText;

        // 古いイメージを削除
        foreach(Image child in imageContainer) Destroy(child.gameObject);

        // 古いボタンを削除
        foreach(Transform child in buttonContainer) Destroy(child.gameObject);

        // 新しいイメージを生成
        foreach(var sprite in data.photos)
        {
            // プレハブ（Rootオブジェクト）を生成
            GameObject imageObject=Instantiate(imagePrefab, imageContainer);
            Image image=imageObject.GetComponent<Image>();
            image.sprite=sprite;
        }
        // 新しいボタンを生成
        foreach(var choice in data.choices)
        {
            // プレハブ（Rootオブジェクト）を生成
            GameObject rootObject=Instantiate(buttonPrefab, buttonContainer);
            // 全ての子のTMPに同じテキストを流し込む（Button用とImage用）
            TextMeshProUGUI[] texts=rootObject.GetComponentsInChildren<TextMeshProUGUI>();
            foreach(var t in texts) t.text=choice.buttonLabel;

            // 子要素からHoldButtonスクリプトを探す
            HoldButton holdScript=rootObject.GetComponentInChildren<HoldButton>();

            if(holdScript!=null)
            {
                bool isLocked=db.confirmedIdeology!=IdeologyType.None && choice.ideologyType!=IdeologyType.None;

                if(isLocked)
                {
                    // ロック時はButtonコンポーネントを無効化
                    var btn = holdScript.GetComponent<Button>();
                    if(btn!=null) btn.interactable=false;
                }
                else
                {
                    // ホールド時間と完了時のアクションを注入
                    // durationが0なら即時実行、あればその時間ホールドさせる
                    float time=(choice.pressDuration>0 ) ? choice.pressDuration : 0.01f;
                    holdScript.Initalize(time, ()=>OnChoiceSelected(choice));
                }
            }
        }
    }
    private void OnChoiceSelected(InquiryChoice choice)
    {
        db.RecordResponse(currentData.questionID, choice);
        audioSource.PlayOneShot(audioSource.clip);
        // 次の質問へ
        if(wipeEffect!=null)
        {
            maskObject.SetActive(true);
            wipeEffect.Play();
        }
        DisplayInquiry(choice.nextInquiry);
    }
    private void FinishInquiry()
    {
        Debug.Log("質問シーケンス終了。次のモードへ移行します。");
        switch (GameManager.Instance.GetProgressFlag())
        {
            case 0:
                GameManager.Instance.SetProgressFlag(1);
                break;
            case 1:
                GameManager.Instance.SetProgressFlag(2);
                break;
            case 2:
                GameManager.Instance.SetProgressFlag(3);
                break;
            case 3:
                GameManager.Instance.SetProgressFlag(4);
                break;
            case 4:
                GameManager.Instance.SetProgressFlag(5);
                break;
            default:
                break;
        }
        // ここで対戦シーンやリザルトシーンへ遷移させる。
        SceneManager.LoadSceneAsync("Lobby");
    }
}
