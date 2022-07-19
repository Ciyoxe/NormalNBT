using System.Text;
using NormalNBT.BEutils;

namespace NormalNBT;

public enum TagId
{
    End         = 0,
    Byte        = 1,
    Short       = 2,
    Int         = 3,
    Long        = 4,
    Float       = 5,
    Double      = 6,
    ByteArray   = 7,
    String      = 8,
    List        = 9,
    Compound    = 10,
    IntArray    = 11,
    LongArray   = 12,
}

public abstract class NBT
{
    public TagId   Id   { get; protected set; }
    public string? Name { get; protected set; }
    
    /// <summary>
    /// Get child tag, only if NBT is compound
    /// </summary>
    /// <exception cref="Exception">If nbt not a compound</exception>
    public virtual NBT this[string name] => throw new Exception("NBT is not a compound");
    
    /// <summary>
    /// Get child tag, only if NBT is list of tags
    /// </summary>
    /// <exception cref="Exception">If nbt not a list</exception>
    public virtual NBT this[int index]   => throw new Exception("NBT is not a list");

    /// <summary>
    /// Reads nbt in binary format
    /// </summary>
    /// <param name="readFunc">Function to read bytes, every call should return next byte</param>
    /// <exception cref="Exception">If unknown tag id reached</exception>
    public static NBT Read(Func<byte> readFunc)
    {
        string? ReadStr()
        {
            var len  = BigEndianReader.ReadUshort(readFunc);
            if (len == 0)
                return null;
            var utf8 = new byte[len];
            for (var i = 0; i < len; i++)
            {
                utf8[i] = readFunc();
            }
            // TODO: changed utf8
            return Encoding.UTF8.GetString(utf8);
        }
        // Read only payloud, without type and id
        NBT ReadPayload(string? name, int id)
        {
            switch ((TagId)id)
            {
                case TagId.Byte:
                    return new NBTbyte(name, BigEndianReader.ReadSByte(readFunc));
                case TagId.Short:
                    return new NBTshort(name, BigEndianReader.ReadShort(readFunc));
                case TagId.Int:
                    return new NBTint(name, BigEndianReader.ReadInt(readFunc));
                case TagId.Long:
                    return new NBTlong(name, BigEndianReader.ReadLong(readFunc));
                case TagId.Float:
                    return new NBTfloat(name, BigEndianReader.ReadFloat(readFunc));
                case TagId.Double:
                    return new NBTdouble(name, BigEndianReader.ReadDouble(readFunc));
                case TagId.String:
                    return new NBTstring(name, ReadStr() ?? "");
                case TagId.ByteArray:
                {
                    var arrLen = BigEndianReader.ReadInt(readFunc);
                    var array  = new byte[arrLen];
                    for (var i = 0; i < arrLen; i++) {
                        array[i] = readFunc();
                    }
                    return new NBTbyteArr(name, array);
                }
                case TagId.IntArray:
                {
                    var arrLen = BigEndianReader.ReadInt(readFunc);
                    var array  = new int[arrLen];
                    for (var i = 0; i < arrLen; i++) {
                        array[i] = BigEndianReader.ReadInt(readFunc);
                    }
                    return new NBTintArr(name, array);
                }
                case TagId.LongArray:
                {
                    var arrLen = BigEndianReader.ReadInt(readFunc);
                    var array  = new long[arrLen];
                    for (var i = 0; i < arrLen; i++) {
                        array[i] = BigEndianReader.ReadLong(readFunc);
                    }
                    return new NBTlongArr(name, array);
                }
                case TagId.List:
                {
                    var tagId   = BigEndianReader.ReadByte(readFunc);
                    var listLen = BigEndianReader.ReadInt(readFunc);
                    var tags    = new List<NBT>(listLen);
                    for (var i = 0; i < listLen; i++) {
                        tags.Add(ReadPayload(null, tagId));
                    }
                    return new NBTlist(name, tags);
                }
                case TagId.Compound:
                {
                    var tags = new List<NBT>();
                    while (true)
                    {
                        var tagId = BigEndianReader.ReadByte(readFunc);
                        if (tagId == 0)
                            break;
                        var tagName = ReadStr();
                        tags.Add(ReadPayload(tagName, tagId));
                    }
                    return new NBTcompound(name, tags);
                }
            }
            throw new Exception("Unknown tag id: " + id);
        }
        var id   = BigEndianReader.ReadByte(readFunc);
        var name = ReadStr();
        return ReadPayload(name, id);
    }
    /// <summary>
    /// Reads nbt in binary format
    /// </summary>
    /// <param name="stream">uses only ReadByte method</param>
    /// <exception cref="Exception">If unknown tag id reached</exception>
    public static NBT Read(Stream stream)
    {
        return Read(()=> unchecked((byte)stream.ReadByte()));   
    }
    
