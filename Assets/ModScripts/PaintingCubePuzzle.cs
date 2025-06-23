using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Random;
using static UnityEngine.Debug;

public class PaintingCubePuzzle
{
    private PCColor _missingColor;

    private ColorInfo[] _colorReferences;

    private bool failed;

    private int retries = 0;

    private ColorInfo[] tempCube = new ColorInfo[6];
    public ColorInfo[] Grid;
    public int StartingPos;
    public List<DirectionInfo> TrackedDirections;

    public bool CheckVertex(ColorInfo[] vertex) => Enumerable.Range(0, 3).Select(x => tempCube[x]).SequenceEqual(vertex);

    

    private static readonly int[][] cubeOrientationTable =
    {
        new[] { 1, 5, 2, 0, 4, 3 },
        new[] { 4, 1, 0, 3, 5, 2 },
        new[] { 3, 0, 2, 5, 4, 1 },
        new[] { 2, 1, 5, 3, 0, 4 }
    };

    public PaintingCubePuzzle(PCColor missingColor, ColorInfo[] colorReferences, MonoRandom ruleSeeder)
    {
        _missingColor = missingColor;
        _colorReferences = colorReferences;

        GenerateCube(ruleSeeder);
    }

    private struct OrientedCube
    {
        public ColorInfo[] CurrentCube { get; set; }
        public ColorInfo[] CurrentGrid { get; set; }
        public int[] CurrentOrientation { get; set; }
        public int CurrentPosition { get; set; }

        public OrientedCube(ColorInfo[] currentCube, ColorInfo[] currentGrid, int[] currentOrientation, int currentPosition)
        {
            CurrentCube = currentCube;
            CurrentGrid = currentGrid;
            CurrentOrientation = currentOrientation;
            CurrentPosition = currentPosition;
        }
    }

    private List<ColorInfo[]> gridCandidates = new List<ColorInfo[]>();
    private List<List<DirectionInfo>> trackedMovesCandidates = new List<List<DirectionInfo>>();
    private List<int> startingPosCandidates = new List<int>();

    void GenerateCube(MonoRandom ruleSeeder)
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

        var pcColors = (PCColor[]) Enum.GetValues(typeof(PCColor));
        var vertexIxes = new PCColor[7][];
        for (var cube = 0; cube < 7; cube++)
        {
            var colors = pcColors.ToList();
            colors.Remove((PCColor) cube);
            ruleSeeder.ShuffleFisherYates(colors);
            vertexIxes[cube] = new PCColor[3];
            for (var i = 0; i < 3; i++)
                vertexIxes[cube][i] = colors[i];
        }

        var vertex = Enumerable.Range(0, 3).ToArray();

        for (int i = 0; i < 3; i++)
            tempCube[vertex[i]] = _colorReferences[(int) vertexIxes[(int) _missingColor][i]];

        tryagain:

        var restColors = pcColors.Where(color => !vertexIxes[(int)_missingColor].Contains(color) && color != _missingColor).ToArray().Shuffle();

        for (int i = 0; i < 3; i++)
            tempCube[cubeNetOpposite[vertex[i]]] = _colorReferences[(int) restColors[i]];

        Log(tempCube.Select(x => $"[{x.Color}]").Join());

        GeneratePuzzle();

        if (failed)
        {
            retries++;

            if (retries > 3)
                throw new Exception("The puzzle cannot be generated.");

            failed = false;

            goto tryagain;
        }

        var randomCandidate = Range(0, gridCandidates.Count);

        StartingPos = startingPosCandidates[randomCandidate];
        Grid = gridCandidates[randomCandidate];
        TrackedDirections = trackedMovesCandidates[randomCandidate];
    }

    void GeneratePuzzle()
    {
        var goalQueue = new Queue<int>(Enumerable.Range(0, 16).ToList().Shuffle());

        var oppositeDir = new Dictionary<CubeDirection, CubeDirection>
        {
            { CubeDirection.Up, CubeDirection.Down },
            { CubeDirection.Right, CubeDirection.Left },
            { CubeDirection.Down, CubeDirection.Up },
            { CubeDirection.Left, CubeDirection.Right }
        };

        while (goalQueue.Count > 0)
        {
            var goal = goalQueue.Dequeue();

            var cube = new OrientedCube(tempCube.ToArray(), new ColorInfo[16], Enumerable.Range(0, 6).ToArray(), goal);

            var trackedCandidates = new List<DirectionInfo>();

            while (true)
            {
                if (cube.CurrentCube.All(x => x == null) || trackedCandidates.Count > 100)
                    break;

                var randomMove = DirectionInfo.GetValidDirections(cube.CurrentPosition).Where(x => x != null).PickRandom();

                trackedCandidates.Add(new DirectionInfo(oppositeDir[randomMove.Direction], cube.CurrentPosition));

                if (cube.CurrentGrid[cube.CurrentPosition] == null && cube.CurrentCube[cube.CurrentOrientation[5]] != null)
                {
                    cube.CurrentGrid[cube.CurrentPosition] = cube.CurrentCube[cube.CurrentOrientation[5]];
                    cube.CurrentCube[cube.CurrentOrientation[5]] = null;
                }
                else if (cube.CurrentGrid[cube.CurrentPosition] != null && cube.CurrentCube[cube.CurrentOrientation[5]] == null)
                {
                    cube.CurrentCube[cube.CurrentOrientation[5]] = cube.CurrentGrid[cube.CurrentPosition];
                    cube.CurrentGrid[cube.CurrentPosition] = null;
                }

                cube.CurrentPosition = randomMove.Position;
                var orientation = cube.CurrentOrientation.ToArray();
                cube.CurrentOrientation = cubeOrientationTable[(int)randomMove.Direction].Select(x => orientation[x]).ToArray();
            }

            if (trackedCandidates.Count > 100)
                continue;

            gridCandidates.Add(cube.CurrentGrid.ToArray());
            startingPosCandidates.Add(cube.CurrentPosition);
            trackedMovesCandidates.Add(trackedCandidates.ToList());
        }

        if (gridCandidates.Count == 0)
            failed = true;

    }
}
