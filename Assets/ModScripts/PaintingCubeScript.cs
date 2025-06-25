using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Debug;
using static UnityEngine.Random;

public class PaintingCubeScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable[] gridButtons;
    public KMSelectable reset;

    public Transform cube;
    public MeshRenderer[] cubeFaceRenders;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private bool cbActive;

    private ColorInfo[] grid = new ColorInfo[16], initialGrid;
    private readonly ColorInfo[] cubeFaces = new ColorInfo[6];
    private List<PCColor> netColors;
    private PCColor missingColor;

    public GameObject debugDot;

    private int currentCubePos, startingCubePos;

    private int[] cubeFaceIxes;

    private Coroutine cubeMoving;

    private DirectionInfo[] validDirections;

    private PaintingCubePuzzle puzzle;

    private static readonly Color[] faceColors =
    {
        Color.red,
        new Color(1, 0.5f, 0),
        new Color(1, 1, 0),
        Color.green,
        new Color(0, 0.4f, 1),
        new Color(0, 0, 0.5f),
        new Color(0.5f, 0, 0.5f)
    };

    private static readonly Color32 gridBorderColor = new Color32(141, 159, 194, 255);

    private static readonly int[][] cubeOrientationTable =
    {
        new[] { 1, 5, 2, 0, 4, 3 },
        new[] { 4, 1, 0, 3, 5, 2 },
        new[] { 3, 0, 2, 5, 4, 1 },
        new[] { 2, 1, 5, 3, 0, 4 }
    };

    private Vector3 ObtainGridPos(int cubePos)
    {
        var unmodified = gridButtons[cubePos].transform.localPosition;

        return new Vector3(unmodified.x, 0.01f, unmodified.z);
    }

    private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) => rotation * (point - pivot) + pivot;


    void Awake()
    {

        moduleId = moduleIdCounter++;

        foreach (KMSelectable button in gridButtons)
            button.OnInteract += delegate () { GridButtonPress(button); return false; };

        reset.OnInteract += delegate () { ResetPress(); return false; };

        cbActive = Colorblind.ColorblindModeActive;

    }

    void Start()
    {
        // Under no circumstances should this be enabled outside of the editor.
        debugDot.SetActive(Application.isEditor);

        netColors = Enumerable.Range(0, 7).Select(x => (PCColor) x).ToList();
        missingColor = (PCColor) Range(0, 7);

        netColors.RemoveAt((int) missingColor);

        puzzle = new PaintingCubePuzzle(missingColor, Enumerable.Range(0, 7).Select(x => new ColorInfo((PCColor) x, faceColors[x])).ToArray(), RuleSeedable.GetRNG());

        grid = puzzle.Grid.ToArray();
        currentCubePos = startingCubePos = puzzle.StartingPos;
        initialGrid = grid.ToArray();

        SetGrid();
        SetCube();

        cubeFaceIxes = Enumerable.Range(0, 6).ToArray();

        validDirections = DirectionInfo.GetValidDirections(currentCubePos);
        cube.localPosition = ObtainGridPos(currentCubePos);

        var ruleseedCheck = RuleSeedable.GetRNG().Seed;

        if (ruleseedCheck != 1)
            Log($"[Painting Cube #{moduleId}] Currently using ruleseed #{ruleseedCheck}");

        Log($"[Painting Cube #{moduleId}] The missing color from the grid is: {missingColor}. The correct vertex to use is: {puzzle.ObtainVertex()}");
        Log($"[Painting Cube #{moduleId}] The initial grid is: {puzzle.ObtainGrid()}");
        Log($"[Painting Cube #{moduleId}] The cube's starting position is at {"ABCD"[startingCubePos % 4]}{(startingCubePos / 4) + 1}");
        Log($"[Painting Cube #{moduleId}] The number of moves you could possibly make are at least or at most {puzzle.TrackedDirections.Count}");
    }

    void SetGrid()
    {
        for (int i = 0; i < 16; i++)
        {
            gridButtons[i].GetComponent<MeshRenderer>().material.color = grid[i]?.MatColor ?? gridBorderColor;
            gridButtons[i].GetComponentInChildren<TextMesh>().text = cbActive ? grid[i]?.Color.ToString()[0].ToString() ?? string.Empty : string.Empty;
        }

    }

    void SetCube()
    {
        for (int i = 0; i < 6; i++)
        {
            cubeFaceRenders[i].material.color = cubeFaces[i]?.MatColor ?? Color.white;
            cubeFaceRenders[i].GetComponentInChildren<TextMesh>().text = cbActive ? cubeFaces[i]?.Color.ToString()[0].ToString() ?? string.Empty : string.Empty;

            if (cubeFaces[i] == null || !cbActive)
                continue;

            var faceColor = cubeFaces[i].Color;

            cubeFaceRenders[i].GetComponentInChildren<TextMesh>().color = Enumerable.Range(0, 4).Any(x => (PCColor) x == faceColor) ? Color.black : Color.white;
        }
    }

    void GridButtonPress(KMSelectable button)
    {
        var ix = Array.IndexOf(gridButtons, button);

        if (moduleSolved || cubeMoving != null || validDirections.All(x => x?.Position != ix))
            return;

        var movingIx = validDirections.IndexOf(x => x?.Position == ix);

        cubeMoving = StartCoroutine(MoveCube(validDirections[movingIx]));
    }

    void ResetPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        reset.AddInteractionPunch(0.4f);

        if (moduleSolved || cubeMoving != null)
            return;

        currentCubePos = startingCubePos;
        validDirections = DirectionInfo.GetValidDirections(currentCubePos);

        for (int i = 0; i < 6; i++)
            cubeFaces[i] = null;

        grid = initialGrid.ToArray();

        cube.localEulerAngles = Vector3.zero;
        cube.localPosition = ObtainGridPos(currentCubePos);

        cubeFaceIxes = Enumerable.Range(0, 6).ToArray();

        SetGrid();
        SetCube();
    }

    IEnumerator MoveCube(DirectionInfo dir)
    {
        var duration = 0.25f;
        var elapsed = 0f;

        var axis = new[]
        {
            new[] { Vector3.forward, Vector3.right },
            new[] { Vector3.right, Vector3.back },
            new[] { Vector3.back, Vector3.left },
            new[] { Vector3.left, Vector3.forward }
        };

        var copiedOrientation = cubeFaceIxes.ToArray();

        var rotSet = axis[(int) dir.Direction];

        var startPos = cube.localPosition;
        var startRot = cube.localRotation;
        var rotPoint = startPos + (rotSet[0] + Vector3.down) / 2 * 0.03f;

        if (grid[currentCubePos] != null && cubeFaces[cubeFaceIxes[5]] == null)
        {
            cubeFaces[cubeFaceIxes[5]] = grid[currentCubePos];
            grid[currentCubePos] = null;
        }
        else if (grid[currentCubePos] == null && cubeFaces[cubeFaceIxes[5]] != null)
        {
            grid[currentCubePos] = cubeFaces[cubeFaceIxes[5]];
            cubeFaces[cubeFaceIxes[5]] = null;
        }

        SetGrid();
        SetCube();

        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            var t = Easing.InOutSine(Mathf.Min(elapsed, duration), 0, 1, duration);
            cube.localRotation = Quaternion.AngleAxis(90 * t, rotSet[1]) * startRot;
            cube.localPosition = RotatePointAroundPivot(startPos, rotPoint, Quaternion.AngleAxis(90 * t, rotSet[1]));
        }

        Audio.PlaySoundAtTransform("Block", transform);

        cubeFaceIxes = cubeOrientationTable[(int) dir.Direction].Select(x => copiedOrientation[x]).ToArray();

        currentCubePos = dir.Position;
        validDirections = DirectionInfo.GetValidDirections(currentCubePos);

        if (cubeFaces.Count(x => x != null) == 5 && grid[currentCubePos] != null && cubeFaces[cubeFaceIxes[5]] == null)
        {
            cubeFaces[cubeFaceIxes[5]] = grid[currentCubePos];
            grid[currentCubePos] = null;

            SetGrid();
            SetCube();

            if (new[] { Enumerable.Range(0, 3).ToArray(), new[] { 0, 2, 3 }, new[] { 0, 3, 4 }, new[] { 0, 4, 1 } }.Select(x => x.Select(y => cubeFaces[cubeFaceIxes[y]]).ToArray()).Any(puzzle.CheckVertex))
            {
                Log($"[Painting Cube #{moduleId}] The current orientation of the cube has the correct vertex. Solved!");
                StartCoroutine(Solve());
            }
        }

        cubeMoving = null;
    }

    IEnumerator Solve()
    {
        var duration = 1f;
        var elapsed = 0f;

        var oldPos = cube.localPosition;
        var nahIdWin = new Vector3(oldPos.x, 4, oldPos.z);

        foreach (var text in gridButtons.Select(x => x.GetComponentInChildren<TextMesh>()))
            text.text = string.Empty;

        foreach (var text in cubeFaceRenders.Select(x => x.GetComponentInChildren<TextMesh>()))
            text.text = string.Empty;

        Audio.PlaySoundAtTransform("Solve", transform);

        moduleSolved = true;
        Module.HandlePass();

        Coroutine spin, flashingGrid;

        spin = StartCoroutine(SpinningCube());
        flashingGrid = StartCoroutine(FlashGrid());

        while (elapsed < duration)
        {
            cube.localPosition = new Vector3(oldPos.x, Easing.InQuint(elapsed, oldPos.y, nahIdWin.y, duration), oldPos.z);
            yield return null;
            elapsed += Time.deltaTime;
        }

        StopCoroutine(spin);

        cube.localPosition = nahIdWin;
        cube.gameObject.SetActive(false);
    }

    IEnumerator SpinningCube()
    {
        float offset;
        var buildup = 0f;

        while (true)
        {
            if (buildup < 2.5f)
                buildup += 0.05f;

            offset = Time.deltaTime * 35 * buildup;
            cube.localEulerAngles += offset * Vector3.one;
            yield return null;
        }
    }

    IEnumerator FlashGrid()
    {
        yield return null;

        var renders = gridButtons.Select(x => x.GetComponent<MeshRenderer>()).ToArray();

        var ixOrder = new[] { 0, 1, 2, 3, 7, 11, 15, 14, 13, 12, 8, 4, 5, 6, 10, 9 };

        Coroutine[] startFlashingCell = new Coroutine[16];

        for (int i = 0; i < 16; i++)
        {
            startFlashingCell[ixOrder[i]] = StartCoroutine(FlashCell(renders[ixOrder[i]]));
            yield return new WaitForSeconds(0.03f);
        }
        yield return new WaitForSeconds(0.4f);

        foreach (var cell in startFlashingCell)
            StopCoroutine(cell);

        foreach (var render in renders)
            render.material.color = Color.green;

        yield return new WaitForSeconds(0.05f);

        foreach (var render in renders)
            render.material.color = gridBorderColor;

        yield return new WaitForSeconds(0.05f);

        foreach (var render in renders)
            render.material.color = Color.green;

    }

    IEnumerator FlashCell(MeshRenderer cell)
    {
        var colorIx = 0;

        while (true)
        {
            cell.material.color = faceColors[colorIx];

            yield return new WaitForSeconds(0.03f);

            colorIx++;
            colorIx %= faceColors.Length;

        }
    }

    // Twitch Plays


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} move urdl to move the cube that many directions || !{0} cb to toggle colorblind || !{0} reset to reset the module back to its initial state";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        yield return null;

        if ("CB".ContainsIgnoreCase(split[0]))
        {
            cbActive = !cbActive;
            SetCube();
            SetGrid();
            yield break;
        }

        if ("MOVE".ContainsIgnoreCase(split[0]))
        {
            if (split.Length == 1)
            {
                yield return "sendtochaterror I don't understand.";
                yield break;
            }

            if (split.Length > 2)
            {
                yield return "sendtochaterror Please input your moves without spaces!";
                yield break;
            }

            if (!split[1].Any("URDL".Contains))
            {
                yield return $"sendtochaterror {split[1].Where(x => !"URDL".Contains(x)).Join(", ")} is/are invalid!";
                yield break;
            }

            var directions = split[1].Select(x => (CubeDirection)"URDL".IndexOf(x)).ToList();

            foreach (var direction in directions)
            {
                var dirPosition = DirectionInfo.GetValidDirections(currentCubePos).SingleOrDefault(x => x?.Direction == direction);

                if (dirPosition == null)
                {
                    yield return $"sendtochaterror The module has halted since going {direction.ToString().ToLowerInvariant()} goes out of bounds.";
                    yield break;
                }

                gridButtons[dirPosition.Position].OnInteract();
                yield return new WaitUntil(() => cubeMoving == null);
            }

            yield break;
        }

        if ("RESET".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
                yield break;

            reset.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;

        var makeMoves = puzzle.TrackedDirections.ToList();

        while (cubeMoving != null)
            yield return true;

        if (currentCubePos != startingCubePos)
        {
            reset.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        for (int i = makeMoves.Count - 1; i >= 0; i--)
        {
            gridButtons[makeMoves[i].Position].OnInteract();
            yield return new WaitUntil(() => cubeMoving == null);
        }
    }


}





