namespace NormalNBT.BEutils;


/// <summary>
/// Reads primitive types from sequence of bytes
/// readFunc call should return next byte
/// </summary>
public static class BigEndianReader
{
    public static byte ReadByte(Func<byte> readFunc)
    {
        return readFunc();
    }
    public static sbyte ReadSByte(Func<byte> readFunc)
    {
        return unchecked((sbyte)readFunc());
    }
    public static ushort ReadUshort(Func<byte> readFunc)
    {
        return unchecked((ushort)((readFunc() << 8) | readFunc()));
    }
    public static short ReadShort(Func<byte> readFunc)
    {
        return unchecked((short)ReadUshort(readFunc));
    }
    public static uint ReadUInt(Func<byte> readFunc)
    {
        return unchecked((uint) (
            (readFunc() << 24) | 
            (readFunc() << 16) | 
            (readFunc() << 8)  | 
             readFunc()
        ));
    }
    public static int ReadInt(Func<byte> readFunc)
    {
        return unchecked((int)ReadUInt(readFunc));
    }
    public static long ReadLong(Func<byte> readFunc)
    {
        return
            ((long)readFunc() << 56) |
            ((long)readFunc() << 48) |
            ((long)readFunc() << 40) |
            ((long)readFunc() << 32) |
            ((long)readFunc() << 24) |
            ((long)readFunc() << 16) |
            ((long)readFunc() << 8)  |
                   readFunc();
    }
    public static ulong ReadULong(Func<byte> readFunc)
    {
        return unchecked((ulong)ReadLong(readFunc));
    }
    public static float ReadFloat(Func<byte> readFunc)
    {
        return BitConverter.Int32BitsToSingle(ReadInt(readFunc));
    }
    public static double ReadDouble(Func<byte> readFunc)
    {
        return BitConverter.Int64BitsToDouble(ReadLong(readFunc));
    }
}

/// <summary>
/// Writes primitive types to sequence of bytes
/// writeFunc call - next byte
/// </summary>
public static class BigEndianWriter
{
    public static void WriteByte(Action<byte> writeFunc, byte value)
    {
        writeFunc(value);
    }
    public static void WriteSByte(Action<byte> writeFunc, sbyte value)
    {
        writeFunc(unchecked((byte)value));
    }
    public static void WriteUShort(Action<byte> writeFunc, ushort value)
    {
        unchecked
        {
            writeFunc((byte)((value & 0x_FF_00) >> 8));
            writeFunc((byte) (value & 0x_00_FF));
        }
    }
    public static void WriteShort(Action<byte> writeFunc, short value)
    {
        WriteUShort(writeFunc, unchecked((ushort)value));
    }
    public static void WriteInt(Action<byte> writeFunc, int value)
    {
        unchecked
        {
            writeFunc((byte)(((uint)value & 0x_FF_00_00_00) >> 24));
            writeFunc((byte)(((uint)value & 0x_00_FF_00_00) >> 16));
            writeFunc((byte)(((uint)value & 0x_00_00_FF_00) >> 8));
            writeFunc((byte) ((uint)value & 0x_00_00_00_FF));
        }
    }
    public static void WriteUInt(Action<byte> writeFunc, uint value)
    {
        WriteInt(writeFunc, unchecked((int)value));
    }
    public static void WriteLong(Action<byte> writeFunc, long value)
    {
        unchecked
        {
            writeFunc((byte)(((ulong)value & 0x_FF_00_00_00_00_00_00_00) >> 56));
            writeFunc((byte)(((ulong)value & 0x_00_FF_00_00_00_00_00_00) >> 48));
            writeFunc((byte)(((ulong)value & 0x_00_00_FF_00_00_00_00_00) >> 40));
            writeFunc((byte)(((ulong)value & 0x_00_00_00_FF_00_00_00_00) >> 32));
            writeFunc((byte)(((ulong)value & 0x_00_00_00_00_FF_00_00_00) >> 24));
            writeFunc((byte)(((ulong)value & 0x_00_00_00_00_00_FF_00_00) >> 16));
            writeFunc((byte)(((ulong)value & 0x_00_00_00_00_00_00_FF_00) >> 8));
            writeFunc((byte) ((ulong)value & 0x_00_00_00_00_00_00_00_FF));
        }
    }
    public static void WriteULong(Action<byte> writeFunc, ulong value)
    {
        WriteLong(writeFunc, unchecked((long)value));
    }
    public static void WriteFloat(Action<byte> writeFunc, float value)
    {
        WriteInt(writeFunc, BitConverter.SingleToInt32Bits(value));
    }
    public static void WriteDouble(Action<byte> writeFunc, double value)
    {
        WriteLong(writeFunc, BitConverter.DoubleToInt64Bits(value));
    }
}