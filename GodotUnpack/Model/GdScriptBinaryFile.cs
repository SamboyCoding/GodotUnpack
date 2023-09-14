using System.Text;
using GodotUnpack.Util;

namespace GodotUnpack.Model;

public class GdScriptBinaryFile
{
    private const uint TokenByteMask = 0x80;
    public GdScriptBinaryFileHeader Header { get; init; }
    public string[] Identifiers { get; init; }
    public GdScriptConstant[] Constants { get; init; }
    public Dictionary<int, uint> LineMap { get; init; }
    public uint[] Tokens { get; init; }
    
    public static GdScriptBinaryFile Read(MemoryMappedFileSpanWrapper wrapper, ref long pos)
    {
        var header = wrapper.Read<GdScriptBinaryFileHeader>(pos);
        pos += GdScriptBinaryFileHeader.Size;
        
        var identifiers = new string[header.IdentifierMapSize];
        for (var i = 0; i < identifiers.Length; i++)
            identifiers[i] = GdScriptIdentifier.Read(wrapper, ref pos);
        
        var constants = new GdScriptConstant[header.ConstantMapSize];
        for (var i = 0; i < constants.Length; i++)
            constants[i] = GdScriptConstant.Read(wrapper, ref pos);
        
        var lineMap = new Dictionary<int, uint>();
        for (var i = 0; i < header.LineMapSize; i++)
        {
            var token = wrapper.Read<int>(pos);
            var lineAndColumn = wrapper.Read<uint>(pos + sizeof(int));
            lineMap.Add(token, lineAndColumn);
            pos += sizeof(int) + sizeof(uint);
        }
        
        var tokens = new uint[header.TokenArraySize];
        for (var i = 0; i < tokens.Length; i++)
        {
            var b = wrapper.Read<byte>(pos);

            if ((b & TokenByteMask) != 0)
            {
                tokens[i] = wrapper.Read<uint>(pos)/* & ~tokenByteMask*/;
                pos += sizeof(uint);
            }
            else
            {
                tokens[i] = b;
                pos += sizeof(byte);
            }
        }

        pos += header.TokenArraySize * sizeof(uint);
        
        return new()
        {
            Header = header,
            Identifiers = identifiers,
            Constants = constants,
            LineMap = lineMap,
            Tokens = tokens
        };
    }

    public string DumpMetadata()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Bytecode Version: {Header.BytecodeVersion}");
        
        sb.AppendLine();
        sb.AppendLine($"Identifiers ({Header.IdentifierMapSize}):");
        for (var i = 0; i < Identifiers.Length; i++)
            sb.AppendLine($"\t{i} => {Identifiers[i]}");
        
        sb.AppendLine();
        sb.AppendLine($"Constants ({Header.ConstantMapSize}):");
        for (var i = 0; i < Constants.Length; i++)
            sb.AppendLine($"\t{i} => {Constants[i].Kind} : {Constants[i].TryFormat()}");

        sb.AppendLine();
        sb.AppendLine($"Line Map ({Header.LineMapSize}) - token to line/column:");
        foreach (var (token, lineAndColumn) in LineMap)
            sb.AppendLine($"\t{token:000} => {lineAndColumn}");
        
        sb.AppendLine();
        sb.AppendLine($"Tokens ({Header.TokenArraySize}):");
        for (var i = 0; i < Tokens.Length; i++)
            sb.AppendLine($"\t{i:000} => 0x{Tokens[i]:X8}");

        sb.AppendLine();
        sb.AppendLine("Decompiled output:");
        sb.AppendLine();

        sb.Append(Decompile());
        
