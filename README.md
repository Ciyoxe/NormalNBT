# NormalNBT

Small C# library for reading and writing named binary tags

Example:
```C#
using NormalNBT;

var outstream = new MemoryStream();
var tag       = new NBTcompound("Test nbt", new NBT[]
{
    new NBTbyte("byte tag", 127),
    new NBTintArr("array tag", new []{ 1, 2, 3, 4 }),
    new NBTlist("list tag", new []
    {
        new NBTlong(0),
        new NBTlong(123456),
    })
});
NBT.Write(tag, outstream);

var instream = new MemoryStream(outstream.ToArray());
var newtag = NBT.Read(instream);

//  Test nbt <compound>: {
//  byte tag <byte>: 127,
//  array tag <int[]>: [1, 2, 3, 4],
//  list tag <list>: [ <long>: 0,  <long>: 0]
//  }
Console.WriteLine(newtag);
// 123456
Console.WriteLine(((NBTlong)newtag["list tag"][1]).Value);
```
If you want to use your own collection, you can pass functions into `NBT.Write` and `NBT.Read`
```C#
var queue = new Queue<byte>();
var tag   = new NBTcompound("Test nbt", ...);
NBT.Write(tag, queue.Enqueue);
NBT.Read(queue.Dequeue);
```
Example: Reading compressed level.dat file
```C#
var filestream = File.Open("level.dat", FileMode.Open);
var datastream = new GZipStream(filestream, CompressionMode.Decompress);
Console.WriteLine(NBT.Read(datastream));
```
