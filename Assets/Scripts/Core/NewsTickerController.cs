using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class NewsTickerController : MonoBehaviour
{
    [Header("ファイル設定")]
    [Tooltip("StreamingAssetsフォルダ内のファイル名を指定")]
    [SerializeField] private string csvFileName="DogSpeakData.csv";

    [Header("更新設定")]
    [Tooltip("ニュースを更新する間隔（秒）")]
    [SerializeField] private float updateInterval=5.0f;
    [SerializeField] private bool updateOnStart=true;

    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI uiText;
    // リサイザーへの参照
    [SerializeField] private SpeechBubbleResizer bubbleResizer;

    private string filePath;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // StreamingAssetsのパスを生成
        filePath=Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (updateOnStart)
        {
            
        }
    }

    private IEnumerator NewsLoop()
    {
        while(true)
        {
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // CSVを読み込んでランダムな行を表示する
    public void ShowRandomNews()
    {
        if(!File.Exists(filePath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {filePath}");
            uiText.text="System Error: Data missing.";
            return;
        }
        // CSV読み込み（動的に読み込むため、毎回File.ReadAllLinesを行う）
        // 頻度が高い場合はキャッシュを検討すべきだが、数秒に1回ならば問題ないと判断
        List<string> lines=new List<string>();

        try
        {
            // 日本語エンコーディング対応のためUTF8を指定
            using(StreamReader sr=new StreamReader(filePath, Encoding.UTF8))
            {
                while(!sr.EndOfStream)
                {
                    lines.Add(sr.ReadLine());
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"読み込みエラー: {e.Message}");
            return;
        }
        if(lines.Count<=1) return; // ヘッダーのみ

        // ヘッダー（0行目）を除外してランダムに選ぶ
        int randomIndex=Random.Range(1, lines.Count);
        string targetLine=lines[randomIndex];

        // CSVパース（簡易版）
        // "Type,Content"の形式を想定し、最初のカンマで分割する
        string[] split=targetLine.Split(new char[] {','}, 2);

        if(split.Length>=2)
        {
            string content=split[1];

            // CSVの使用でダブルクォートで囲まれている場合の除去処理
            content=content.Trim('"');

            // テキスト更新
            uiText.text=content;
        }
    }
}
