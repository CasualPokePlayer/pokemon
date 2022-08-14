using System;

public partial class GameBoy {

    public void CpuWriteBE<T>(int addr, T data) where T : struct {
        switch(Type.GetTypeCode(typeof(T))) {
            case TypeCode.Byte:
            case TypeCode.SByte:
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (object) data);
                break;
            case TypeCode.Int16:
            case TypeCode.UInt16:
                ushort u16 = (ushort) (object) data;
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (u16 >> 8));
                Lib3dsvc.VC_Poke((ushort) (addr + 1), (byte) (u16 & 0xff));
                break;
            case TypeCode.Int32:
            case TypeCode.UInt32:
                uint u32 = (uint) (object) data;
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (u32 >> 24));
                Lib3dsvc.VC_Poke((ushort) (addr + 1), (byte) (u32 >> 16));
                Lib3dsvc.VC_Poke((ushort) (addr + 2), (byte) (u32 >> 8));
                Lib3dsvc.VC_Poke((ushort) (addr + 3), (byte) (u32 & 0xff));
                break;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                ulong u64 = (ulong) (object) data;
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (u64 >> 56));
                Lib3dsvc.VC_Poke((ushort) (addr + 1), (byte) (u64 >> 48));
                Lib3dsvc.VC_Poke((ushort) (addr + 2), (byte) (u64 >> 40));
                Lib3dsvc.VC_Poke((ushort) (addr + 3), (byte) (u64 >> 32));
                Lib3dsvc.VC_Poke((ushort) (addr + 4), (byte) (u64 >> 24));
                Lib3dsvc.VC_Poke((ushort) (addr + 5), (byte) (u64 >> 16));
                Lib3dsvc.VC_Poke((ushort) (addr + 6), (byte) (u64 >> 8));
                Lib3dsvc.VC_Poke((ushort) (addr + 7), (byte) (u64 & 0xff));
                break;
        }
    }

    public void CpuWriteLE<T>(int addr, T data) where T : struct {
        switch(Type.GetTypeCode(typeof(T))) {
            case TypeCode.Byte:
            case TypeCode.SByte:
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (object) data);
                break;
            case TypeCode.Int16:
            case TypeCode.UInt16:
                ushort u16 = (ushort) (object) data;
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (u16 & 0xff));
                Lib3dsvc.VC_Poke((ushort) (addr + 1), (byte) (u16 >> 8));
                break;
            case TypeCode.Int32:
            case TypeCode.UInt32:
                uint u32 = (uint) (object) data;
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (u32 & 0xff));
                Lib3dsvc.VC_Poke((ushort) (addr + 1), (byte) (u32 >> 8));
                Lib3dsvc.VC_Poke((ushort) (addr + 2), (byte) (u32 >> 16));
                Lib3dsvc.VC_Poke((ushort) (addr + 3), (byte) (u32 >> 24));
                break;
            case TypeCode.Int64:
            case TypeCode.UInt64:
                ulong u64 = (ulong) (object) data;
                Lib3dsvc.VC_Poke((ushort) (addr), (byte) (u64 & 0xff));
                Lib3dsvc.VC_Poke((ushort) (addr + 1), (byte) (u64 >> 8));
                Lib3dsvc.VC_Poke((ushort) (addr + 2), (byte) (u64 >> 16));
                Lib3dsvc.VC_Poke((ushort) (addr + 3), (byte) (u64 >> 24));
                Lib3dsvc.VC_Poke((ushort) (addr + 4), (byte) (u64 >> 32));
                Lib3dsvc.VC_Poke((ushort) (addr + 5), (byte) (u64 >> 40));
                Lib3dsvc.VC_Poke((ushort) (addr + 6), (byte) (u64 >> 48));
                Lib3dsvc.VC_Poke((ushort) (addr + 7), (byte) (u64 >> 56));
                break;
        }
    }

    public void CpuWrite(int addr, byte[] data) {
        for(int i = 0; i < data.Length; i++) {
            CpuWriteBE(addr + i, data[i]);
        }
    }

    public T CpuReadBE<T>(int addr) where T : struct {
        switch(Type.GetTypeCode(typeof(T))) {
            case TypeCode.Byte:
            case TypeCode.SByte:
                return (T) (object) Lib3dsvc.VC_Peek((ushort) (addr));
            case TypeCode.Int16:
            case TypeCode.UInt16:
                return (T) (object) (ushort) ((Lib3dsvc.VC_Peek((ushort) (addr)) << 8) | (Lib3dsvc.VC_Peek((ushort) (addr + 1))));
            case TypeCode.Int32:
            case TypeCode.UInt32:
                return (T) (object) (uint) ((Lib3dsvc.VC_Peek((ushort) (addr)) << 24) | (Lib3dsvc.VC_Peek((ushort) (addr + 1)) << 16) |
                                            (Lib3dsvc.VC_Peek((ushort) (addr + 2)) << 8) | (Lib3dsvc.VC_Peek((ushort) (addr + 3))));
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return (T) (object) (ulong) ((Lib3dsvc.VC_Peek((ushort) (addr)) << 56) | (Lib3dsvc.VC_Peek((ushort) (addr + 1)) << 48) |
                                             (Lib3dsvc.VC_Peek((ushort) (addr + 2)) << 40) | (Lib3dsvc.VC_Peek((ushort) (addr + 3)) << 32) |
                                             (Lib3dsvc.VC_Peek((ushort) (addr + 4)) << 24) | (Lib3dsvc.VC_Peek((ushort) (addr + 5)) << 16) |
                                             (Lib3dsvc.VC_Peek((ushort) (addr + 6)) << 8) | (Lib3dsvc.VC_Peek((ushort) (addr + 7))));
            default: return default;
        }
    }

    public T CpuReadLE<T>(int addr) where T : struct {
        switch(Type.GetTypeCode(typeof(T))) {
            case TypeCode.Byte:
            case TypeCode.SByte:
                return (T) (object) Lib3dsvc.VC_Peek((ushort) (addr));
            case TypeCode.Int16:
            case TypeCode.UInt16:
                return (T) (object) (ushort) ((Lib3dsvc.VC_Peek((ushort) (addr))) | (Lib3dsvc.VC_Peek((ushort) (addr + 1)) << 8));
            case TypeCode.Int32:
            case TypeCode.UInt32:
                return (T) (object) (uint) ((Lib3dsvc.VC_Peek((ushort) (addr))) | (Lib3dsvc.VC_Peek((ushort) (addr + 1)) << 8) |
                                            (Lib3dsvc.VC_Peek((ushort) (addr + 2)) << 16) | (Lib3dsvc.VC_Peek((ushort) (addr + 3)) << 24));
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return (T) (object) (ulong) ((Lib3dsvc.VC_Peek((ushort) (addr))) | (Lib3dsvc.VC_Peek((ushort) (addr + 1)) << 8) |
                                             (Lib3dsvc.VC_Peek((ushort) (addr + 2)) << 16) | (Lib3dsvc.VC_Peek((ushort) (addr + 3)) << 24) |
                                             (Lib3dsvc.VC_Peek((ushort) (addr + 4)) << 32) | (Lib3dsvc.VC_Peek((ushort) (addr + 5)) << 40) |
                                             (Lib3dsvc.VC_Peek((ushort) (addr + 6)) << 48) | (Lib3dsvc.VC_Peek((ushort) (addr + 7)) << 56));
            default: return default;
        }
    }

    public byte[] CpuRead(int addr, int length) {
        byte[] ret = new byte[length];
        for(int i = 0; i < length; i++) {
            ret[i] = CpuReadBE<byte>(addr + i);
        }
        return ret;
    }

    public RAMStream From(int addr) {
        return new RAMStream { GB = this, Position = addr };
    }

    public void CpuWriteBE<T>(string addr, T data) where T : struct {
        CpuWriteBE(SYM[addr], data);
    }

    public void CpuWriteLE<T>(string addr, T data) where T : struct {
        CpuWriteLE(SYM[addr], data);
    }

    public void CpuWrite(string addr, byte[] data) {
        CpuWrite(SYM[addr], data);
    }

    public T CpuReadBE<T>(string addr) where T : struct {
        return CpuReadBE<T>(SYM[addr]);
    }

    public T CpuReadLE<T>(string addr) where T : struct {
        return CpuReadLE<T>(SYM[addr]);
    }

    public byte[] CpuRead(string addr, int length) {
        return CpuRead(SYM[addr], length);
    }

    public RAMStream From(string addr) {
        return From(SYM[addr]);
    }
}

