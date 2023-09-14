using System.Text;
using GodotUnpack.Util;

namespace GodotUnpack.Model;

public class GdScriptConstant
{
    public GdScriptVariantKind Kind { get; init; }
    public bool Is64Bit { get; init; }
    public byte[] Data { get; init; }
    
    public static GdScriptConstant Read(MemoryMappedFileSpanWrapper wrapper, ref long pos)
    {
        var kindAndFlags = wrapper.Read<GdScriptVariantKind>(pos);
        pos += sizeof(uint);
        
        var is64Bit = (kindAndFlags & GdScriptVariantKind.Flag64Bit) != 0;
        var kind = kindAndFlags & ~GdScriptVariantKind.Flag64Bit;

        var origPos = pos;
        var data = kind switch
        {
            GdScriptVariantKind.Nil => Array.Empty<byte>(),
            GdScriptVariantKind.Bool => wrapper.GetReadOnlySpan(pos, 4).ToArray(),
            GdScriptVariantKind.Int when is64Bit => wrapper.GetReadOnlySpan(pos, 8).ToArray(),
            GdScriptVariantKind.Int => wrapper.GetReadOnlySpan(pos, 4).ToArray(),
            GdScriptVariantKind.Real when is64Bit => wrapper.GetReadOnlySpan(pos, 8).ToArray(),
            GdScriptVariantKind.Real => wrapper.GetReadOnlySpan(pos, 4).ToArray(),
            GdScriptVariantKind.String => ReadString(wrapper, ref pos),
            GdScriptVariantKind.Vector2 => wrapper.GetReadOnlySpan(pos, 8).ToArray(), //2x float
            GdScriptVariantKind.Rect2 => wrapper.GetReadOnlySpan(pos, 16).ToArray(), //4x float
            GdScriptVariantKind.Vector3 => wrapper.GetReadOnlySpan(pos, 12).ToArray(), //3x float
            GdScriptVariantKind.Transform2D => wrapper.GetReadOnlySpan(pos, 24).ToArray(), //6x float
            GdScriptVariantKind.Plane => wrapper.GetReadOnlySpan(pos, 16).ToArray(), //4x float (x, y, z, d)
            GdScriptVariantKind.Quat => wrapper.GetReadOnlySpan(pos, 16).ToArray(), //4x float (x, y, z, w)
            GdScriptVariantKind.Aabb => wrapper.GetReadOnlySpan(pos, 24).ToArray(), //6x float (x, y, z, size_x, size_y, size_z)
            GdScriptVariantKind.Basis => wrapper.GetReadOnlySpan(pos, 36).ToArray(), //9x float (3x Vector3)
            GdScriptVariantKind.Transform => wrapper.GetReadOnlySpan(pos, 48).ToArray(), //12x float (3x Vector3, origin_x, origin_y, origin_z)
            GdScriptVariantKind.Color => wrapper.GetReadOnlySpan(pos, 4).ToArray(), //1x uint32 - RGBA
            GdScriptVariantKind.NodePath => ReadNodePath(wrapper, ref pos),
            GdScriptVariantKind.Rid => Array.Empty<byte>(), //3.5 doesn't write anything
            GdScriptVariantKind.Object => wrapper.GetReadOnlySpan(pos, 8).ToArray(), //1x uint64 - instance ID
            GdScriptVariantKind.Dictionary => throw new NotImplementedException("TODO - Dictionary"),
            GdScriptVariantKind.Array => throw new NotImplementedException("TODO - Array"),
            GdScriptVariantKind.PoolByteArray => ReadPoolArray(wrapper, ref pos, 1),
            GdScriptVariantKind.PoolIntArray => ReadPoolArray(wrapper, ref pos, 4),
            GdScriptVariantKind.PoolRealArray => ReadPoolArray(wrapper, ref pos, 4),
            GdScriptVariantKind.PoolStringArray => throw new NotImplementedException("TODO - PoolStringArray"),
            GdScriptVariantKind.PoolVector2Array => ReadPoolArray(wrapper, ref pos, 8),
            GdScriptVariantKind.PoolVector3Array => ReadPoolArray(wrapper, ref pos, 12),
            GdScriptVariantKind.PoolColorArray => ReadPoolArray(wrapper, ref pos, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(kind) , $"Unexpected variant kind - {kind}")
        };
        
        if(pos == origPos)
            //Not all of the above move the position, so we need to do it manually
            pos += data.Length;
        
        return new()
        {
            Kind = kind,
            Is64Bit = is64Bit,
            Data = data
        };
    }

    private static byte[] ReadNodePath(MemoryMappedFileSpanWrapper wrapper, ref long pos)
    {
        var originalPos = pos;
        var nameCount = wrapper.Read<uint>(pos);
        nameCount &= 0x7FFFFFFF; //Clear the top bit
        var subNameCount = wrapper.Read<uint>(pos);
        var flags = wrapper.Read<uint>(pos);
        pos += sizeof(uint) * 3;
        
        var totalCount = nameCount + subNameCount;
        
        for (var i = 0; i < totalCount; i++)
        {
            ReadString(wrapper, ref pos); //We don't care about storing it, just move the position 
        }
        
        //Take all the data from the original position to the current position
        return wrapper.GetReadOnlySpan(originalPos, pos - originalPos).ToArray();
    }

    private static byte[] ReadString(MemoryMappedFileSpanWrapper wrapper, ref long pos)
    {
        var length = wrapper.Read<uint>(pos);
        pos += sizeof(uint);
        
        var data = wrapper.GetReadOnlySpan(pos - 4, length + 4).ToArray();
        pos += length;
        
        //Align
        pos = (pos + 3) & ~3;
        
        return data;
    }
    
    private static byte[] ReadPoolArray(MemoryMappedFileSpanWrapper wrapper, ref long pos, int elementSize)
    {
        var length = wrapper.Read<uint>(pos);
        
        //Include the length in the data
        var data = wrapper.GetReadOnlySpan(pos, length * elementSize + sizeof(uint)).ToArray();
        pos += length * elementSize + sizeof(uint);
        
        return data;
    }

    public string TryFormat()
    {
        return Kind switch
        {
            GdScriptVariantKind.Nil => "nil",
            GdScriptVariantKind.Bool => BitConverter.ToBoolean(Data).ToString(),
            GdScriptVariantKind.Int when Is64Bit => BitConverter.ToInt64(Data).ToString(),
            GdScriptVariantKind.Int => BitConverter.ToInt32(Data).ToString(),
            GdScriptVariantKind.Real when Is64Bit => BitConverter.ToDouble(Data).ToString(),
            GdScriptVariantKind.Real => BitConverter.ToSingle(Data).ToString(),
            GdScriptVariantKind.String => '"' + Encoding.UTF8.GetString(Data.AsSpan(4)).TrimEnd('\0') + '"', //Skip the length, trim the null bytes, and quote
            _ => string.Join(' ', Data.Select(b => b.ToString("X2"))),
        };
    }
}