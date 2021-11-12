using System.IO;
using System.Threading;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

class Program
{
    static void Main(string[] args)
    {
        //GoldAbraTAS.StartSearch();
        /*Gold gb = new Gold(true);
        gb.SetRTCOffset(-69);
        gb.PlayBizhawkInputLog("movies/pokegold_00_00.txt");
        gb.Record("lol");
        gb.Hold(Joypad.Left, "OWPlayerInput");
        gb.Execute("L L L U U U U L U L L L L L L L L L L L L L Del D D D D D D D R R D D D D D D D D D D R R D D R D D D D D D D D D D D D L D D L D Del D L D D D L");
        gb.Hold(Joypad.B, "DoBattleTransition.done");
        gb.RunUntil("CalcMonStats");
        int dvs = gb.CpuRead("wEnemyMonDVs") << 8 | gb.CpuRead(gb.SYM["wEnemyMonDVs"] + 1);
        var dvsstring = $"0x{dvs:x4}";
        Console.WriteLine(dvsstring);
        gb.Dispose();*/
        /*Gold gb = new Gold(true);
        gb.SetRTCOffset(-69);
        gb.PlayBizhawkInputLog("movies/pokegold_00.txt");*/
        /*Gold gb = new Gold(true);
        byte[] state = gb.SaveState();
        bool isCgb = state[gb.SaveStateLabels["iscgb"]] == 1;
        byte[] vram = new byte[isCgb ? 0x4000 : 0x2000];
        byte[] sram = new byte[0x8000]; // todo: detect SRAM size?
        byte[] wram = new byte[isCgb ? 0x8000 : 0x2000];
        byte[] hram = new byte[0x200]; // contains OAM, I/O, and HRAM technically
        Buffer.BlockCopy(state, gb.SaveStateLabels["vram"], vram, 0, vram.Length);
        Buffer.BlockCopy(state, gb.SaveStateLabels["sram"], sram, 0, sram.Length);
        Buffer.BlockCopy(state, gb.SaveStateLabels["wram"], wram, 0, wram.Length);
        Buffer.BlockCopy(state, gb.SaveStateLabels["hram"], hram, 0, hram.Length);
        File.WriteAllBytes("vram.bin", vram);
        File.WriteAllBytes("sram.bin", sram);
        File.WriteAllBytes("wram.bin", wram);
        File.WriteAllBytes("hram.bin", hram);
        gb.Dispose();*/
        /*Gold gb = new Gold();
        byte[] state = gb.SaveState();
        Array.Fill<byte>(state, 0, gb.SaveStateLabels["wram"], 0x8000);
        gb.LoadState(state);
        gb.SaveState("test.bin");
        gb.Dispose();*/
        GameBoy gb = new GameBoy("roms/sgb2_bios.bin", "roms/pokegold.gbc");
        gb.ResetSPC();
        byte[] state = gb.SaveState();
        byte[] spcState = new byte[67 * 1024L];
        Buffer.BlockCopy(state, gb.SaveStateLabels["sgbspc"], spcState, 0, spcState.Length);
        File.WriteAllBytes("sgb2_state.bin", spcState);
        gb.Dispose();
    }
}