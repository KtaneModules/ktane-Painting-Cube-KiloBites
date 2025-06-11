using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Random;
using static UnityEngine.Debug;

public class PaintingCubePuzzle
{
    private ColorInfo[] _net;
    private PCColor _missingColor;

    private ColorInfo[] _colorReferences;

    private bool isSolved, failed;

    private int retries = 0;

    private ColorInfo[] tempCube = new ColorInfo[6];

    public bool CheckVertex(ColorInfo[] vertex) => Enumerable.Range(0, 3).Select(x => tempCube[x]).SequenceEqual(vertex);

    private static readonly int[][] cubeOrientationTable =
    {
        new[] { 1, 5, 2, 0, 4, 3 },
        new[] { 4, 1, 0, 3, 5, 2 },
        new[] { 3, 0, 2, 5, 4, 1 },
        new[] { 2, 1, 5, 3, 0, 4 }
    };


    public PaintingCubePuzzle(ColorInfo[] net, PCColor missingColor, ColorInfo[] colorReferences)
    {
        _net = net;
        _missingColor = missingColor;
        _colorReferences = colorReferences;


        GenerateCube();
    }

    private struct OrientedCube
    {
        public ColorInfo[] CurrentCube { get; private set; }
        public ColorInfo[] CurrentGrid { get; private set; }
        public int[] CurrentOrientation { get; private set; }
        public int CurrentPosition { get; private set; }

        public OrientedCube(ColorInfo[] currentCube, ColorInfo[] currentGrid, int[] currentOrientation, int currentPosition)
        {
            CurrentCube = currentCube;
            CurrentGrid = currentGrid;
            CurrentOrientation = currentOrientation;
            CurrentPosition = currentPosition;
        }
    }

    void GenerateCube()
    {
        var cubeNetOpposite = new Dictionary<int, int>
        {
            { 0, 5 },
            { 1, 3 },
            { 2, 4 },
            { 3, 1 },
            { 4, 2 },
            { 5, 0 }
        };

        var vertexIxes = new[]
        {
            new[] { 1, 2, 4 },
            new[] { 2, 3, 5 },
            new[] { 3, 4, 6 },
            new[] { 0, 4, 5 },
            new[] { 1, 5, 6 },
            new[] { 0, 2, 6 },
            new[] { 0, 1, 3 }
        };

        var vertex = new[] { 0, 2, 1 };

        for (int i = 0; i < 3; i++)
            tempCube[vertex[i]] = _colorReferences[vertexIxes[(int)_missingColor][i]];

        tryagain:

        var restColors = Enumerable.Range(0, 7).Where(x => !vertexIxes[(int)_missingColor].Contains(x) && (PCColor)x != _missingColor).ToArray().Shuffle();

        for (int i = 0; i < 3; i++)
            tempCube[cubeNetOpposite[vertex[i]]] = _colorReferences[restColors[i]];

        Log(tempCube.Select(x => $"[{x.Color}]").Join());

        GeneratePuzzle();

        if (failed)
            goto tryagain;
    }

    void GeneratePuzzle()
    {
        var positionQueue = new Queue<int>(Enumerable.Range(0, 16).ToList().Shuffle());

        var positionCandidates = new List<int>();

        while (positionQueue.Count > 0)
        {
            var position = positionQueue.Dequeue();

            var cube = new OrientedCube(tempCube.ToArray(), new ColorInfo[16], Enumerable.Range(0, 6).ToArray(), position);

            var candidates = new List<OrientedCube>();

            var adj = DirectionInfo.GetValidDirections(position).Where(x => x != null).ToArray();

            foreach (var cell in adj)
            {
                if (cube.CurrentGrid[cell.Position] == null && cube.CurrentCube[cube.CurrentOrientation[5]] != null)
                {
                    var cubeCandidate = cube.CurrentCube.ToArray();
                    var gridCandidate = cube.CurrentGrid.ToArray();

                    gridCandidate[cell.Position] = cubeCandidate[cube.CurrentOrientation[5]];
                    cubeCandidate[cube.CurrentOrientation[5]] = null;

                    candidates.Add(new OrientedCube(cubeCandidate, gridCandidate, cubeOrientationTable[(int)cell.Direction].Select(x => cube.CurrentOrientation[x]).ToArray(), cell.Position));
                }
            }

            Backtrack(candidates, new List<OrientedCube>() { cube });

            if (isSolved)
            {
                positionCandidates.Add(position);
                isSolved = false;
            }
        }

        failed = positionCandidates.Count == 0;

        if (failed)
        {
            retries++;

            if (retries == 3)
                throw new Exception("Cube puzzle cannot be generated.");

            return;
        }

    }

    void Backtrack(List<OrientedCube> candidates, List<OrientedCube> current)
    {
        if (current.Any(x => x.CurrentCube.All(y => y == null)))
        {
            isSolved = true;
            return;
        }

        foreach (var candidate in candidates)
        {
            var adj = DirectionInfo.GetValidDirections(candidate.CurrentPosition).Where(x => x != null).ToArray();

            var newCandidates = new List<OrientedCube>();

            foreach (var cell in adj)
            {
                var cubeCandidate = candidate.CurrentCube.ToArray();
                var gridCandidate = candidate.CurrentGrid.ToArray();

                if (candidate.CurrentGrid[cell.Position] == null && candidate.CurrentCube[candidate.CurrentOrientation[5]] != null)
                {
                    gridCandidate[cell.Position] = cubeCandidate[candidate.CurrentOrientation[5]];
                    cubeCandidate[candidate.CurrentOrientation[5]] = null;

                    newCandidates.Add(new OrientedCube(cubeCandidate, gridCandidate, cubeOrientationTable[(int)cell.Direction].Select(x => candidate.CurrentOrientation[x]).ToArray(), cell.Position));
                }
            }

            foreach (var newCandidate in newCandidates)
            {
                current.Add(newCandidate);
                Backtrack(newCandidates, current);
                current.Remove(newCandidate);
            }
        }
    }
}
