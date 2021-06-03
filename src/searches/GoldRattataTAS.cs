using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class GoldRattataTASState
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
public static class GoldRattataTAS
{

    const int MaxCost = 50;
    static StreamWriter Writer;
    public static HashSet<int> seenStates = new HashSet<int>();

    public static void OverworldSearch(Gsc gb, GoldRattataTASState state)
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
            if (ret == gb.SYM["DoPlayerMovement.BumpSound"])
            {
                continue;
            }
            if (ret == gb.SYM["RandomEncounter.ok"])
            {
                gb.RunUntil(gb.SYM["CalcMonStats"]);
                //if (gb.CpuRead("wEnemyMonSpecies") == gb.Species["RATTATA"].Id && gb.CpuRead("wEnemyMonLevel") == 8)
                {
                    int dvs = gb.CpuRead("wEnemyMonDVs") << 8 | gb.CpuRead(gb.SYM["wEnemyMonDVs"] + 1);

                    //int hp = (((dvs >> 9) & 8) | ((dvs >> 6) & 4) | ((dvs >> 3) & 2) | (dvs & 1)) & 0xf;
                    int atk = (dvs >> 12) & 0xf;
                    int def = (dvs >> 8) & 0xf;
                    int spd = (dvs >> 4) & 0xf;
                    int spc = dvs & 0xf;

					//if (atk == 12 && (def == 11 || def == 15) && spd >= 12 && (spc == 11 || spc == 15))
                    {
                        lock (Writer)
                        {
                            var foundRattata = $"[{state.WastedFrames} cost] {state.Log}{edge.Action.LogString()} - 0x{dvs:x4}";
                            Writer.WriteLine(foundRattata);
                            Writer.Flush();
                            Console.WriteLine(foundRattata);
                        }
                    }
                }
                continue;
            }
            OverworldSearch(gb, new GoldRattataTASState
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

        GscMap violetCityMap = dummyGb.Maps["VioletCity"];
        GscMap route32map = dummyGb.Maps["Route32"];
        Pathfinding.GenerateEdges(violetCityMap, 0, 16, violetCityMap.Tileset.LandPermissions, Action.Delay | Action.Down | Action.Up | Action.Left | Action.A, violetCityMap[14, 35]);
        Pathfinding.GenerateEdges(violetCityMap, 0, 16, violetCityMap.Tileset.LandPermissions, Action.Delay | Action.Down | Action.Up | Action.Left | Action.A, violetCityMap[15, 35]);
        Pathfinding.GenerateEdges(route32map, 0, 16, route32map.Tileset.LandPermissions, Action.Delay | Action.Down | Action.Left | Action.Right | Action.A, route32map[14, 23]);
        GscTile startTile = violetCityMap[30, 26];
        violetCityMap[14, 35].AddEdge(0, new Edge<GscTile>() { Action = Action.Down, NextTile = route32map[14, 0], NextEdgeset = 0, Cost = 0 });
        violetCityMap[15, 35].AddEdge(0, new Edge<GscTile>() { Action = Action.Down, NextTile = route32map[15, 0], NextEdgeset = 0, Cost = 0 });
        route32map[15, 20].RemoveEdge(0, Action.Left);
		route32map[15, 21].RemoveEdge(0, Action.Left);
		route32map[15, 22].RemoveEdge(0, Action.Left);
        Writer = new StreamWriter("gold_rattata_tas" + DateTime.Now.Ticks + ".txt");

        for (int threadIndex = 0; threadIndex < numThreads; threadIndex++)
        {
            new Thread(parameter => {
                int index = (int)parameter;
                Gold gb = new Gold(true);		
                Console.WriteLine("starting movie");
                gb.PlayBizhawkInputLog("movies/pokegold.txt");
                Console.WriteLine("finished movie");
                gb.RunUntil("OWPlayerInput");
                for (int i = 0; i < index; i++)
                {
                    gb.AdvanceFrame();
                    gb.RunUntil("OWPlayerInput");
                }

                OverworldSearch(gb, new GoldRattataTASState
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