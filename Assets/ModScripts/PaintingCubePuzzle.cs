using System;
using System.Collections.Generic;
using System.Linq;

public class PaintingCubePuzzle
{
    private ColorInfo[] _net;
    private PCColor _missingColor;

    public ColorInfo[] Grid;

    private bool isSolved;

    private static readonly int[][] cubeOrientationTable =
    {
        new[] { 1, 5, 2, 0, 4, 3 },
        new[] { 4, 1, 0, 3, 5, 2 },
        new[] { 3, 0, 2, 5, 4, 1 },
        new[] { 2, 1, 5, 3, 0, 4 }
    };


    public PaintingCubePuzzle(ColorInfo[] net, PCColor missingColor)
    {
        _net = net;
        _missingColor = missingColor;
    }

    private struct OrientedCube
    {
        public ColorInfo[] CurrentNet { get; private set; }
        public ColorInfo[] CurrentGrid { get; private set; }
        public int[] CurrentOrientation { get; private set; }
        public int CurrentPosition { get; private set; }

        public OrientedCube(ColorInfo[] currentNet, ColorInfo[] currentGrid, int[] currentOrientation, int currentPosition)
        {
            CurrentNet = currentNet;
            CurrentGrid = currentGrid;
            CurrentOrientation = currentOrientation;
            CurrentPosition = currentPosition;
        }
    }

    void GenerateOrder()
    {
        var newNet = new ColorInfo[6];

        int[] ixes;

        switch (_missingColor)
        {
            case PCColor.Red:
                ixes = new[] { 0, 1, 4, 3 };
                break;
            case PCColor.Orange:
                ixes = new[] { 0, 2, 1, 5 };
                break;
            case PCColor.Yellow:
                ixes = new[] { 2, 5, 1, 0 };
                break;
            case PCColor.Green:
                ixes = new[] { 5, 4, 1, 0 };
                break;
            case PCColor.Blue:
                ixes = new[] { 1, 0, 2, 5 };
                break;
            case PCColor.Indigo:
                ixes = new[] { 1, 2, 5, 1 };
                break;
            default:
                ixes = new[] { 1, 5, 4, 0 };
                break;
        }

        var pairings = Enumerable.Range(0, 4).SelectMany(x => Enumerable.Range(0, 4).Select(y => new[] { x, y }).ToArray()).ToArray();

        var pairs = pairings.Select(x => x.Select(y => _net[ixes[y]]).ToArray()).ToList();



    }

    void GeneratePuzzle()
    {


        var coordinateQueue = new Queue<int>(Enumerable.Range(0, 16).ToList().Shuffle());

        while (coordinateQueue.Count > 0)
        {
            var num = coordinateQueue.Dequeue();

            var solvedCube = new OrientedCube(_net.ToArray(), new ColorInfo[16], Enumerable.Range(0, 6).ToArray(), num);

            var adj = DirectionInfo.GetValidDirections(num).Where(x => x != null).ToArray();

            var candidates = new List<OrientedCube>();

            foreach (var cell in adj)
            {
                if (Valid(Grid[num], solvedCube.CurrentNet[solvedCube.CurrentOrientation[5]]))
                {
                    var cubeCandidate = solvedCube.CurrentNet.ToArray();
                    var gridCandidate = solvedCube.CurrentGrid.ToArray();

                    gridCandidate[cell.Position] = cubeCandidate[solvedCube.CurrentOrientation[5]];
                    cubeCandidate[solvedCube.CurrentOrientation[5]] = null;

                    candidates.Add(new OrientedCube(cubeCandidate, gridCandidate, cubeOrientationTable[(int)cell.Direction].Select(x => solvedCube.CurrentOrientation[x]).ToArray(), cell.Position));
                }
            }

            Recurse(candidates, new List<OrientedCube> { solvedCube });

            if (isSolved)
                break;
        }

    }

    private bool Valid(ColorInfo cell, ColorInfo cubeFace) => cell == null && cubeFace != null;

    void Recurse(List<OrientedCube> candidates, List<OrientedCube> current)
    {
        if (current.Any(x => x.CurrentNet.All(y => y == null)))
        {
            isSolved = true;
            return;
        }

        foreach (var candidate in candidates)
        {
            if (Valid(candidate.CurrentGrid[candidate.CurrentPosition], candidate.CurrentNet[candidate.CurrentOrientation[5]]))
            {
                current.Add(candidate);
            }
        }
    }
}
