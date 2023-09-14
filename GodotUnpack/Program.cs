using GodotUnpack.Model;
using GodotUnpack.Util;

internal class Program
{
    public static void Main(string[] args)
    {
        var path = args[0];

        var wrapper = new MemoryMappedFileSpanWrapper(path);
        var header = wrapper.Read<PckFileHeader>(0);
        
        if(header.Magic != PckFileHeader.ExpectedMagic)
            throw new InvalidDataException($"Invalid magic number - got {header.Magic:X8}, expected {PckFileHeader.ExpectedMagic:X8}");
        
        Console.WriteLine($"Format version: {header.FormatVersion}");
        Console.WriteLine($"Engine version: {header.EngineMajorVersion}.{header.EngineMinorVersion}.{header.EnginePatchVersion}");
        Console.WriteLine($"File Count: {header.FileCount}");

        var pos = (long) PckFileHeader.Size;
        var assetInfos = new PckFileAssetInfo[header.FileCount];

        for (var i = 0; i < header.FileCount; i++)
        {
            assetInfos[i] = PckFileAssetInfo.Read(wrapper, ref pos);
        }
        
        Console.WriteLine($"Read {assetInfos.Length} asset infos OK. Unpacking raw content now...");
        
        var outputDir = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "_unpacked");
        
        if(Directory.Exists(outputDir))
            Directory.Delete(outputDir, true);
        
        Directory.CreateDirectory(outputDir);

        for (var i = 0; i < assetInfos.Length; i++)
        {
            var assetInfo = assetInfos[i];
            
            const string resPrefix = "res://"; 
            var relativePath = assetInfo.Name[resPrefix.Length..];
            var assetPath = Path.Combine(outputDir, relativePath.Replace('/', '\\'));
            var extension = Path.GetExtension(assetPath);
            
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);

            // Console.WriteLine($"Unpacking {relativePath} ({i + 1}/{assetInfos.Length})");

            using var fileStream = File.OpenWrite(assetPath);

            wrapper.CopyTo(fileStream, assetInfo.Offset, assetInfo.Size);
            if (extension == ".gdc")
            {
                var scriptPos = assetInfo.Offset;
                var scriptPath = Path.ChangeExtension(assetPath, ".gdc_decompiled");

                try
                {
                    var gdScriptBinaryFile = GdScriptBinaryFile.Read(wrapper, ref scriptPos);

                    File.WriteAllText(scriptPath, gdScriptBinaryFile.Decompile());
                }
                catch (IOException)
                {
                    Console.WriteLine($"Failed to decompile {relativePath} - IO error");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to decompile {relativePath} - {e}");
                    File.WriteAllText(scriptPath, $"Failed to decompile {relativePath} - {e}");
                }
            }
        }
        
        Console.WriteLine("Done!");
    }
}