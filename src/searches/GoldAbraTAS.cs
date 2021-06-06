using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class GoldAbraTASState
{

    public string Log;
    public GscTile Tile;
    public int EdgeSet;
    public int WastedFrames;
    public Action BlockedActions;
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
        hash.Add(BlockedActions);
        hash.Add(HRandomAdd);
        hash.Add(HRandomSub);
        hash.Add(RDiv);
        return hash.ToHashCode();
    }
}

// Code heavily plagiarized from: https://github.com/entrpntr/gb-rta-bruteforce/blob/master/src/dabomstew/rta/entei/GSToto.java
public static class GoldAbraTAS
{

    const int MaxCost = 0;
    static StreamWriter Writer;
    public static HashSet<int> seenStates = new HashSet<int>();

    public static void OverworldSearch(Gsc gb, GoldAbraTASState state)
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

            if ((state.BlockedActions & edge.Action) > 0) continue;

            int ret = gb.Execute(edge.Action);
            if (ret == gb.SYM["DoPlayerMovement.BumpSound"])
            {
                continue;
            }
            if (ret == gb.SYM["RandomEncounter.ok"])
            {
                gb.Hold(Joypad.B, gb.SYM["CalcMonStats"]);
                if (gb.CpuRead("wEnemyMonSpecies") == gb.Species["ABRA"].Id)
                {
                    lock (Writer)
                    {
                        var foundAbra = $"[{state.WastedFrames} cost] {state.Log}{edge.Action.LogString()}";
                        Writer.WriteLine(foundAbra);
                        Writer.Flush();
                        Console.WriteLine(foundAbra);
                    }
                }
                continue;
            }

            Action blockedActions = state.BlockedActions;

            if ((edge.Action & Action.A) > 0)
                blockedActions |= Action.A;
            else
                blockedActions &= ~(Action.A);

            OverworldSearch(gb, new GoldAbraTASState
            {
                Log = state.Log + edge.Action.LogString() + " ",
                Tile = edge.NextTile,
                EdgeSet = edge.NextEdgeset,
                WastedFrames = state.WastedFrames + edge.Cost,
                BlockedActions = blockedActions,
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
        GscMap route34GateMap = dummyGb.Maps["Route34IlexForestGate"];
        GscMap route34Map = dummyGb.Maps["Route34"];
        Pathfinding.GenerateEdges(route34GateMap, 0, 16, route34GateMap.Tileset.LandPermissions, Action.Up | Action.Right | Action.A, route34GateMap[4, 0]);
        Pathfinding.GenerateEdges(route34GateMap, 0, 16, route34GateMap.Tileset.LandPermissions, Action.Up | Action.Right | Action.A, route34GateMap[5, 0]);
        Pathfinding.GenerateEdges(route34Map, 0, 16, route34Map.Tileset.LandPermissions, Action.Up | Action.Left | Action.Right | Action.A, route34Map[7, 6]);
        GscTile startTile = route34GateMap[4, 7];
        route34GateMap[4, 7].AddEdge(0, new Edge<GscTile>() { Action = Action.Up, NextTile = route34GateMap[4, 6], NextEdgeset = 0, Cost = 0 });
        route34Map[13, 37].AddEdge(0, new Edge<GscTile>() { Action = Action.Up, NextTile = route34Map[13, 36], NextEdgeset = 0, Cost = 0 });
        route34Map[14, 37].AddEdge(0, new Edge<GscTile>() { Action = Action.Up, NextTile = route34Map[14, 36], NextEdgeset = 0, Cost = 0 });
        for (int i = 0; i < 3; i++)
        {
            route34Map[13, 36 - i].RemoveEdge(0, Action.Left);
            route34Map[13, 36 - i].RemoveEdge(0, Action.Left | Action.A);
            route34Map[13, 36 - i].AddEdge(0, new Edge<GscTile>() { Action = Action.Right, NextTile = route34Map[14, 36 - i], NextEdgeset = 0, Cost = 0 });
        }
        for (int i = 0; i < 14; i++)
        {
            route34Map[14, 36 - i].RemoveEdge(0, Action.Right);
            route34Map[14, 36 - i].RemoveEdge(0, Action.Right | Action.A);
        }
        for (int i = 0; i < 10; i++)
        {
            route34Map[14, 36 - i].RemoveEdge(0, Action.Left);
            route34Map[14, 36 - i].RemoveEdge(0, Action.Left | Action.A);
            route34Map[14, 36 - i].AddEdge(0, new Edge<GscTile>() { Action = Action.Up, NextTile = route34Map[14, 35 - i], NextEdgeset = 0, Cost = 0 });
            route34Map[14, 36 - i].AddEdge(0, new Edge<GscTile>() { Action = Action.Up | Action.A, NextTile = route34Map[14, 35 - i], NextEdgeset = 0, Cost = 0 });
        }
        route34GateMap.Sprites.Remove(5, 7);
        Pathfinding.DebugDrawEdges(route34Map, 0);
        dummyGb.Dispose();
        Writer = new StreamWriter("gold_abra_tas" + DateTime.Now.Ticks + ".txt");

        for (int threadIndex = 0; threadIndex < numThreads; threadIndex++)
        {
            new Thread(parameter => {
                int index = (int)parameter;
                Gold gb = new Gold(true);		
                gb.SetRTCOffset(-69);
                Console.WriteLine("starting movie");
                string lcdpattern = "";
                switch (index)
                {
                    case 0:
                        lcdpattern = "00";
                        break;
                    case 1:
                        lcdpattern = "01";
                        break;
                    case 2:
                        lcdpattern = "10";
                        break;
                    case 3:
                        lcdpattern = "11";
                        break;
                    default:
                        break;
                }
                if (lcdpattern == "")
                {
                    throw new ArgumentOutOfRangeException("Too many threads!");
                }
                gb.PlayBizhawkInputLog("movies/pokegold_" + lcdpattern + ".txt");
                Console.WriteLine("finished movie");
                gb.Record("test");
                gb.Hold(Joypad.B, "OWPlayerInput");
                /*for (int i = 0; i < index; i++)
                {
                    gb.AdvanceFrame();
                    gb.Hold(Joypad.B, "OWPlayerInput");
                }*/
                /*var startrng = (gb.CpuRead("hRandomAdd") << 8) | gb.CpuRead("hRandomSub");
                var startrdiv = gb.CpuRead(0xFF04);
                var startrngframe = gb.CpuRead("hVBlankCounter");
                Console.WriteLine($"0x{startrng:x4}");
                Console.WriteLine($"0x{startrdiv:x2}");
                Console.WriteLine($"0x{startrngframe:x2}");*/


                OverworldSearch(gb, new GoldAbraTASState
                {
                    Log = $"thread {index} ",
                    Tile = startTile,
                    WastedFrames = 0,
                    EdgeSet = 0,
                    BlockedActions = Action.A,
                    HRandomAdd = gb.CpuRead("hRandomAdd"),
                    HRandomSub = gb.CpuRead("hRandomSub"),
                    RDiv = gb.CpuRead(0xFF04)
                });
            }).Start(threadIndex);
        }
    }
}