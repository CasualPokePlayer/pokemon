using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class MtSilverTASState
{

    public string Log;
    public GscTile Tile;
    public int EdgeSet;
    public int WastedFrames;
    public byte HRandomAdd;
    public byte HRandomSub;
    public byte RDiv;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Tile.X);
        hash.Add(Tile.Y);
        hash.Add(EdgeSet);
        hash.Add(WastedFrames);
        hash.Add(HRandomAdd);
        hash.Add(HRandomSub);
        hash.Add(RDiv);
        return hash.ToHashCode();
    }
}

// Code heavily plagiarized from: https://github.com/entrpntr/gb-rta-bruteforce/blob/master/src/dabomstew/rta/entei/GSToto.java
public static class MtSilverTAS
{

    const int MaxCost = 18;
    static StreamWriter Writer;
    public static HashSet<int> seenStates = new HashSet<int>();

    public static void OverworldSearch(Gsc gb, MtSilverTASState state)
    {
        if (!seenStates.Add(state.GetHashCode()))
        {
            return;
        }
        byte[] oldState = gb.SaveState();

        foreach (Edge<GscTile> edge in state.Tile.Edges[state.EdgeSet])
        {
            gb.LoadState(oldState);
            if (edge.Cost + state.WastedFrames > MaxCost) continue;

            int ret = gb.Execute(edge.Action);
            if (ret == gb.SYM["DoPlayerMovement.TryStep"] || ret == gb.SYM["DoPlayerMovement.TrySurf"])
            {
                continue;
            }
            if (ret == gb.SYM["TryWildEncounter.no_battle"])
            {
                if (gb.XCoord == 9 && gb.YCoord == 11 && gb.CpuRead("wMapGroup") == 3 && gb.CpuRead("wMapNumber") == 68)
                {
                    {
                        lock (Writer)
                        {
                            var foundPath = $"[{state.WastedFrames} cost] {state.Log}{edge.Action.LogString()}";
                            Writer.WriteLine(foundPath);
                            Writer.Flush();
                            Console.WriteLine(foundPath);
                        }
                    }
                }
                continue;
            }
            OverworldSearch(gb, new MtSilverTASState
            {
                Log = state.Log + edge.Action.LogString() + " ",
                Tile = edge.NextTile,
                EdgeSet = edge.NextEdgeset,
                WastedFrames = state.WastedFrames + edge.Cost,
                HRandomAdd = gb.CpuRead("hRandomAdd"),
                HRandomSub = gb.CpuRead("hRandomSub"),
                RDiv = gb.CpuRead(0xFF04)
            });
            gb.LoadState(oldState);
        }
    }

    public static void StartSearch(int numThreads = 4)
    {
        Gold dummyGb = new Gold();

        GscMap r28 = dummyGb.Maps["Route28"];
        GscMap silverCaveOutside = dummyGb.Maps["SilverCaveOutside"];
        GscMap silverCaveRoom1 = dummyGb.Maps["SilverCaveRoom1"];
		GscMap silverCaveRoom2 = dummyGb.Maps["SilverCaveRoom2"];
		GscMap silverCaveRoom3 = dummyGb.Maps["SilverCaveRoom3"];

        Pathfinding.GenerateEdges(r28, 0, 13, r28.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, r28[33, 6]);
        Pathfinding.GenerateEdges(silverCaveOutside, 18, 11, silverCaveOutside.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveOutside[39, 33]);
        Pathfinding.GenerateEdges(silverCaveRoom1, 3, 2, silverCaveRoom1.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveRoom1[15, 33]);
		Pathfinding.GenerateEdges(silverCaveRoom2, 6, 5, silverCaveRoom2.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveRoom2[19, 31]);
		Pathfinding.GenerateEdges(silverCaveRoom3, 9, 11, silverCaveRoom3.Tileset.LandPermissions, Action.Left | Action.Right | Action.Up | Action.Down, silverCaveRoom3[10, 33]);
		GscTile startTile = r28[33, 6];
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
        Writer = new StreamWriter("mt_silver_tas" + DateTime.Now.Ticks + ".txt");

        for (int threadIndex = 0; threadIndex < numThreads; threadIndex++)
        {
            new Thread(parameter => {
                int index = (int)parameter;
                Gold gb = new Gold(true);
                //gb.Record("gold_test"); 
				gb.SetRTCOffset(-69);
                Console.WriteLine("starting movie");
                gb.PlayBizhawkInputLog("movies/gold.txt");
                Console.WriteLine("finished movie");
				gb.SetSpeedupFlags(0);
				gb.Record("gold_manip_test");
                gb.RunUntil("DoPlayerMovement");
                for (int i = 0; i < index; i++)
                {
                    gb.AdvanceFrame();
                    gb.RunUntil("DoPlayerMovement");
                }

                OverworldSearch(gb, new MtSilverTASState
                {
                    Log = $"thread {index} ",
                    Tile = startTile,
                    WastedFrames = 0,
                    EdgeSet = 0,
                    HRandomAdd = gb.CpuRead("hRandomAdd"),
                    HRandomSub = gb.CpuRead("hRandomSub"),
                    RDiv = gb.CpuRead(0xFF04)
                });
            }).Start(threadIndex);
        }
    }
}