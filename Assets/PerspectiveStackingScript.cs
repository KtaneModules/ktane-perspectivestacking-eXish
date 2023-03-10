using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class PerspectiveStackingScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;
    public MeshRenderer[] gridRends;
    public Material[] gridMats;
    public TextMesh[] displayTexts;
    public TextMesh[] graphTexts;
    public GameObject graph;

    private int[][][] cube = new int[5][][];
    private int[][] input = new int[5][];
    private int[][] answer = new int[5][];
    private List<Stage> stageList = new List<Stage>();
    private string[] ignoredModules;
    private int maxStages;
    private int curStage;
    private int mode;
    private int rot1;
    private int rot2;
    private int selectedCol = -1;
    private bool lightsOn;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += LightsOn;
    }

    void Start()
    {
        ClearGraph();
        ClearGrid();
        displayTexts[0].text = "";
        displayTexts[1].text = "";
        graph.SetActive(false);
        ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Perspective Stacking", new string[]{
                "14",
                "Forget Enigma",
                "Forget Everything",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Organization",
                "Perspective Stacking",
                "Purgatory",
                "Queen’s War",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ültimate Custom Night",
                "The Very Annoying Button"
            });
        if (!Application.isEditor)
            GenerateStages();
        for (int i = 0; i < cube.Length; i++)
        {
            cube[i] = new int[5][];
            input[i] = new int[5];
            answer[i] = new int[5];
            for (int j = 0; j < cube[i].Length; j++)
                cube[i][j] = new int[5];
        }
    }

    void LightsOn()
    {
        if (Application.isEditor)
            GenerateStages();
        if (maxStages == 0)
        {
            StartCoroutine(HandleEarlySolve());
            return;
        }
        if (curStage == bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count)
            DisplayStage(curStage);
        graph.SetActive(true);
        lightsOn = true;
    }

    void Update()
    {
        if (mode == 0 && lightsOn && curStage != bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count)
        {
            int dif = Math.Abs(curStage - bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count);
            for (int i = 0; i < dif; i++)
                CalculateStage(stageList[curStage + i]);
            curStage = bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
            if (curStage == maxStages)
            {
                mode = 1;
                displayTexts[0].text = "INPUT";
                displayTexts[0].fontSize = 55;
                rot1 = UnityEngine.Random.Range(0, 4);
                rot2 = UnityEngine.Random.Range(0, 4);
                Debug.LogFormat("[Perspective Stacking #{0}] The displayed numbers are: {1} & {2}", moduleId, rot1, rot2);
                displayTexts[1].text = rot1.ToString() + "\n" + rot2.ToString();
                displayTexts[1].fontSize = 55;
                ClearGraph();
                ClearGrid();
                SetAnswerFace();
            }
            else
                DisplayStage(curStage);
        }
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            int index = Array.IndexOf(buttons, pressed);
            if (index < 25 && selectedCol != -1)
            {
                audio.PlaySoundAtTransform("press" + UnityEngine.Random.Range(1, 6), transform);
                gridRends[index].material = gridMats[selectedCol];
                input[index / 5][index % 5] = selectedCol;
            }
            else if (index >= 25)
            {
                if (index == 32 && mode == 1)
                {
                    pressed.AddInteractionPunch();
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                    bool[][] temp = new bool[5][];
                    for (int i = 0; i < 5; i++)
                    {
                        temp[i] = new bool[5];
                        for (int j = 0; j < 5; j++)
                            temp[i][j] = input[i][j] == answer[i][j];
                    }
                    Debug.LogFormat("[Perspective Stacking #{0}] Inputted submission grid:\n", moduleId);
                    for (int i = 0; i < 5; i++)
                        Debug.Log(ConvertColor(gridMats[input[i][0]].name) + ConvertColor(gridMats[input[i][1]].name) + ConvertColor(gridMats[input[i][2]].name) + ConvertColor(gridMats[input[i][3]].name) + ConvertColor(gridMats[input[i][4]].name));
                    if (temp.Any(x => x.Contains(false)))
                    {
                        Debug.LogFormat("[Perspective Stacking #{0}] Incorrect submission, strike", moduleId);
                        GetComponent<KMBombModule>().HandleStrike();
                        mode = 2;
                        displayTexts[0].text = "";
                        displayTexts[1].text = "";
                        displayTexts[1].fontSize = 70;
                        if (selectedCol != -1)
                        {
                            ClearSelectionColor();
                            selectedCol = -1;
                        }
                        StartCoroutine(HandleIncorrect(temp));
                    }
                    else
                    {
                        moduleSolved = true;
                        Debug.LogFormat("[Perspective Stacking #{0}] Correct submission, module solved", moduleId);
                        GetComponent<KMBombModule>().HandlePass();
                        audio.PlaySoundAtTransform("solve", transform);
                        displayTexts[0].text = "";
                        displayTexts[1].text = "";
                        if (selectedCol != -1)
                        {
                            ClearSelectionColor();
                            selectedCol = -1;
                        }
                        PassGrid();
                    }
                }
                else if (index == 32 && mode == 2)
                {
                    pressed.AddInteractionPunch();
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                    StopAllCoroutines();
                    mode = 1;
                    displayTexts[0].text = "INPUT";
                    displayTexts[0].fontSize = 55;
                    rot1 = UnityEngine.Random.Range(0, 4);
                    rot2 = UnityEngine.Random.Range(0, 4);
                    Debug.LogFormat("[Perspective Stacking #{0}] The displayed numbers are: {1} & {2}", moduleId, rot1, rot2);
                    displayTexts[1].text = rot1.ToString() + "\n" + rot2.ToString();
                    displayTexts[1].fontSize = 55;
                    ClearGraph();
                    ClearGrid();
                    SetAnswerFace();
                }
                else if (index == 31 && mode == 1)
                {
                    pressed.AddInteractionPunch();
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                    if (selectedCol != -1)
                        ClearSelectionColor();
                    selectedCol = 0;
                    buttons[index].gameObject.GetComponentInChildren<TextMesh>().color = Color.white;
                }
                else if (mode == 1)
                {
                    pressed.AddInteractionPunch();
                    audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                    if (selectedCol != -1)
                        ClearSelectionColor();
                    selectedCol = index - 24;
                    buttons[index].gameObject.GetComponentInChildren<TextMesh>().color = Color.white;
                }
            }
        }
    }

    void DisplayStage(int stage)
    {
        displayTexts[0].text = (stage + 1).ToString() + "/" + maxStages.ToString();
        displayTexts[0].fontSize = displayTexts[0].text.Length > 4 ? 70 - (13 * (displayTexts[0].text.Length - 4)) : 70;
        displayTexts[1].text = (stageList[stage].z + 1).ToString();
        ClearGraph();
        graphTexts[stageList[stage].gravity].color = new Color(0.58823529411f, 0.58823529411f, 0.58823529411f);
        ClearGrid();
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (stageList[stage].x == j && stageList[stage].y == i)
                {
                    gridRends[i * 5 + j].material = gridMats[stageList[stage].color + 1];
                    i = 5;
                    break;
                }
            }
        }
    }

    void GenerateStages()
    {
        maxStages = bomb.GetSolvableModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
        for (int i = 0; i < maxStages; i++)
        {
            Stage temp = new Stage();
            temp.x = UnityEngine.Random.Range(0, 5);
            temp.y = UnityEngine.Random.Range(0, 5);
            temp.z = UnityEngine.Random.Range(0, 5);
            temp.color = UnityEngine.Random.Range(0, 6);
            temp.gravity = UnityEngine.Random.Range(0, 6);
            temp.stageNum = i + 1;
            stageList.Add(temp);
        }
    }

    void CalculateStage(Stage s)
    {
        Debug.LogFormat("[Perspective Stacking #{0}] <Stage {5}> Placed a {1} cube at {2}{3}{4}", moduleId, gridMats[s.color + 1].name, "ABCDE"[s.x], s.y + 1, s.z + 1, s.stageNum);
        Debug.LogFormat("[Perspective Stacking #{0}] <Stage {2}> Applied gravity in the {1} direction", moduleId, graphTexts[s.gravity].name, s.stageNum);
        cube[s.z][s.y][s.x] = s.color + 1;
        redo:
        bool keepGoing = false;
        if (s.gravity == 0)
        {
            for (int x = 1; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        if (cube[z][y][x] != 0 && (cube[z][y][x - 1] == 0 || cube[z][y][x - 1] == cube[z][y][x]))
                        {
                            cube[z][y][x - 1] = cube[z][y][x];
                            cube[z][y][x] = 0;
                            keepGoing = true;
                        }
                    }
                }
            }
        }
        else if (s.gravity == 1)
        {
            for (int x = 3; x >= 0; x--)
            {
                for (int y = 4; y >= 0; y--)
                {
                    for (int z = 4; z >= 0; z--)
                    {
                        if (cube[z][y][x] != 0 && (cube[z][y][x + 1] == 0 || cube[z][y][x + 1] == cube[z][y][x]))
                        {
                            cube[z][y][x + 1] = cube[z][y][x];
                            cube[z][y][x] = 0;
                            keepGoing = true;
                        }
                    }
                }
            }
        }
        else if (s.gravity == 2)
        {
            for (int y = 3; y >= 0; y--)
            {
                for (int x = 4; x >= 0; x--)
                {
                    for (int z = 4; z >= 0; z--)
                    {
                        if (cube[z][y][x] != 0 && (cube[z][y + 1][x] == 0 || cube[z][y + 1][x] == cube[z][y][x]))
                        {
                            cube[z][y + 1][x] = cube[z][y][x];
                            cube[z][y][x] = 0;
                            keepGoing = true;
                        }
                    }
                }
            }
        }
        else if (s.gravity == 3)
        {
            for (int y = 1; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        if (cube[z][y][x] != 0 && (cube[z][y - 1][x] == 0 || cube[z][y - 1][x] == cube[z][y][x]))
                        {
                            cube[z][y - 1][x] = cube[z][y][x];
                            cube[z][y][x] = 0;
                            keepGoing = true;
                        }
                    }
                }
            }
        }
        else if (s.gravity == 4)
        {
            for (int z = 3; z >= 0; z--)
            {
                for (int x = 4; x >= 0; x--)
                {
                    for (int y = 4; y >= 0; y--)
                    {
                        if (cube[z][y][x] != 0 && (cube[z + 1][y][x] == 0 || cube[z + 1][y][x] == cube[z][y][x]))
                        {
                            cube[z + 1][y][x] = cube[z][y][x];
                            cube[z][y][x] = 0;
                            keepGoing = true;
                        }
                    }
                }
            }
        }
        else if (s.gravity == 5)
        {
            for (int z = 1; z < 5; z++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (cube[z][y][x] != 0 && (cube[z - 1][y][x] == 0 || cube[z - 1][y][x] == cube[z][y][x]))
                        {
                            cube[z - 1][y][x] = cube[z][y][x];
                            cube[z][y][x] = 0;
                            keepGoing = true;
                        }
                    }
                }
            }
        }
        if (keepGoing)
            goto redo;
        Debug.LogFormat("[Perspective Stacking #{0}] <Stage {1}> The internal cube is now:\n", moduleId, s.stageNum);
        for (int y = 0; y < 5; y++)
        {
            for (int z = 4; z >= 0; z--)
                Debug.Log(ConvertColor(gridMats[cube[z][y][0]].name) + ConvertColor(gridMats[cube[z][y][1]].name) + ConvertColor(gridMats[cube[z][y][2]].name) + ConvertColor(gridMats[cube[z][y][3]].name) + ConvertColor(gridMats[cube[z][y][4]].name) + "\n");
            if (y != 4)
                Debug.Log("\n");
        }
    }

    void SetAnswerFace()
    {
        bool[][] received = new bool[5][];
        for (int i = 0; i < 5; i++)
            received[i] = new bool[5];
        if (rot1 == 0 && rot2 == 0)
        {
            for (int z = 0; z < 5; z++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (cube[z][y][x] != 0 && !received[y][x])
                        {
                            answer[y][x] = cube[z][y][x];
                            received[y][x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 0 && rot2 == 1)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int z = 4; z >= 0; z--)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - z][x])
                        {
                            answer[4 - z][x] = cube[z][y][x];
                            received[4 - z][x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 0 && rot2 == 2)
        {
            for (int z = 4; z >= 0; z--)
            {
                for (int y = 4; y >= 0; y--)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - y][x])
                        {
                            answer[4 - y][x] = cube[z][y][x];
                            received[4 - y][x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 0 && rot2 == 3)
        {
            for (int y = 4; y >= 0; y--)
            {
                for (int z = 0; z < 5; z++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (cube[z][y][x] != 0 && !received[z][x])
                        {
                            answer[z][x] = cube[z][y][x];
                            received[z][x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 1 && rot2 == 0)
        {
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int z = 4; z >= 0; z--)
                    {
                        if (cube[z][y][x] != 0 && !received[y][4 - z])
                        {
                            answer[y][4 - z] = cube[z][y][x];
                            received[y][4 - z] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 1 && rot2 == 1)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 4; x >= 0; x--)
                {
                    for (int z = 4; z >= 0; z--)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - x][4 - z])
                        {
                            answer[4 - x][4 - z] = cube[z][y][x];
                            received[4 - x][4 - z] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 1 && rot2 == 2)
        {
            for (int x = 4; x >= 0; x--)
            {
                for (int y = 4; y >= 0; y--)
                {
                    for (int z = 4; z >= 0; z--)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - y][4 - z])
                        {
                            answer[4 - y][4 - z] = cube[z][y][x];
                            received[4 - y][4 - z] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 1 && rot2 == 3)
        {
            for (int y = 4; y >= 0; y--)
            {
                for (int x = 0; x < 5; x++)
                {
                    for (int z = 4; z >= 0; z--)
                    {
                        if (cube[z][y][x] != 0 && !received[x][4 - z])
                        {
                            answer[x][4 - z] = cube[z][y][x];
                            received[x][4 - z] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 2 && rot2 == 0)
        {
            for (int z = 4; z >= 0; z--)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 4; x >= 0; x--)
                    {
                        if (cube[z][y][x] != 0 && !received[y][4 - x])
                        {
                            answer[y][4 - x] = cube[z][y][x];
                            received[y][4 - x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 2 && rot2 == 1)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int z = 0; z < 5; z++)
                {
                    for (int x = 4; x >= 0; x--)
                    {
                        if (cube[z][y][x] != 0 && !received[z][4 - x])
                        {
                            answer[z][4 - x] = cube[z][y][x];
                            received[z][4 - x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 2 && rot2 == 2)
        {
            for (int z = 0; z < 5; z++)
            {
                for (int y = 4; y >= 0; y--)
                {
                    for (int x = 4; x >= 0; x--)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - y][4 - x])
                        {
                            answer[4 - y][4 - x] = cube[z][y][x];
                            received[4 - y][4 - x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 2 && rot2 == 3)
        {
            for (int y = 4; y >= 0; y--)
            {
                for (int z = 4; z >= 0; z--)
                {
                    for (int x = 4; x >= 0; x--)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - z][4 - x])
                        {
                            answer[4 - z][4 - x] = cube[z][y][x];
                            received[4 - z][4 - x] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 3 && rot2 == 0)
        {
            for (int x = 4; x >= 0; x--)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        if (cube[z][y][x] != 0 && !received[y][z])
                        {
                            answer[y][z] = cube[z][y][x];
                            received[y][z] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 3 && rot2 == 1)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        if (cube[z][y][x] != 0 && !received[x][z])
                        {
                            answer[x][z] = cube[z][y][x];
                            received[x][z] = true;
                        }
                    }
                }
            }
        }
        else if (rot1 == 3 && rot2 == 2)
        {
            for (int x = 0; x < 5; x++)
            {
                for (int y = 4; y >= 0; y--)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - y][z])
                        {
                            answer[4 - y][z] = cube[z][y][x];
                            received[4 - y][z] = true;
                        }
                    }
                }
            }
        }
        else
        {
            for (int y = 4; y >= 0; y--)
            {
                for (int x = 4; x >= 0; x--)
                {
                    for (int z = 0; z < 5; z++)
                    {
                        if (cube[z][y][x] != 0 && !received[4 - x][z])
                        {
                            answer[4 - x][z] = cube[z][y][x];
                            received[4 - x][z] = true;
                        }
                    }
                }
            }
        }
        Debug.LogFormat("[Perspective Stacking #{0}] Expected submission grid:\n", moduleId);
        for (int i = 0; i < 5; i++)
            Debug.Log(ConvertColor(gridMats[answer[i][0]].name) + ConvertColor(gridMats[answer[i][1]].name) + ConvertColor(gridMats[answer[i][2]].name) + ConvertColor(gridMats[answer[i][3]].name) + ConvertColor(gridMats[answer[i][4]].name));
    }

    void ClearGraph()
    {
        for (int i = 0; i < graphTexts.Length; i++)
            graphTexts[i].color = Color.white;
    }

    void ClearGrid()
    {
        for (int i = 0; i < gridRends.Length; i++)
            gridRends[i].material = gridMats[0];
    }

    void ClearSelectionColor()
    {
        for (int i = 25; i < 32; i++)
            buttons[i].gameObject.GetComponentInChildren<TextMesh>().color = Color.black;
    }

    void PassGrid()
    {
        for (int i = 0; i < gridRends.Length; i++)
            gridRends[i].material = gridMats[2];
    }

    string ConvertColor(string c)
    {
        if (c == "grey")
            return "x";
        else
            return c.ToUpper()[0].ToString();
    }

    IEnumerator HandleEarlySolve()
    {
        yield return null;
        moduleSolved = true;
        GetComponent<KMBombModule>().HandlePass();
        Debug.LogFormat("[Perspective Stacking #{0}] Module forcefully solved due to no non-ignored modules being detected", moduleId);
    }

    IEnumerator HandleIncorrect(bool[][] grid)
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                input[i][j] = 0;
                answer[i][j] = 0;
                if (grid[i][j])
                    gridRends[i * 5 + j].material = gridMats[2];
                else
                    gridRends[i * 5 + j].material = gridMats[1];
            }
        }
        yield return new WaitForSeconds(4f);
        ClearGrid();
        int counter = 0;
        while (true)
        {
            if (counter == maxStages)
                counter = 0;
            DisplayStage(counter);
            yield return new WaitForSeconds(3f);
            counter++;
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <R/G/B/C/M/Y/X> [Presses the specified button] | !{0} <A-E><1-5> [Presses the specified square with letter as column and number as row] | !{0} submit [Presses the SUB button] | The first two commands can be chained with spaces";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (mode == 0)
            {
                yield return "sendtochaterror The SUB button cannot be interacted with right now!";
                yield break;
            }
            yield return null;
            buttons[32].OnInteract();
        }
        else
        {
            string[] parameters = command.Split(' ');
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Length != 1 && parameters[i].Length != 2)
                {
                    yield return "sendtochaterror!f What the heck is '" + parameters[i] + "' supposed to mean?";
                    yield break;
                }
                if (parameters[i].Length == 1 && !parameters[i].ToUpperInvariant().EqualsAny("R", "G", "B", "C", "M", "Y", "X"))
                {
                    yield return "sendtochaterror!f What the heck is '" + parameters[i] + "' supposed to mean?";
                    yield break;
                }
                if (parameters[i].Length == 2 && (!parameters[i].ToUpperInvariant()[0].EqualsAny('A', 'B', 'C', 'D', 'E') || !parameters[i].ToUpperInvariant()[1].EqualsAny('1', '2', '3', '4', '5')))
                {
                    yield return "sendtochaterror!f What the heck is '" + parameters[i] + "' supposed to mean?";
                    yield break;
                }
            }
            if (mode != 1)
            {
                yield return "sendtochaterror These buttons cannot be interacted with right now!";
                yield break;
            }
            yield return null;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ToUpperInvariant() == "X")
                    buttons[31].OnInteract();
                else if (parameters[i].Length == 1)
                    buttons[25 + "RGBCMY".IndexOf(parameters[i].ToUpperInvariant())].OnInteract();
                else
                    buttons[(int.Parse(parameters[i].ToUpperInvariant()[1].ToString()) - 1) * 5 + "ABCDE".IndexOf(parameters[i].ToUpperInvariant()[0])].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (mode == 0) yield return true;
        if (mode == 2)
        {
            buttons[32].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        if (selectedCol != -1)
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (answer[i][j] == selectedCol && input[i][j] != answer[i][j])
                    {
                        buttons[i * 5 + j].OnInteract();
                        yield return new WaitForSeconds(.1f);
                    }
                }
            }
        }
        for (int i = 0; i < 7; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (answer[j][k] == i && input[j][k] != answer[j][k])
                    {
                        if (selectedCol != i)
                        {
                            if (i == 0)
                            {
                                buttons[31].OnInteract();
                                yield return new WaitForSeconds(.1f);
                            }
                            else
                            {
                                buttons[24 + i].OnInteract();
                                yield return new WaitForSeconds(.1f);
                            }
                        }
                        buttons[j * 5 + k].OnInteract();
                        yield return new WaitForSeconds(.1f);
                    }
                }
            }
        }
        buttons[32].OnInteract();
    }
}

class Stage
{
    public int x;
    public int y;
    public int z;
    public int color;
    public int gravity;
    public int stageNum;
}