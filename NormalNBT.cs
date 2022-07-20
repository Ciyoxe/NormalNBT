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
    public abstract object Value { get; set; }
    
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
                    var array  = new List<byte>(arrLen);
                    for (var i = 0; i < arrLen; i++) {
                        array.Add(readFunc());
                    }
                    return new NBTbyteArr(name, array);
                }
                case TagId.IntArray:
                {
                    var arrLen = BigEndianReader.ReadInt(readFunc);
                    var array  = new List<int>(arrLen);
                    for (var i = 0; i < arrLen; i++) {
                        array.Add(BigEndianReader.ReadInt(readFunc));
                    }
                    return new NBTintArr(name, array);
                }
                case TagId.LongArray:
                {
                    var arrLen = BigEndianReader.ReadInt(readFunc);
                    var array  = new List<long>(arrLen);
                    for (var i = 0; i < arrLen; i++) {
                        array.Add(BigEndianReader.ReadLong(readFunc));
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
                    BigEndianWriter.WriteSByte(writeFunc, ((NBTbyte)nbttag).Data);
                    break;
                case TagId.Short:
                    BigEndianWriter.WriteShort(writeFunc, ((NBTshort)nbttag).Data);
                    break;
                case TagId.Int:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTint)nbttag).Data);
                    break;
                case TagId.Long:
                    BigEndianWriter.WriteLong(writeFunc, ((NBTlong)nbttag).Data);
                    break;
                case TagId.Float:
                    BigEndianWriter.WriteFloat(writeFunc, ((NBTfloat)nbttag).Data);
                    break;
                case TagId.Double:
                    BigEndianWriter.WriteDouble(writeFunc, ((NBTdouble)nbttag).Data);
                    break;
                case TagId.String:
                    WriteStr(((NBTstring)nbttag).Data);
                    break;
                case TagId.ByteArray:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTbyteArr)nbttag).Data.Count);
                    foreach (var b in ((NBTbyteArr)nbttag).Data) {
                        writeFunc(b);
                    }
                    break;
                case TagId.IntArray:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTintArr)nbttag).Data.Count);
                    foreach (var i in ((NBTintArr)nbttag).Data) {
                        BigEndianWriter.WriteInt(writeFunc, i);
                    }
                    break;
                case TagId.LongArray:
                    BigEndianWriter.WriteInt(writeFunc, ((NBTlongArr)nbttag).Data.Count);
                    foreach (var l in ((NBTlongArr)nbttag).Data) {
                        BigEndianWriter.WriteLong(writeFunc, l);
                    }
                    break;
                case TagId.List:
                    var list = ((NBTlist)nbttag).Data;
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
                    var dict = ((NBTcompound)nbttag).Data;
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
    public sbyte Data;

    public override object Value 
    { 
        get => Data; 
        set => Data = (sbyte)value; 
    }

    public NBTbyte(string? name, sbyte value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.Byte;
    }
    public NBTbyte(sbyte value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <byte>: {Data}";
}
public class NBTshort : NBT
{
    public short Data;

    public override object Value
    {
        get => Data;
        set => Data = (short)value;
    }

    public NBTshort(string? name, short value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.Short;
    }
    public NBTshort(short value): this(null, value)
    { }

    public override string ToString() => $"{Name} <short>: {Data}";
}
public class NBTint : NBT
{
    public int Data;

    public override object Value
    {
        get => Data;
        set => Data = (int)value;
    }

    public NBTint(string? name, int value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.Int;
    }
    public NBTint(int value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <int>: {Data}";
}
public class NBTlong : NBT
{
    public long Data;

    public override object Value
    {
        get => Data;
        set => Data = (long)value;
    }

    public NBTlong(string? name, long value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.Long;
    }
    public NBTlong(long value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <long>: {Data}";
}
public class NBTfloat : NBT
{
    public float Data;

    public override object Value
    {
        get => Data;
        set => Data = (float)value;
    }
    public NBTfloat(string? name, float value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.Float;
    }
    public NBTfloat(float value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <float>: {Data}";
}
public class NBTdouble : NBT
{
    public double Data;

    public override object Value
    {
        get => Data;
        set => Data = (double)value;
    }
    public NBTdouble(string? name, double value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.Double;
    }
    public NBTdouble(double value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <double>: {Data}";
}
public class NBTcompound : NBT
{
    public Dictionary<string, NBT> Data = new();

    public override object Value
    {
        get => Data;
        set => Data = (Dictionary<string, NBT>)value;
    }

    public NBTcompound(string? name, IEnumerable<NBT> children)
    {
        Id    = TagId.Compound;
        Name  = name;
        foreach (var child in children)
        {
            if (child.Name == null)
                throw new Exception("All tags in compound must be named");
            Data.Add(child.Name, child);
        }
    }
    public NBTcompound(IEnumerable<NBT> children) : this(null, children)
    { }
    
    public override NBT this[string name] => Data[name];
    
    public override string ToString() => $"{Name} <compound>: {{\n{string.Join(",\n", Data.Values)}\n}}";
}
public class NBTbyteArr : NBT
{
    public List<byte> Data;

    public override object Value
    {
        get => Data;
        set => Data = (List<byte>)value;
    }

    public NBTbyteArr(string? name, List<byte> value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.ByteArray;
    }
    public NBTbyteArr(List<byte> value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <byte[]>: [{string.Join(", ", Data)}]";
}
public class NBTintArr : NBT
{
    public List<int> Data;

    public override object Value
    {
        get => Data;
        set => Data = (List<int>)value;
    }

    public NBTintArr(string? name, List<int> value)
    {
        Name = name;
        Data = value;
        Id   = TagId.IntArray;
    }
    public NBTintArr(List<int> value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <int[]>: [{string.Join(", ", Data)}]";
}
public class NBTlongArr : NBT
{
    public List<long> Data;

    public override object Value
    {
        get => Data;
        set => Data = (List<long>)value;
    }

    public NBTlongArr(string? name, List<long> value)
    {
        Name = name;
        Data = value;
        Id   = TagId.LongArray;
    }
    public NBTlongArr(List<long> value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <long[]>: [{string.Join(", ", Data)}]";
}
public class NBTstring : NBT
{
    public string Data;

    public override object Value
    {
        get => Data;
        set => Data = (string)value;
    }

    public NBTstring(string? name, string value)
    {
        Name  = name;
        Data = value;
        Id    = TagId.String;
    }
    public NBTstring(string value): this(null, value)
    { }
    
    public override string ToString() => $"{Name} <string>: '{Data}'";
}
public class NBTlist : NBT
{
    public List<NBT> Data;

    public override object Value
    {
        get => Data;
        set => Data = (List<NBT>)value;
    }

    public NBTlist(string? name, IEnumerable<NBT> children)
    {
        Id    = TagId.List;
        Name  = name;
        Data = children.ToList();
    }
    public NBTlist(IEnumerable<NBT> children) : this(null, children)
    { }
    
    public override NBT this[int index] => Data[index];
    
    public override string ToString() => $"{Name} <list>: [{string.Join(", ", Data)}]";
}
