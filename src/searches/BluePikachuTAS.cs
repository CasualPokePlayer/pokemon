using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class BluePikachuTASState
{

    public string Log;
    public RbyTile Tile;
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
public static class BluePikachuTAS
{

    const int MaxCost = 2;
    static StreamWriter Writer;
    public static HashSet<int> seenStates = new HashSet<int>();

    public static void OverworldSearch(Rby gb, BluePikachuTASState state)
    {
        if (!seenStates.Add(state.GetHashCode()))
        {
            return;
        }
        byte[] oldState = gb.SaveState();

        foreach (Edge<RbyTile> edge in state.Tile.Edges[state.EdgeSet])
        {
            gb.LoadState(oldState);
            if (edge.Cost + state.WastedFrames > MaxCost) continue;

            int ret = gb.Execute(edge.Action);
            if (ret == gb.SYM["CollisionCheckOnLand.collision"] || ret == gb.SYM["CollisionCheckOnWater.collision"])
            {
                continue;
            }
            if (ret == gb.SYM["CalcStats"])
            {
                if (gb.XCoord == 1 && gb.YCoord == 18 && gb.CpuRead("wEnemyMonSpecies") == gb.Species["PIKACHU"].Id && gb.CpuRead("wEnemyMonLevel") == 5)
                {
                    int dvs = gb.CpuRead("wEnemyMonDVs") << 8 | gb.CpuRead(gb.SYM["wEnemyMonDVs"] + 1);

                    int hp = (((dvs >> 9) & 8) | ((dvs >> 6) & 4) | ((dvs >> 3) & 2) | (dvs & 1)) & 0xf;
                    int atk = (dvs >> 12) & 0xf;
                    int def = (dvs >> 8) & 0xf;
                    int spd = (dvs >> 4) & 0xf;
                    int spc = dvs & 0xf;

                    {
                        lock (Writer)
                        {
                            var foundPikachu = $"[{state.WastedFrames} cost] {state.Log}{edge.Action.LogString()} - 0x{dvs:x4}";
                            Writer.WriteLine(foundPikachu);
                            Writer.Flush();
                            Console.WriteLine(foundPikachu);
                        }
                    }
                }
                continue;
            }
            OverworldSearch(gb, new BluePikachuTASState
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
        Blue dummyGb = new Blue();

        RbyMap viridianCityMap = dummyGb.Maps[1];
        RbyMap route2map = dummyGb.Maps[13];
        RbyMap gatehouseMap = dummyGb.Maps[50];
        RbyMap viridianForestMap = dummyGb.Maps[51];
        Pathfinding.GenerateEdges(viridianCityMap, 0, 17, viridianCityMap.Tileset.LandPermissions, Action.Up | Action.Left | Action.A, viridianCityMap[17, 0]);
        Pathfinding.GenerateEdges(route2map, 0, 17, route2map.Tileset.LandPermissions, Action.Up | Action.Left | Action.Right | Action.A, route2map[3, 44]);
        Pathfinding.GenerateEdges(gatehouseMap, 0, 17, gatehouseMap.Tileset.LandPermissions, Action.Right | Action.Up | Action.A, gatehouseMap[5, 0]);
        Pathfinding.GenerateEdges(viridianForestMap, 0, 17, viridianForestMap.Tileset.LandPermissions, Action.Right | Action.Down | Action.Up | Action.Left | Action.A, viridianForestMap[1, 18]);
        RbyTile startTile = viridianCityMap[23, 26];
        viridianCityMap[17, 0].AddEdge(0, new Edge<RbyTile>() { Action = Action.Up, NextTile = route2map[7, 71], NextEdgeset = 0, Cost = 0 });
        viridianCityMap[18, 0].AddEdge(0, new Edge<RbyTile>() { Action = Action.Up, NextTile = route2map[8, 71], NextEdgeset = 0, Cost = 0 });
        route2map[3, 44].AddEdge(0, new Edge<RbyTile>() { Action = Action.Up, NextTile = gatehouseMap[4, 7], NextEdgeset = 0, Cost = 0 });
        gatehouseMap[4, 7].AddEdge(0, new Edge<RbyTile>() { Action = Action.Up, NextTile = gatehouseMap[4, 6], NextEdgeset = 0, Cost = 0 });
        gatehouseMap[5, 1].AddEdge(0, new Edge<RbyTile>() { Action = Action.Up, NextTile = viridianForestMap[17, 47], NextEdgeset = 0, Cost = 0 });
        viridianForestMap[17, 47].AddEdge(0, new Edge<RbyTile>() { Action = Action.Up, NextTile = viridianForestMap[17, 46], NextEdgeset = 0, Cost = 0 });
        viridianForestMap[25, 12].RemoveEdge(0, Action.A);
        viridianForestMap[25, 12].RemoveEdge(0, Action.Up);
        viridianForestMap[26, 12].RemoveEdge(0, Action.Left);
        viridianForestMap[26, 11].RemoveEdge(0, Action.Left);
        viridianForestMap[25, 12].GetEdge(0, Action.Right).Cost = 0;
        viridianForestMap[1, 19].RemoveEdge(0, Action.A);
        viridianForestMap[1, 19].GetEdge(0, Action.Up).Cost = 0;
        Writer = new StreamWriter("blue_pikachu_tas" + DateTime.Now.Ticks + ".txt");

        for (int threadIndex = 0; threadIndex < numThreads; threadIndex++)
        {
            new Thread(parameter => {
                int index = (int)parameter;
                Blue gb = new Blue();
                gb.SetSpeedupFlags(SpeedupFlags.All);
                Console.WriteLine("starting movie");
                gb.PlayBizhawkInputLog("movies/blue.txt");
                Console.WriteLine("finished movie");
                gb.RunUntil("JoypadOverworld");
                for (int i = 0; i < index; i++)
                {
                    gb.AdvanceFrame();
                    gb.RunUntil("JoypadOverworld");
                }

                OverworldSearch(gb, new BluePikachuTASState
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