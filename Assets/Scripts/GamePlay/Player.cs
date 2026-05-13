using System.Collections.Generic;

public class Player
{
    public PlayerID id;
    public List<CardData> hand = new List<CardData>();
    public HashSet<CardData> revealedCards = new HashSet<CardData>(); // 公開されたカード
    public HashSet<CardData> interrogatedCards = new HashSet<CardData>(); // 尋問されたカード
    public bool isCPU;
    public string playerName;
    public int totalPoints;
    public int wins;
    public IdeologyType ideologyType;
    public Player(PlayerID id, bool isCPU, string playerName, int totalPoints, IdeologyType ideologyType)
    {
        this.id = id;
        this.isCPU = isCPU;
        this.playerName = playerName;
        this.totalPoints = totalPoints;
        this.ideologyType = ideologyType;
        this.wins = 0; // 初期化
    }
}
