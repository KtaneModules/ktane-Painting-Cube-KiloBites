using UnityEngine;

public class ColorInfo
{
    public PCColor Color { get; private set; }
    public Color MatColor { get; private set; }

    public ColorInfo(PCColor color, Color matColor)
    {
        Color = color;
        MatColor = matColor;
    }
}