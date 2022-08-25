using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public enum LoadFlags : int {

    GcbMode = 0x01,          // Treat the ROM as having CGB support regardless of what its header advertises.
    GbaFlag = 0x02,          // Use GBA initial CPU register values in CGB mode.
    MultiCartCompat = 0x04,  // Use heuristics to detect and support multicart MBCs disguised as MBC1.
    SgbMode = 0x08,          // Treat the ROM as having SGB support regardless of what its header advertises.
    ReadOnlySav = 0x10,      // Prevent implicit saveSavedata calls for the ROM.
    NoBios = 0x20,           // Use heuristics to boot without a BIOS.
}

public struct Registers {

    public int PC;
    public int SP;
    public int AF;
    public int BC;
    public int DE;
    public int HL;
}

public enum SpeedupFlags : uint {

    None = 0x00,
    NoSound = 0x01,   // Skip generating sound samples.
    NoPPUCall = 0x02, // Skip PPU calls. (breaks LCD interrupt)
    NoVideo = 0x04,   // Skip writing to the video buffer.
    All = 0xffffffff,
}

public enum Joypad : byte {

    None = 0x0,
    A = 0x1,
    B = 0x2,
    Select = 0x4,
    Start = 0x8,
    Right = 0x10,
    Left = 0x20,
    Up = 0x40,
    Down = 0x80,
    All = 0xff,
}

public partial class GameBoy : IDisposable {

    public const int SamplesPerFrame = 35112;

    public Lib3dsvc VC;
    public byte[] VideoBuffer; // Although shared library outputs in the ARGB little-endian format, the data is interpreted as big-endian making it effectively BGRA.
    public byte[] AudioBuffer;
    public Joypad CurrentJoypad;
    public int BufferSamples;
    public int StateSize;

    public ROM ROM;
    public SYM SYM;

    public Scene Scene;
    public ulong EmulatedSamples;

    // Get Reg and flag values.
    public Registers Registers {
        get { VC.GetRegs(out Registers regs); return regs; }
    }

    public GameBoy(string romFile) {
        VC = new Lib3dsvc();
        ROM = new ROM(romFile);
        Debug.Assert(ROM.HeaderChecksumMatches(), "Cartridge header checksum mismatch!");

        var rombuf = File.ReadAllBytes(romFile);
        VC.Init(rombuf, rombuf.Length);

        VideoBuffer = new byte[160 * 144 * 4];
        AudioBuffer = new byte[(SamplesPerFrame + 2064) * 2 * 2]; // Stereo 16-bit samples

        string symPath = "symfiles/" + Path.GetFileNameWithoutExtension(romFile) + ".sym";
        if(File.Exists(symPath)) {
            SYM = new SYM(symPath);
            ROM.Symbols = SYM;
        }

        StateSize = VC.StateLength();
    }

    public void Dispose() {
        if(Scene != null) Scene.Dispose();
        VC.Deinit();
        VC.Dispose();
    }

    public void HardReset() {
        VC.Reset();
        BufferSamples = 0;
    }

    // Emulates 'runsamples' number of samples, or until a video frame has to be drawn. (1 sample = 2 cpu cycles)
    public int RunFor() {
        int ret = VC.RunFrame(CurrentJoypad, VideoBuffer, AudioBuffer, out var runsamples);

        int outsamples = BufferSamples + runsamples;
        BufferSamples += runsamples;
        BufferSamples -= outsamples;
        EmulatedSamples += (ulong)runsamples;

        if(Scene != null) {
            Scene.OnAudioReady(outsamples);
            // returns a negative value if a video frame needs to be drawn.
            if(ret < 0) {
                Scene.Begin();
                Scene.Render();
                Scene.End();
            }
        }

        return ret;
    }

    // Emulates until the next video frame has to be drawn. Returns the hit address.
    public int AdvanceFrame(Joypad joypad = Joypad.None) {
        CurrentJoypad = joypad;
        int hitaddress = RunFor();
        CurrentJoypad = Joypad.None;
        return hitaddress;
    }

