using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

public class BlueSpearowTASState {

    public string Log;
    public RbyTile Tile;
    public int EdgeSet;
    public int WastedFrames;
    public byte HRandomAdd;
    public byte HRandomSub;
    public byte RDiv;

    public override int GetHashCode() {
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
public static class BlueSpearowTAS {

    const int MaxCost = 7;
    static StreamWriter Writer;
    public static HashSet<int> seenStates = new HashSet<int>();

    public static void OverworldSearch(Rby gb, BlueSpearowTASState state) {
        if (!seenStates.Add(state.GetHashCode())) {
            return;
        }
        byte[] oldState = gb.SaveState();

        foreach(Edge<RbyTile> edge in state.Tile.Edges[state.EdgeSet]) {
            gb.LoadState(oldState);
            if (edge.Cost + state.WastedFrames > MaxCost) continue;

            int ret = gb.Execute(edge.Action);
            if (ret == gb.SYM["CollisionCheckOnLand.collision"] || ret == gb.SYM["CollisionCheckOnWater.collision"]) {
                continue;
            }
            if (ret == gb.SYM["CalcStats"])
            {
                if (gb.CpuRead("wEnemyMonSpecies") == gb.Species["SPEAROW"].Id && gb.CpuRead("wEnemyMonLevel") == 5) {
                    int dvs = gb.CpuRead("wEnemyMonDVs") << 8 | gb.CpuRead(gb.SYM["wEnemyMonDVs"] + 1);

                    int hp = (((dvs >> 9) & 8) | ((dvs >> 6) & 4) | ((dvs >> 3) & 2) | (dvs & 1)) & 0xf;
                    int atk = (dvs >> 12) & 0xf;
                    int def = (dvs >> 8) & 0xf;
                    int spd = (dvs >> 4) & 0xf;
                    int spc = dvs & 0xf;

                    if (hp < 10 && atk > 9 && def < 10 && spd > 13)
                    {
                        lock (Writer) {
                            var foundSpearow = $"[{state.WastedFrames} cost] {state.Log}{edge.Action.LogString()} - 0x{dvs:x4}";
                            Writer.WriteLine(foundSpearow);
                            Writer.Flush();
                            Console.WriteLine(foundSpearow);
                        }
                    }
                }
                continue;
            }
            OverworldSearch(gb, new BlueSpearowTASState {
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

    public static void StartSearch(int numThreads = 4) {
        Blue dummyGb = new Blue();
        
        RbyMap viridianCityMap = dummyGb.Maps[1];
        RbyMap route22Map = dummyGb.Maps[33];
        Pathfinding.GenerateEdges(viridianCityMap, 0, 17, viridianCityMap.Tileset.LandPermissions, Action.Left | Action.Up | Action.A, viridianCityMap[0, 17]);
        Pathfinding.GenerateEdges(route22Map, 0, 17, route22Map.Tileset.LandPermissions, Action.Up | Action.Down | Action.Left | Action.A, route22Map[33, 11]);
        RbyTile startTile = viridianCityMap[29, 20];
        viridianCityMap[0, 17].AddEdge(0, new Edge<RbyTile>() { Action = Action.Left, NextTile = route22Map[39, 9], NextEdgeset = 0, Cost = 0 });
        Writer = new StreamWriter("blue_spearow_tas" + DateTime.Now.Ticks + ".txt");
        
        for (int threadIndex = 0; threadIndex < numThreads; threadIndex++) {
            new Thread(parameter => {
                int index = (int)parameter;
                Blue gb = new Blue();
                //gb.Record("test");
                Console.WriteLine("starting movie");
                gb.PlayBizhawkInputLog("movies/blue.txt");
                Console.WriteLine("finished movie");
                gb.RunUntil("JoypadOverworld");
                for (int i = 0; i < index; i++) {
                    gb.AdvanceFrame();
                    gb.RunUntil("JoypadOverworld");
                }
                gb.SetSpeedupFlags(SpeedupFlags.NoSound | SpeedupFlags.NoVideo);

                OverworldSearch(gb, new BlueSpearowTASState {
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