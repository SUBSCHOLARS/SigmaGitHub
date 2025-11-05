using System.Collections.Generic;

public class Player
{
    public PlayerID id;
    public List<CardData> hand = new List<CardData>();
    public bool isCPU;
    public string playerName;
    public int totalPoints;
    public Player(PlayerID id, bool isCPU, string playerName)
    {
        this.id = id;
        this.isCPU = isCPU;
        this.playerName = playerName;
    }
}