    public void AdvanceFrames(int amount, Joypad joypad = Joypad.None) {
        for(int i = 0; i < amount; i++) AdvanceFrame(joypad);
    }

    // Emulates while holding the specified input until the program counter hits one of the specified breakpoints.
    public unsafe int Hold(Joypad joypad, params int[] addrs) {
        fixed(int* addrPtr = addrs) { // Note: Not fixing the pointer causes an AccessValidationException.
            VC.SetInterruptAddresses(addrPtr, addrs.Length);
            int hitaddress;
            do {
                hitaddress = AdvanceFrame(joypad);
            } while(Array.IndexOf(addrs, hitaddress) == -1);
            VC.SetInterruptAddresses(null, 0);
            return hitaddress;
        }
    }

    // Helper function that emulates with no joypad held.
    public int RunUntil(params int[] addrs) {
        return Hold(Joypad.None, addrs);
    }

    // Writes one byte of data to the CPU bus.
    public void CpuWrite(int addr, byte data) {
        VC.Poke((ushort)addr, data);
    }

    // Reads one byte of data from the CPU bus.
    public byte CpuRead(int addr) {
        return VC.Peek((ushort)addr);
    }

    // Returns the emulator state as a buffer.
    public byte[] SaveState() {
        byte[] state = new byte[StateSize];
        VC.SaveStateBinary(state, StateSize);
        return state;
    }

    // Helper function that writes the buffer directly to disk.
    public void SaveState(string file) {
        File.WriteAllBytes(file, SaveState());
    }

    // Loads the emulator state given by a buffer.
    public void LoadState(byte[] buffer) {
        VC.LoadStateBinary(buffer, buffer.Length);
    }

    // Helper function that reads the buffer directly from disk.
    public void LoadState(string file) {
        LoadState(File.ReadAllBytes(file));
    }

    // Helper functions that translate SYM labels to their respective addresses.
    public int RunUntil(params string[] addrs) {
        return RunUntil(Array.ConvertAll(addrs, e => SYM[e]));
    }

    public int Hold(Joypad joypad, params string[] addrs) {
        return Hold(joypad, Array.ConvertAll(addrs, e => SYM[e]));
    }

    public void CpuWrite(string addr, byte data) {
        CpuWrite(SYM[addr], data);
    }

    public byte CpuRead(string addr) {
        return CpuRead(SYM[addr]);
    }

    // Helper function that creates a basic scene graph with a video buffer component.
    public void Show() {
        Scene s = new Scene(this, 160, 144);
        s.AddComponent(new VideoBufferComponent(0, 0, 160, 144));
    }

    // Helper function that creates a basic scene graph with a video buffer component and a record component.
    public void Record(string movie) {
        Show();
        Scene.AddComponent(new RecordingComponent(movie));
    }

    public void PlayBizhawkMovie(string bk2File) {
        using(FileStream bk2Stream = File.OpenRead(bk2File))
        using(ZipArchive zip = new ZipArchive(bk2Stream, ZipArchiveMode.Read))
        using(StreamReader bk2Reader = new StreamReader(zip.GetEntry("Input Log.txt").Open())) {
            PlayBizhawkInputLog(bk2Reader.ReadToEnd().Split('\n'));
        }
    }

    public void PlayBizhawkInputLog(string fileName) {
        PlayBizhawkInputLog(File.ReadAllLines(fileName));
    }

    public void PlayBizhawkInputLog(string[] lines) {
        Joypad[] joypadFlags = { Joypad.Up, Joypad.Down, Joypad.Left, Joypad.Right, Joypad.Start, Joypad.Select, Joypad.B, Joypad.A };
        lines = lines.Subarray(2, lines.Length - 3);
        for(int i = 0; i < lines.Length; i++) {
            if(lines[i][9] != '.') {
                HardReset();
            }
            Joypad joypad = Joypad.None;
            for(int j = 0; j < joypadFlags.Length; j++) {
                if(lines[i][j + 1] != '.') {
                    joypad |= joypadFlags[j];
                }
            }
            AdvanceFrame(joypad);
        }
    }