public class RAMStream {

    public GameBoy GB;
    public int Position;

    public byte Peek() {
        return GB.CpuRead(Position);
    }

    public byte u8() {
        return GB.CpuRead(Position++);
    }

    public ushort u16le() {
        ushort ret = GB.CpuReadLE<ushort>(Position);
        Position += 2;
        return ret;
    }

    public ushort u16be() {
        ushort ret = GB.CpuReadBE<ushort>(Position);
        Position += 2;
        return ret;
    }

    public int u24le() {
        return (GB.CpuRead(Position++)) | (GB.CpuRead(Position++) << 8) | (GB.CpuRead(Position++) << 16);
    }

    public int u24be() {
        return (GB.CpuRead(Position++) << 16) | (GB.CpuRead(Position++) << 8) | (GB.CpuRead(Position++));
    }

    public uint u32le() {
        uint ret = GB.CpuReadLE<uint>(Position);
        Position += 4;
        return ret;
    }

    public uint u32be() {
        uint ret = GB.CpuReadBE<uint>(Position);
        Position += 4;
        return ret;
    }

    public ulong u64le() {
        ulong ret = GB.CpuReadLE<ulong>(Position);
        Position += 8;
        return ret;
    }

    public ulong u64be() {
        ulong ret = GB.CpuReadBE<ulong>(Position);
        Position += 8;
        return ret;
    }

    public byte[] Read(int length) {
        byte[] ret = GB.CpuRead(Position, length);
        Position += length;
        return ret;
    }

    public void Seek(int amount) {
        Position += amount;
    }
}