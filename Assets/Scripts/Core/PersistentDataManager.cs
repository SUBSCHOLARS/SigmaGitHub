using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    public string PlayerName { get; private set; } = "Ian";
    public int GameProgressFlag { get; private set; } = 0;
    public bool TutorialFinished { get; private set; } = false;

    // エンディング到達フラグ
    public bool EndingBrainwash { get; private set; } = false;
    public bool EndingDisqualification { get; private set; } = false;
    public bool EndingDisqualificationBeforeIdeology { get; private set; } = false;
    public bool EndingRevolution { get; private set; } = false;

    private const string KEY_PROGRESS   = "SIGMA_GameProgressFlag";
    private const string KEY_NAME       = "SIGMA_PlayerName";
    private const string KEY_TUTORIAL   = "SIGMA_TutorialFinished";
    private const string KEY_END_BRAIN  = "SIGMA_Ending_Brainwash";
    private const string KEY_END_DISQ   = "SIGMA_Ending_Disqualification";
    private const string KEY_END_BEFORE = "SIGMA_Ending_DisqBeforeIdeology";
    private const string KEY_END_REV    = "SIGMA_Ending_Revolution";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadData();
    }

    // セッター — 呼び出し後に自動保存
    public void SetPlayerName(string name)               { PlayerName = name; SaveData(); }
    public void SetGameProgressFlag(int flag)             { GameProgressFlag = flag; SaveData(); }
    public void SetTutorialFinished(bool value)           { TutorialFinished = value; SaveData(); }
    public void SetEndingBrainwash()                      { EndingBrainwash = true; SaveData(); }
    public void SetEndingDisqualification()               { EndingDisqualification = true; SaveData(); }
    public void SetEndingDisqualificationBeforeIdeology() { EndingDisqualificationBeforeIdeology = true; SaveData(); }
    public void SetEndingRevolution()                     { EndingRevolution = true; SaveData(); }

    private void SaveData()
    {
        PlayerPrefs.SetInt(KEY_PROGRESS,   GameProgressFlag);
        PlayerPrefs.SetString(KEY_NAME,    PlayerName);
        PlayerPrefs.SetInt(KEY_TUTORIAL,   TutorialFinished ? 1 : 0);
        PlayerPrefs.SetInt(KEY_END_BRAIN,  EndingBrainwash ? 1 : 0);
        PlayerPrefs.SetInt(KEY_END_DISQ,   EndingDisqualification ? 1 : 0);
        PlayerPrefs.SetInt(KEY_END_BEFORE, EndingDisqualificationBeforeIdeology ? 1 : 0);
        PlayerPrefs.SetInt(KEY_END_REV,    EndingRevolution ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        GameProgressFlag = PlayerPrefs.GetInt(KEY_PROGRESS, 0);
        PlayerName       = PlayerPrefs.GetString(KEY_NAME, "Ian");
        TutorialFinished = PlayerPrefs.GetInt(KEY_TUTORIAL, 0) == 1;
        EndingBrainwash  = PlayerPrefs.GetInt(KEY_END_BRAIN,  0) == 1;
        EndingDisqualification               = PlayerPrefs.GetInt(KEY_END_DISQ,   0) == 1;
        EndingDisqualificationBeforeIdeology = PlayerPrefs.GetInt(KEY_END_BEFORE, 0) == 1;
        EndingRevolution = PlayerPrefs.GetInt(KEY_END_REV,   0) == 1;
    }
}