        return sb.ToString();
    }

    public string Decompile()
    {
        var sb = new StringBuilder();
        var indent = 0;
        foreach (var t in Tokens)
        {
            var token = (GdScriptTokenType)(t & 0x7F);
            var data = (t >> 8);
            switch (token)
            {
                case GdScriptTokenType.NewLine:
                    sb.AppendLine();
                    indent = (int)data;
                    for (var i = 0; i < indent; i++)
                        sb.Append('\t');
                    break;
                case GdScriptTokenType.Identifier:
                    sb.Append(Identifiers[data]);
                    break;
                case GdScriptTokenType.Constant:
                    sb.Append(Constants[data].TryFormat());
                    break;
                case GdScriptTokenType.BuiltInType:
                    sb.Append(GdScriptBuiltins.Types[(int)data]);
                    break;
                case GdScriptTokenType.BuiltInFunc:
                    sb.Append(GdScriptBuiltins.Functions[(int)data]);
                    break;
                case GdScriptTokenType.Colon:
                    sb.Append(':');
                    break;
                case GdScriptTokenType.Comma:
                    sb.Append(", ");
                    break;
                case GdScriptTokenType.ParenthesisOpen:
                    sb.Append('(');
                    break;
                case GdScriptTokenType.ParenthesisClose:
                    sb.Append(')');
                    break;
                case GdScriptTokenType.BracketOpen:
                    sb.Append('[');
                    break;
                case GdScriptTokenType.BracketClose:
                    sb.Append(']');
                    break;
                case GdScriptTokenType.CurlyBracketOpen:
                    sb.Append('{');
                    break;
                case GdScriptTokenType.CurlyBracketClose:
                    sb.Append('}');
                    break;
                case GdScriptTokenType.Period:
                    sb.Append('.');
                    break;
                case GdScriptTokenType.ForwardArrow:
                    sb.Append("-> ");
                    break;
                case GdScriptTokenType.PrClass:
                    sb.Append("class ");
                    break;
                case GdScriptTokenType.PrClassName:
                    sb.Append("class_name ");
                    break;
                case GdScriptTokenType.PrExtends:
                    sb.Append("extends ");
                    break;
                case GdScriptTokenType.PrConst:
                    sb.Append("const ");
                    break;
                case GdScriptTokenType.PrVar:
                    sb.Append("var ");
                    break;
                case GdScriptTokenType.PrFunc:
                    sb.Append("func ");
                    break;
                case GdScriptTokenType.PrStatic:
                    sb.Append("static ");
                    break;
                case GdScriptTokenType.PrAs:
                    sb.Append(" as ");
                    break;
                case GdScriptTokenType.PrPreload:
                    sb.Append("preload");
                    break;
                case GdScriptTokenType.OpAssign:
                    sb.Append("= ");
                    break;
                case GdScriptTokenType.OpShiftRight:
                    sb.Append(">> ");
                    break;
                case GdScriptTokenType.OpShiftLeft:
                    sb.Append("<< ");
                    break;
                case GdScriptTokenType.OpIn:
                    sb.Append(" in ");
                    break;
                case GdScriptTokenType.OpEqual:
                    sb.Append(" == ");
                    break;
                case GdScriptTokenType.OpAdd:
                    sb.Append(" + ");
                    break;
                case GdScriptTokenType.OpSub:
                    sb.Append(" - ");
                    break;
                case GdScriptTokenType.OpMul:
                    sb.Append(" * ");
                    break;
                case GdScriptTokenType.OpDiv:
                    sb.Append(" / ");
                    break;
                case GdScriptTokenType.OpMod:
                    sb.Append(" % ");
                    break;
                case GdScriptTokenType.OpLess:
                    sb.Append(" < ");
                    break;
                case GdScriptTokenType.OpLessEqual:
                    sb.Append(" <= ");
                    break;
                case GdScriptTokenType.OpGreater:
                    sb.Append(" > ");
                    break;
                case GdScriptTokenType.OpGreaterEqual:
                    sb.Append(" >= ");
                    break;
                case GdScriptTokenType.OpNotEqual:
                    sb.Append(" != ");
                    break;
                case GdScriptTokenType.OpNot:
                    sb.Append('!');
                    break;
                case GdScriptTokenType.OpAnd:
                    sb.Append(" and ");
                    break;
                case GdScriptTokenType.OpOr:
                    sb.Append(" or ");
                    break;
                case GdScriptTokenType.OpAssignAdd:
                    sb.Append(" += ");
                    break;
                case GdScriptTokenType.CfFor:
                    sb.Append("for ");
                    break;
                case GdScriptTokenType.CfWhile:
                    sb.Append("while ");
                    break;
                case GdScriptTokenType.CfIf:
                    sb.Append("if ");
                    break;
                case GdScriptTokenType.CfElif:
                    sb.Append("elif ");
                    break;
                case GdScriptTokenType.CfElse:
                    sb.Append("else ");
                    break;
                case GdScriptTokenType.CfReturn:
                    sb.Append("return ");
                    break;
                case GdScriptTokenType.CfContinue:
                    sb.Append("continue ");
                    break;
                case GdScriptTokenType.CfMatch:
                    sb.Append("match ");
                    break;
                case GdScriptTokenType.Eof:
                    //no-op
                    break;
                default:
                    sb.Append(token).Append(' ');
                    break;
            }
        }

        return sb.ToString();
    }
}