using System;
using System.Collections.Generic;
using System.Linq;

public class DirectionInfo
{
    public CubeDirection Direction { get; private set; }
    public int Position { get; private set; }

    public DirectionInfo(CubeDirection direction, int position)
    {
        Direction = direction;
        Position = position;
    }

    public override string ToString() => $"{"ABCD"[Position % 4]}{(Position / 4) + 1}";

    public static DirectionInfo[] GetValidDirections(int cubePos)
    {

        var row = cubePos / 4;
        var col = cubePos % 4;

        var adj = new[]
        {
            row - 1,
            col + 1,
            row + 1,
            col - 1
        };

        return adj.Select((x, i) => x < 0 || x > 3 ? null : new DirectionInfo((CubeDirection)i, ((i % 2 != 0 ? row : x) * 4) + (i % 2 == 0 ? col : x))).ToArray();
    }
}