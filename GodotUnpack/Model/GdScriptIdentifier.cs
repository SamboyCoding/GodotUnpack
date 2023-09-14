using System.Text;
using GodotUnpack.Util;

namespace GodotUnpack.Model;

public static class GdScriptIdentifier
{
    public static string Read(MemoryMappedFileSpanWrapper wrapper, ref long pos)
    {
        var nameLength = wrapper.Read<uint>(pos);
        var obfuscatedName = wrapper.GetReadOnlySpan(pos + 4, nameLength);
        var copy = obfuscatedName.ToArray();
        for (var i = 0; i < copy.Length; i++) 
            copy[i] ^= 0xb6; //Xor with 0xb6 to get the original name
        var name = Encoding.UTF8.GetString(copy);
        name = name.TrimEnd('\0');

        pos += 4 + nameLength; //Already aligned with trailing null bytes
        
        return name;
    }
}