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
        const int numThreads = 1;
        var timer = new Stopwatch();
        timer.Reset();
        timer.Start();
        for (int threadIndex = 0; threadIndex < numThreads; threadIndex++)
        {
            new Thread(parameter => {
                int index = (int)parameter;
                LoadFlags loadFlags = LoadFlags.ReadOnlySav | LoadFlags.NoBios | LoadFlags.GcbMode;
                string romFile = (index / 2) switch
                {
                    0 => "roms/sml.gb",
                    _ => throw new Exception(),
                };
                GameBoy gb = new GameBoy(null, romFile, SpeedupFlags.None, loadFlags);
                gb.Record("test");
                while (timer.ElapsedMilliseconds < 120000)
                {
                    gb.AdvanceFrame();
                }
                Console.WriteLine("Ran Thread {0} for 2 minutes with {1} samples emulated, with an average framerate of {2} fps, using romfile {3} in {4} mode", index, gb.EmulatedSamples, gb.EmulatedSamples / 35112.0 / 120.0, romFile, (index & 1) == 0 ? "dmg" : "cgb"); 
                gb.Dispose();
            }).Start(threadIndex);
        }
    }
}