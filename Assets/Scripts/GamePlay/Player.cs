using System.Collections.Generic;

public class Player
{
    public PlayerID id;
    public List<CardData> hand = new List<CardData>();
    public bool isCPU;
    public string playerName;
    public Player(PlayerID id, bool isCPU, string name)
    {
        this.id = id;
        this.isCPU = isCPU;
        this.playerName = name;
    }
}
