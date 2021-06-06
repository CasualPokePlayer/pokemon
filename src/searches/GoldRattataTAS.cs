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
public static class GoldRattataTAS
{

    const int MaxCost = 4;
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

            if ((state.BlockedActions & edge.Action) > 0) continue;

            int ret = gb.Execute(edge.Action);
            if (ret == gb.SYM["DoPlayerMovement.BumpSound"])
            {
                continue;
            }
            if (ret == gb.SYM["RandomEncounter.ok"])
            {
                /*var prerng = (gb.CpuRead("hRandomAdd") << 8) | gb.CpuRead("hRandomSub");
                var prerdiv = gb.CpuRead(0xFF04);
                var prerngframe = gb.CpuRead("hVBlankCounter");*/
                gb.Hold(Joypad.B, gb.SYM["CalcMonStats"]);
                if (gb.CpuRead("wEnemyMonSpecies") == gb.Species["RATTATA"].Id && gb.CpuRead("wEnemyMonLevel") == 8)
                {
                    /*var postrng = (gb.CpuRead("hRandomAdd") << 8) | gb.CpuRead("hRandomSub");
                    var postrdiv = gb.CpuRead(0xFF04);
                    var postrngframe = gb.CpuRead("hVBlankCounter");*/

                    int dvs = gb.CpuRead("wEnemyMonDVs") << 8 | gb.CpuRead(gb.SYM["wEnemyMonDVs"] + 1);

                    //int hp = (((dvs >> 9) & 8) | ((dvs >> 6) & 4) | ((dvs >> 3) & 2) | (dvs & 1)) & 0xf;
                    int atk = (dvs >> 12) & 0xf;
                    int def = (dvs >> 8) & 0xf;
                    int spd = (dvs >> 4) & 0xf;
                    int spc = dvs & 0xf;

                    var statcheck = "";
                    if (atk == 12 && (def == 11 || def == 15) && spd >= 12 && (spc >= 8 || (spc & 3) >= 2))
                    {
                        statcheck = " !";
                    }

                    {
                        lock (Writer)
                        {
                            var foundRattata = $"[{state.WastedFrames} cost] {state.Log}{edge.Action.LogString()} - 0x{dvs:x4}" + statcheck;
                            Writer.WriteLine(foundRattata);
                            Writer.Flush();
                            Console.WriteLine(foundRattata);
                        }
                    }
                }
                continue;
            }

            Action blockedActions = state.BlockedActions;

            if ((edge.Action & Action.A) > 0)
                blockedActions |= Action.A;
            else
                blockedActions &= ~(Action.A);

            OverworldSearch(gb, new GoldRattataTASState
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
        GscMap violetCityMap = dummyGb.Maps["VioletCity"];
        GscMap route32map = dummyGb.Maps["Route32"];
        //Pathfinding.GenerateEdges(violetCityMap, 0, 16, violetCityMap.Tileset.LandPermissions, Action.Delay | Action.Down | Action.Up | Action.Left | Action.Right | Action.A, violetCityMap[14, 35]);
        Pathfinding.GenerateEdges(violetCityMap, 0, 16, violetCityMap.Tileset.LandPermissions, Action.Delay | Action.Down | Action.Up | Action.Left | Action.Right | Action.A, violetCityMap[15, 35]);
        Pathfinding.GenerateEdges(route32map, 0, 16, route32map.Tileset.LandPermissions, Action.Delay | Action.Down | Action.Left | Action.Right | Action.A, route32map[14, 23]);
        GscTile startTile = violetCityMap[30, 26];
        violetCityMap[14, 35].AddEdge(0, new Edge<GscTile>() { Action = Action.Down, NextTile = route32map[14, 0], NextEdgeset = 0, Cost = 0 });
        violetCityMap[15, 35].AddEdge(0, new Edge<GscTile>() { Action = Action.Down, NextTile = route32map[15, 0], NextEdgeset = 0, Cost = 0 });
        route32map[15, 20].RemoveEdge(0, Action.Left);
        route32map[15, 20].RemoveEdge(0, Action.Left | Action.A);
        route32map[15, 21].RemoveEdge(0, Action.Left);
        route32map[15, 21].RemoveEdge(0, Action.Left | Action.A);
        route32map[15, 22].RemoveEdge(0, Action.Left);
        route32map[15, 22].RemoveEdge(0, Action.Left | Action.A);
        dummyGb.Dispose();
        Writer = new StreamWriter("gold_rattata_tas" + DateTime.Now.Ticks + ".txt");

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
                        lcdpattern = "00_00";
                        break;
                    case 1:
                        lcdpattern = "00_11";
                        break;
                    case 2:
                        lcdpattern = "11_00";
                        break;
                    case 3:
                        lcdpattern = "11_11";
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
                gb.Hold(Joypad.B, "OWPlayerInput");
                /*for (int i = 0; i < index; i++)
                {
                    gb.AdvanceFrame();
                    gb.Hold(Joypad.B, "OWPlayerInput");
                }*/
                //gb.Record("test");
                /*var startrng = (gb.CpuRead("hRandomAdd") << 8) | gb.CpuRead("hRandomSub");
                var startrdiv = gb.CpuRead(0xFF04);
                var startrngframe = gb.CpuRead("hVBlankCounter");
                Console.WriteLine($"0x{startrng:x4}");
                Console.WriteLine($"0x{startrdiv:x2}");
                Console.WriteLine($"0x{startrngframe:x2}");*/


                OverworldSearch(gb, new GoldRattataTASState
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