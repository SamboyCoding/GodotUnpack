namespace GodotUnpack.Model;

public struct GdScriptBinaryFileHeader
{
    public const uint ExpectedMagic = 0x43534447; //GDSC little endian
    
    public static unsafe int Size => sizeof(GdScriptBinaryFileHeader);
    
    public uint Magic; //GDSC little endian
    public uint BytecodeVersion;
    public uint IdentifierMapSize;
    public uint ConstantMapSize;
    public uint LineMapSize;
    public uint TokenArraySize;
}