using UnityEngine;

public class ColorInfo
{
    public PCColor Color { get; set; }
    public Color MatColor { get; set; }

    public ColorInfo(PCColor color, Color matColor)
    {
        Color = color;
        MatColor = matColor;
    }
}