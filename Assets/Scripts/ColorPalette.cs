﻿using UnityEngine;

public enum Player
{
    PlayerNone = -1,
    Player1,
    Player2,
    Player3,
    Player4
}

public static class ColorPalette {
    
    public static Color CalcColor(int i, int maxPlayers = 4)
    {
        const float start = -60;
        return Utils.ToColor(start + (180.0f/(maxPlayers - 1))*i, 0.5f, 0.9f, 1);
    }

    public static Color GetColor(this Player player)
    {
        switch (player)
        {
            case Player.Player1:
                return CalcColor(0);
            case Player.Player2:
                return CalcColor(1);
            case Player.Player3:
                return CalcColor(2);
            case Player.Player4:
                return CalcColor(3);           
            default:
                return Color.magenta;
        }
    }

}
