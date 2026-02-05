using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("設定")]
    [Tooltip("満タンになるまでの秒数")]
    public float requiredHoldTime=3.0f;
    [Tooltip("ホールドをやめた時にゲージが減る速度（倍率）")]
    public float decaySpeed=5.0f;
    [Tooltip("完了時に一度だけ呼ばれるイベント")]
    public UnityEvent OnComplete;
    [Header("UI参照")]
    public RectTransform fillMaskRect;
    public RectTransform baseRect;
    [Header("オーディオ設定")]
    [Tooltip("開始時のピッチ")]
    public float minPitch=0.8f;
    [Tooltip("完了時のピッチ")]
    public float maxPitch=1.5f;
    private bool isHolding=false;
    private float currentProgress=0f;
    private bool isCompleted=false;
    private float fillMaskWidth;
    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateProgresUI();
        audioSource=GetComponent<AudioSource>();
        if(baseRect!=null)
        {
            fillMaskWidth=baseRect.sizeDelta.x;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(isCompleted) return;

        // ホールド中の処理
        if(isHolding)
        {
            // 時間経過で進行度を加算
            currentProgress+=Time.deltaTime*(fillMaskWidth/requiredHoldTime);
            if(audioSource!=null && audioSource.isPlaying)
            {
                // Lerpを使って、現在の進捗（0~1）に応じたピッチを計算
                audioSource.pitch=Mathf.Lerp(minPitch, maxPitch, currentProgress/fillMaskWidth);
            }
            // 完了判定
            if(currentProgress>=fillMaskWidth)
            {
                currentProgress=fillMaskWidth;
                isCompleted=true;
                StopAudio();
                OnComplete?.Invoke(); // イベント発火
            }
        }
        else
        {
            //　離している瞬間は急速に戻る
            if(currentProgress>0f)
            {
                currentProgress-=Time.deltaTime*decaySpeed*(fillMaskRect.sizeDelta.x/requiredHoldTime);
                if(currentProgress<0f)
                {
                     currentProgress=0f;
                     if(audioSource!=null) audioSource.pitch=minPitch;
                }
            }
        }
        UpdateProgresUI();
    }
    // マスクの幅を制御してプログレスバー表現を行う
    private void UpdateProgresUI()
    {
        if(fillMaskRect!=null)
        {
            // Widthを操作して左から右に広げる
            float width=currentProgress;
            fillMaskRect.sizeDelta=new Vector2(width, fillMaskRect.sizeDelta.y);
        }
    }
    // 押した瞬間
    public void OnPointerDown(PointerEventData eventData)
    {
        if(!isCompleted)
        {
            isHolding=true;
            PlayAudio();
        }
    }
    // 離した瞬間
    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding=false;
        StopAudio();
    }
    // 音声再生のヘルパー関数
    private void PlayAudio()
    {
        if(audioSource!=null && !audioSource.isPlaying)
        {
            audioSource.pitch=Mathf.Lerp(minPitch, maxPitch, currentProgress/fillMaskWidth);
            audioSource.Play();
        }
    }
    // 音声停止のヘルパー関数
    private void StopAudio()
    {
        if(audioSource!=null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
