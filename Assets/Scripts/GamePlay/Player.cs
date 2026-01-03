using System.Collections.Generic;

public class Player
{
    public PlayerID id;
    public List<CardData> hand = new List<CardData>();
    public List<CardData> revealedCards = new List<CardData>(); // 公開されたカード
    public bool isCPU;
    public string playerName;
    public int totalPoints;
    public int wins;
    public Player(PlayerID id, bool isCPU, string playerName, int totalPoints)
    {
        this.id = id;
        this.isCPU = isCPU;
        this.playerName = playerName;
        this.totalPoints = totalPoints;
        this.wins = 0; // 初期化
    }
}