    public Bitmap Screenshot() {
        Bitmap bitmap;
        if(Scene == null) {
            bitmap = new Bitmap(160, 144, VideoBuffer);
            bitmap.RemapRedAndBlueChannels();
        } else {
            bitmap = new Bitmap(Scene.Window.Width, Scene.Window.Height);
            Renderer.ReadBuffer(bitmap.Pixels);
        }
        return bitmap;
    }
}

public static class Kernel32Imports
{
    [DllImport("kernel32.dll")]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string dllToLoad);
}

public class Lib3dsvc : IDisposable
{
    private string _tempDllPath;
    private IntPtr HModule { get; set; }

    public Lib3dsvc()
    {
        _tempDllPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "-" + dll);
        File.Copy(dll, _tempDllPath, true);
        HModule = Kernel32Imports.LoadLibrary(_tempDllPath);
        if (HModule == IntPtr.Zero)
        {
            Dispose();
            throw new Exception("Could not load dll, " +
                $"error code: {Kernel32Imports.GetLastError()}");
        }

        foreach (var fi in GetType().GetFields())
        {
            if (typeof(Delegate).IsAssignableFrom(fi.FieldType))
            {
                var fp = Kernel32Imports.GetProcAddress(HModule, fi.FieldType.Name);
                if (fp == IntPtr.Zero)
                {
                    Dispose();
                    throw new Exception($"Could not get proc {fi.FieldType.Name}, " +
                        $"error code: {Kernel32Imports.GetLastError()}");
                }
                
                fi.SetValue(this, Marshal.GetDelegateForFunctionPointer(fp, fi.FieldType));
            }
        }
    }

    public void Dispose()
    {
        if (HModule != IntPtr.Zero)
        {
            Kernel32Imports.FreeLibrary(HModule);
            HModule = IntPtr.Zero;
        }

        if (!string.IsNullOrEmpty(_tempDllPath) && File.Exists(_tempDllPath))
        {
            File.Delete(_tempDllPath);
        }

        _tempDllPath = null;
    }

    private const string dll = "lib3dsvc.dll";
    private const CallingConvention cc = CallingConvention.Cdecl;

    public readonly VC_Init Init;
    public readonly VC_Deinit Deinit;
    public readonly VC_RunFrame RunFrame;
    public readonly VC_Reset Reset;
    public readonly VC_StateLength StateLength;
    public readonly VC_SaveStateBinary SaveStateBinary;
    public readonly VC_LoadStateBinary LoadStateBinary;
    public readonly VC_Peek Peek;
    public readonly VC_Poke Poke;
    public readonly VC_GetRegs GetRegs;
    public readonly VC_SetInterruptAddresses SetInterruptAddresses;

    [UnmanagedFunctionPointer(cc)]
    public delegate void VC_Init(byte[] rom, int sz);

    [UnmanagedFunctionPointer(cc)]
    public delegate void VC_Deinit();

    [UnmanagedFunctionPointer(cc)]
    public delegate int VC_RunFrame(Joypad input, byte[] videoBuf, byte[] audioBuf, out int samples);

    [UnmanagedFunctionPointer(cc)]
    public delegate void VC_Reset();

    [UnmanagedFunctionPointer(cc)]
    public delegate int VC_StateLength();

    [UnmanagedFunctionPointer(cc)]
    public delegate bool VC_SaveStateBinary(byte[] stateBuf, int size);

    [UnmanagedFunctionPointer(cc)]
    public delegate bool VC_LoadStateBinary(byte[] stateBuf, int size);

    [UnmanagedFunctionPointer(cc)]
    public delegate byte VC_Peek(ushort addr);

    [UnmanagedFunctionPointer(cc)]
    public delegate void VC_Poke(ushort addr, byte value);

    [UnmanagedFunctionPointer(cc)]
    public delegate void VC_GetRegs(out Registers regs);

    [UnmanagedFunctionPointer(cc)]
    public unsafe delegate void VC_SetInterruptAddresses(int* addrs, int numAddrs);
}