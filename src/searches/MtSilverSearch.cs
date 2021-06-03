/*using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Threading;

public class EndState {

    public GscTile Tile;
    public string Path;
    public bool CanA;
    public int HRA;
    public int HRS;
    public int RDIV;

    public override int GetHashCode() {
        return Tuple.Create(Tile.X, Tile.Y, HRA, HRS, RDIV).GetHashCode();
    }
}

public static class MtSilverSearch {

    public static bool Buffered = false;
    public static bool Overflow = true;
    public static int PassNum = 1;
    public static int Section = 4;
    public static int GlobalDelay = 20;

    public static StreamWriter startWriter = new StreamWriter("r29_start.txt");
    public static StreamWriter r29Writer = new StreamWriter("r29_r29_" + GlobalDelay + ".txt");
    public static StreamWriter cherrygroveWriter = new StreamWriter("r29_cherrygrove_" + GlobalDelay + ".txt");
    public static StreamWriter endWriter = new StreamWriter("r29_end_" + GlobalDelay + ".txt");

    public static GscMap r28;
    public static GscMap mtsilver;
    public static GscMap silverCaveOutside;
    public static GscMap silverCaveRoom1;
    public static GscMap silverCaveRoom2;
    public static GscMap silverCaveRoom3;

    public static void Start(string[] args) {
        string[] startPaths = {
            "L L L L L L L L L D L A+L L D D D D L L L L L L D D",
            "L L L L L L L L L L D D L L D D D L D D L L L L L",
            "L L L L L L L L L L D D L L D D D L D D L L L L L",
            "L L L L L L L L L L L D L D D D D L D L L L D L L",
        };
        BuildGraph();

        //StartSearch(10, 0, 30);
        EndSearch(10, GlobalDelay, startPaths[GlobalDelay - 19], 60);
        //IGT(0, "L L L A+L D L L A+L L D L L L D D L A+D L A+L L L L A+D D L");
    }

    public static void BuildGraph() {
        Gold gb = new Gold();
        r28 = gb.Maps["Route28"];
		silverCaveOutside = gb.Maps["SilverCaveOutside"];
        silverCaveRoom1 = gb.Maps["SilverCaveRoom1"];
		silverCaveRoom2 = gb.Maps["SilverCaveRoom2"];
		silverCaveRoom3 = gb.Maps["SilverCaveRoom3"];

        Pathfinding.GenerateEdges(r28, 0, 13, r28.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, r28[33, 6]);
        Pathfinding.GenerateEdges(silverCaveOutside, 18, 11, silverCaveOutside.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveOutside[39, 33]);
        Pathfinding.GenerateEdges(silverCaveRoom1, 3, 2, silverCaveRoom1.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveRoom1[15, 33]);
		Pathfinding.GenerateEdges(silverCaveRoom2, 6, 5, silverCaveRoom2.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveRoom2[19, 31]);
		Pathfinding.GenerateEdges(silverCaveRoom3, 9, 11, silverCaveRoom3.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveRoom3[10, 33]);
        r28[0, 12].AddEdge(0, new Edge<GscTile> { Action = Action.Left, Cost = 0, NextEdgeset = 0, NextTile = silverCaveOutside[39, 30] });
        r28[0, 13].AddEdge(0, new Edge<GscTile> { Action = Action.Left | Action.A, Cost = 0, NextEdgeset = 0, NextTile = silverCaveOutside[39, 31] });
        r28[0, 12].AddEdge(0, new Edge<GscTile> { Action = Action.Left, Cost = 0, NextEdgeset = 0, NextTile = silverCaveOutside[39, 30] });
        r28[0, 13].AddEdge(0, new Edge<GscTile> { Action = Action.Left | Action.A, Cost = 0, NextEdgeset = 0, NextTile = silverCaveOutside[39, 31] });
        silverCaveOutside[18, 12].AddEdge(0, new Edge<GscTile> { Action = Action.Up, Cost = 0, NextEdgeset = 0, NextTile = silverCaveRoom1[9, 33] });
        silverCaveOutside[18, 12].AddEdge(0, new Edge<GscTile> { Action = Action.Up | Action.A, Cost = 0, NextEdgeset = 0, NextTile = silverCaveRoom1[9, 33] });
        silverCaveRoom1[15, 2].AddEdge(0, new Edge<GscTile> { Action = Action.Up, Cost = 0, NextEdgeset = 0, NextTile = silverCaveRoom2[17, 31] });
        silverCaveRoom1[15, 2].AddEdge(0, new Edge<GscTile> { Action = Action.Up | Action.A, Cost = 0, NextEdgeset = 0, NextTile = silverCaveRoom2[17, 31] });
        silverCaveRoom2[11, 6].AddEdge(0, new Edge<GscTile> { Action = Action.Up, Cost = 0, NextEdgeset = 0, NextTile = silverCaveRoom3[9, 33] });
        silverCaveRoom2[11, 6].AddEdge(0, new Edge<GscTile> { Action = Action.Up | Action.A, Cost = 0, NextEdgeset = 0, NextTile = silverCaveRoom3[9, 33] });
        gb.Dispose();
    }

    public static void StartSearch(int numThreads, int minDelay, int maxDelay) {
        Gold gb = new Gold(true);
        List<GscTile> endTiles = new List<GscTile>() { r29[38, 16] };

        new GscIntroSequence(GscStrat.GfSkip, GscStrat.TitleSkip, GscStrat.Continue).ExecuteUntilIGT(gb);
        gb.AdvanceFrames(minDelay, Joypad.Left);
        byte[] state = gb.SaveState();
        for(int i = 0; i < stateList.Count(); i++) {
            for(int igt = 0; igt < 60; igt++) {
                gb.LoadState(state);
                gb.CpuWrite("wGameTimeFrames", (byte) igt);
                gb.AdvanceFrames(i, Joypad.Left);
                gb.Hold(Joypad.A, "OWPlayerInput");
                stateList[i][igt] = gb.SaveState();
            }
        }

        RandomPathSearch.StartSearch<Gold, GscTile>(numThreads,
                                                    new RandomSearchParameters<GscTile>() {
                                                        StateList = stateList,
                                                        ClusterSize = 1,
                                                        SS = 60,
                                                        NumPathsToFind = int.MaxValue,
                                                        StartEdgeSet = 0,
                                                        StartTile = gb.Tile,
                                                        EndTiles = endTiles.ToArray(),
                                                        ExecutionCallback = (gb, actions) => gb.Execute(actions) == gb.OverworldLoopAddress,
                                                        FoundCallback = (stateIndex, actions, successes) => {
                                                            lock(startWriter) {
                                                                startWriter.WriteLine(successes + " " + stateIndex + " " + ActionFunctions.ActionsToPath(actions));
                                                                startWriter.Flush();
                                                            }
                                                        },
                                                    });
    }

    public static void EndSearch(int numThreads, int delay, string path, int ss) {
        Gold[] gbs = MultiThread.MakeThreads<Gold>(numThreads);
        byte[][] initialStates = Gsc.IGTCheckParallel(gbs, 120, new GscIntroSequence(delay, GscStrat.GfSkip, GscStrat.TitleSkip, GscStrat.Continue), 60, gb => gb.Execute(path) == gb.OverworldLoopAddress).States;
        R29(gbs, initialStates, r29[38, 16]);
    }

    static void R29(Gold[] gbs, byte[][] states, GscTile startTile) {
        HashSet<(int, int, int)> rngs = new HashSet<(int, int, int)>();
        while(true) {
            Console.WriteLine("Searching for r29 path...");
            var ret = RandomPathSearch.StartSearch(gbs,
                                                   new RandomSearchParameters<GscTile>() {
                                                       StateList = new List<byte[][]>() { states },
                                                       ClusterSize = 1,
                                                       SS = 60,
                                                       NumPathsToFind = 1,
                                                       StartEdgeSet = 0,
                                                       StartTile = startTile,
                                                       EndTiles = new GscTile[] { cherrygrove[33, 7] },
                                                       ExecutionCallback = (gb, action) => gb.Execute(action) == gb.OverworldLoopAddress,
                                                   }).First();

            Action[] actions = ret.Actions;
            IGTResults results = ret.Results[0];
            (int, int, int) rng = (results.MostCommonHRA, results.MostCommonHRS, results.MostCommonDivider);
            r29Writer.WriteLine(ActionFunctions.ActionsToPath(ret.Actions));
            r29Writer.Flush();
            if(!rngs.Add(rng)) {
                continue;
            }
            gbs[0].LoadState(results.FirstState);
            Cherrygrove(gbs, ret.Actions, results.States, gbs[0].Tile);
        }
    }

    static void Cherrygrove(Gold[] gbs, Action[] path, byte[][] states, GscTile startTile) {
        long cherryPathsFound = 0;
        long numSeenPaths = 0;
        HashSet<(int, int, int)> rngs = new HashSet<(int, int, int)>();
        while(numSeenPaths < 30) {
            GC.Collect();
            Console.WriteLine("Searching for cherrygrove path (" + cherryPathsFound + ")...");
            var ret = RandomPathSearch.StartSearch(gbs,
                                                   new RandomSearchParameters<GscTile>() {
                                                       StateList = new List<byte[][]>() { states },
                                                       ClusterSize = 1,
                                                       SS = 60,
                                                       NumPathsToFind = 1,
                                                       StartEdgeSet = 0,
                                                       StartTile = startTile,
                                                       EndTiles = new GscTile[] { r30[12, 46], r30[13, 46] },
                                                       ExecutionCallback = (gb, actions) => gb.Execute(actions) == gb.OverworldLoopAddress,
                                                   }).First();

            cherryPathsFound++;
            Action[] actions = ret.Actions;
            IGTResults results = ret.Results[0];
            (int, int, int) rng = (results.MostCommonHRA, results.MostCommonHRS, results.MostCommonDivider);
            Action[] concatPath = path.Concat(actions).ToArray();
            cherrygroveWriter.WriteLine(ActionFunctions.ActionsToPath(concatPath));
            cherrygroveWriter.Flush();
            if(!rngs.Add(rng)) {
                numSeenPaths++;
                continue;
            } else {
                numSeenPaths = 0;
            }
            gbs[0].LoadState(results.FirstState);
            End(gbs, concatPath, results.States, gbs[0].Tile);
        }
    }

    public static void End(Gold[] gbs, Action[] path, byte[][] states, GscTile startTile) {
        Console.WriteLine("Searching for end path...");
        DFParameters<Gold, GscTile> parameters = new DFParameters<Gold, GscTile>() {
            NoEncounterSS = 60,
            MaxCost = 0,
            EndTiles = new GscTile[] { r30[17, 12] },
            FoundCallback = state => {
                Console.WriteLine("Found an end path!!");
                endWriter.WriteLine(ActionFunctions.ActionsToPath(path) + " " + state.Log);
                endWriter.Flush();
            }
        };
        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
    }

    public static void IGT(int delay, string path) {
        MakeSave();
        IGTResults res = Gsc.IGTCheckParallel(MultiThread.MakeThreads<Gold>(8), 120, new GscIntroSequence(delay, GscStrat.GfSkip, GscStrat.TitleSkip, GscStrat.Continue), 60, gb => gb.Execute(path) == gb.OverworldLoopAddress);
        Console.WriteLine(res.TotalSuccesses + " (RNG: " + res.RNGSuccesses(10) + ")");
    }

    public static void Record(int delay, string path) {
        MakeSave();
        Gold gb = new Gold(true);
        gb.Record("frame" + delay);
        gb.SetTimeSec(120);
        new GscIntroSequence(delay, GscStrat.GfSkip, GscStrat.TitleSkip, GscStrat.Continue).Execute(gb);
        gb.Execute(path);
        gb.Dispose();
    }

    private static void MakeSave() {
        byte[] save = File.ReadAllBytes("basesaves/gold_r29_" + (Buffered ? "" : "un") + "buffered_pass" + PassNum + ".sav");
        save[0x2045] = 18;
        save[0x2046] = (byte) (Overflow ? 59 : 51);
        save[0x2047] = 0;

        int checksum = 0;
        for(int i = 0x2009; i < 0x2d69; i++) {
            checksum += (save[i] & 0xff);
        }
        save[0x2d69] = (byte) ((checksum) & 0xff);
        save[0x2d6a] = (byte) ((checksum >> 8) & 0xff);

        File.WriteAllBytes("roms/pokegold.sav", save);
    }
}*/