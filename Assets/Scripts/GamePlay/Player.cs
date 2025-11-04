using System.Collections.Generic;

public class Player
{
    public PlayerID id;
    public List<CardData> hand = new List<CardData>();
    public bool isCPU;
    public Player(PlayerID id, bool isCPU)
    {
        this.id = id;
        this.isCPU = isCPU;
    }
}
