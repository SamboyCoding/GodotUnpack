using System.Runtime.InteropServices;

namespace GodotUnpack.Model;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PckFileHeader
{
    public const uint ExpectedMagic = 0x43_50_44_47; //GDPC but in little endian
    
    public static int Size => sizeof(PckFileHeader);
    
    public uint Magic;
    public uint FormatVersion;
    public uint EngineMajorVersion;
    public uint EngineMinorVersion;
    public uint EnginePatchVersion;
    public fixed uint Reserved[16];
    public uint FileCount;
}