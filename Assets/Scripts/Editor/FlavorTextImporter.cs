#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FlavorTextImporter : EditorWindow
{
    public TextAsset csvFile;
    // カードデータが入っているフォルダ（サブフォルダも検索する）
    private string targetFolder="Assets/CardData";
    [MenuItem("Tools/Import Flavor Text")]
    public static void ShowWindow()
    {
        GetWindow<FlavorTextImporter>("Flavor Import");
    }
    void OnGUI()
    {
        GUILayout.Label("CSVファイルからフレーバーテキストを一括インポート", EditorStyles.boldLabel);
        GUILayout.Space(10);

        csvFile=(TextAsset) EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        targetFolder=EditorGUILayout.TextField("Asset Folder Path", targetFolder);

        GUILayout.Space(10);

        if(GUILayout.Button("Import Flavor Text"))
        {
            if(csvFile!=null)
            {
                ImportData();
            }
            else
            {
                Debug.LogError("CSVファイルが指定されていません。");
            }
        }
    }
    void ImportData()
    {
        string[] lines=csvFile.text.Split('\n');
        int successCount=0;
        int notFoundCount=0;

        // フォルダ内の全てのCardDataをあらかじめ取得しておく（検索高速化のため）
        // GUIDを取得してパスに変換し、ロードする
        string[] guids=AssetDatabase.FindAssets("t:CardData", new string[]{targetFolder});

        // 1行目はヘッダーなのでスキップ(i=1から開始)
        for(int i=1; i<lines.Length; i++)
        {
            string line=lines[i].Trim();
            if(string.IsNullOrEmpty(line)) continue;

            // 間まで分割（FileName, FlavorText）
            // テキスト内にカンマが含まれる場合を考慮して、最初のカンマだけで分割
            string[] data=line.Split(new char[] {','}, 2);
            if(data.Length<2) continue;

            string targetFileName=data[0].Trim(); // 操作の対象とするカードの名前
            string flavor=data[1].Trim().Replace("\"",""); // 引用符があれば削除

            // 該当するアセットを探す
            CardData targetCard=null;

            foreach(string guid in guids)
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                string assetName=System.IO.Path.GetFileNameWithoutExtension(path);

                if(assetName==targetFileName)
                {
                    targetCard=AssetDatabase.LoadAssetAtPath<CardData>(path);
                    break;
                }
            }
            if(targetCard!=null)
            {
                Undo.RecordObject(targetCard, "Update Flavor Text");
                // ここでフレーバーテキストをセット
                targetCard.flavorText=flavor;
                EditorUtility.SetDirty(targetCard);
                successCount++;
            }
            else
            {
                Debug.LogError($"ファイルが見つかりません: {targetFileName}");
                notFoundCount++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"完了! 更新: {successCount}件、未発見: {notFoundCount}件");
    }
}
# endif