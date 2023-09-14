using System.Runtime.Intrinsics;
using System.Text;
using GodotUnpack.Util;

namespace GodotUnpack.Model;

public class PckFileAssetInfo
{
    public string Name { get; init; }
    public long Offset { get; init; }
    public long Size { get; init; }
    public Vector128<byte> Md5 { get; init; }
    public static PckFileAssetInfo Read(MemoryMappedFileSpanWrapper wrapper, ref long pos)
    {
        var nameLength = wrapper.Read<uint>(pos);
        var name = Encoding.UTF8.GetString(wrapper.GetReadOnlySpan(pos + 4, nameLength));
        name = name.TrimEnd('\0');
        
        //Align after reading name
        pos += 4 + nameLength;
        pos = (pos + 3) & ~3;
        
        var offset = wrapper.Read<long>(pos);
        var size = wrapper.Read<long>(pos + sizeof(long));
        var md5 = wrapper.Read<Vector128<byte>>(pos + sizeof(long) * 2);
        
        pos += sizeof(long) * 2 + Vector128<byte>.Count;
        
        //Align pos to 4 bytes
        pos = (pos + 3) & ~3;
        
        return new()
        {
            Name = name,
            Offset = offset,
            Size = size,
            Md5 = md5
        };
    }

}