    /// <summary>
    /// Writes nbt in binary format
    /// </summary>
    /// <param name="tag">tag to write</param>
    /// <param name="writeFunc">Each call - next byte</param>
    /// <exception cref="Exception">If unknown tag id or format</exception>
    public static void Write(NBT tag, Action<byte> writeFunc)
    {
        void WriteStr(string? str)
        {
            if (string.IsNullOrEmpty(str))
            {
                BigEndianWriter.WriteUShort(writeFunc, 0);
                return;
            }
            var utf8 = Encoding.UTF8.GetBytes(str);
            BigEndianWriter.WriteUShort(writeFunc, (ushort)utf8.Length);
            foreach (var b in utf8) {
                BigEndianWriter.WriteByte(writeFunc, b);
            }
        }
        // Write only payload, without type and name
        void WritePayload(NBT nbttag)
        {
            if ((int)nbttag.Id <= 0 || (int)nbttag.Id > 12)
                throw new Exception("Unknown nbt id: " + (int)nbttag.Id);
            switch (nbttag.Id)
            {
                case TagId.Byte:
                    BigEndianWriter.WriteSByte(writeFunc, ((NBTbyte)nbttag).Value);
                    break;
                case TagId.Short:
                    BigEndianWriter.WriteShort(writeFunc, ((NBTshort)nbttag).Value);
                    break;
                case TagId.Int:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTint)nbttag).Value);
                    break;
                case TagId.Long:
                    BigEndianWriter.WriteLong(writeFunc, ((NBTlong)nbttag).Value);
                    break;
                case TagId.Float:
                    BigEndianWriter.WriteFloat(writeFunc, ((NBTfloat)nbttag).Value);
                    break;
                case TagId.Double:
                    BigEndianWriter.WriteDouble(writeFunc, ((NBTdouble)nbttag).Value);
                    break;
                case TagId.String:
                    WriteStr(((NBTstring)nbttag).Value);
                    break;
                case TagId.ByteArray:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTbyteArr)nbttag).Value.Length);
                    foreach (var b in ((NBTbyteArr)nbttag).Value) {
                        writeFunc(b);
                    }
                    break;
                case TagId.IntArray:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTintArr)nbttag).Value.Length);
                    foreach (var i in ((NBTintArr)nbttag).Value) {
                        BigEndianWriter.WriteInt(writeFunc, i);
                    }
                    break;
                case TagId.LongArray:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTlongArr)nbttag).Value.Length);
                    foreach (var l in ((NBTlongArr)nbttag).Value) {
                        BigEndianWriter.WriteLong(writeFunc, l);
                    }
                    break;
                case TagId.List:
                    var list = ((NBTlist)nbttag).Value;
                    if (list.Count == 0)
                    {
                        // if no items, type = 1
                        BigEndianWriter.WriteByte(writeFunc, 1);
                        BigEndianWriter.WriteInt(writeFunc, 0);
                    }
                    else
                    {
                        BigEndianWriter.WriteByte(writeFunc, (byte)list[0].Id);
                        BigEndianWriter.WriteInt(writeFunc, list.Count);
                        foreach (var nbt in list)
                        {
                            WritePayload(nbt);
                        }
                    }
                    break;
                case TagId.Compound:
                    var dict = ((NBTcompound)nbttag).Value;
                    foreach (var nbt in dict.Values)
                    {
                        if (nbt.Name == null)
                            throw new Exception("Compound cant contain nameless tags");
                        BigEndianWriter.WriteByte(writeFunc, (byte)nbt.Id);
                        WriteStr(nbt.Name);
                        WritePayload(nbt);
                    }
                    // end tag
                    BigEndianWriter.WriteByte(writeFunc, 0);
                    break;
            }
        }
        BigEndianWriter.WriteByte(writeFunc, (byte)tag.Id);
        WriteStr(tag.Name);
        WritePayload(tag);
    }
    /// <summary>
    /// Writes nbt in binary format
    /// </summary>
    /// <param name="tag">tag to write</param>
    /// <param name="stream">uses only WriteByte method</param>
    /// <exception cref="Exception">If unknown tag id or format</exception>
    public static void Write(NBT tag, Stream stream)
    {
        Write(tag, stream.WriteByte);
    }
}

