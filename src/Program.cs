using System.IO;
using System.Threading;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

class Program {
    static void Main(string[] args) {
        GoldAbraTAS.StartSearch(1);
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
    }
}