public class NBTbyte : NBT
{
    public sbyte Value;

    public NBTbyte(string? name, sbyte value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.Byte;
    }
    public NBTbyte(sbyte value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <byte>: {Value}";
}
public class NBTshort : NBT
{
    public short Value;

    public NBTshort(string? name, short value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.Short;
    }
    public NBTshort(short value): this(null, value)
    { }

    public override string ToString() => $"{Name} <short>: {Value}";
}
public class NBTint : NBT
{
    public int Value;

    public NBTint(string? name, int value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.Int;
    }
    public NBTint(int value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <int>: {Value}";
}
public class NBTlong : NBT
{
    public long Value;

    public NBTlong(string? name, long value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.Long;
    }
    public NBTlong(long value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <long>: {Value}";
}
public class NBTfloat : NBT
{
    public float Value;

    public NBTfloat(string? name, float value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.Float;
    }
    public NBTfloat(float value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <float>: {Value}";
}
public class NBTdouble : NBT
{
    public double Value;

    public NBTdouble(string? name, double value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.Double;
    }
    public NBTdouble(double value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <double>: {Value}";
}
public class NBTcompound : NBT
{
    public Dictionary<string, NBT> Value = new();

    public NBTcompound(string? name, IEnumerable<NBT> children)
    {
        Id    = TagId.Compound;
        Name  = name;
        foreach (var child in children)
        {
            if (child.Name == null)
                throw new Exception("All tags in compound must be named");
            Value.Add(child.Name, child);
        }
    }
    public NBTcompound(IEnumerable<NBT> children) : this(null, children)
    { }
    
    public override NBT this[string name] => Value[name];
    
    public override string ToString() => $"{Name} <compound>: {{\n{string.Join(",\n", Value.Values)}\n}}";
}
public class NBTbyteArr : NBT
{
    public byte[] Value;

    public NBTbyteArr(string? name, byte[] value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.ByteArray;
    }
    public NBTbyteArr(byte[] value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <byte[]>: [{string.Join(", ", Value)}]";
}
public class NBTintArr : NBT
{
    public int[] Value;

    public NBTintArr(string? name, int[] value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.IntArray;
    }
    public NBTintArr(int[] value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <int[]>: [{string.Join(", ", Value)}]";
}
public class NBTlongArr : NBT
{
    public long[] Value;

    public NBTlongArr(string? name, long[] value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.LongArray;
    }
    public NBTlongArr(long[] value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <long[]>: [{string.Join(", ", Value)}]";
}
public class NBTstring : NBT
{
    public string Value;

    public NBTstring(string? name, string value)
    {
        Name  = name;
        Value = value;
        Id    = TagId.String;
    }
    public NBTstring(string value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <string>: '{Value}'";
}
public class NBTlist : NBT
{
    public List<NBT> Value;

    public NBTlist(string? name, IEnumerable<NBT> children)
    {
        Id    = TagId.List;
        Name  = name;
        Value = children.ToList();
    }
    public NBTlist(IEnumerable<NBT> children) : this(null, children)
    { }
    
    public override NBT this[int index] => Value[index];
    
    public override string ToString() => $"{Name} <list>: [{string.Join(", ", Value)}]";
}
