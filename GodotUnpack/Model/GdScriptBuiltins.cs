﻿namespace GodotUnpack.Model;

public static class GdScriptBuiltins
{
    public static readonly Dictionary<int, string> Types = new()
    {
        { (int)GdScriptVariantKind.Bool, "bool" },
        { (int)GdScriptVariantKind.Int, "int" },
        { (int)GdScriptVariantKind.Real, "float" },
        { (int)GdScriptVariantKind.String, "String" },
        { (int)GdScriptVariantKind.Vector2, "Vector2" },
        { (int)GdScriptVariantKind.Rect2, "Rect2" },
        { (int)GdScriptVariantKind.Transform2D, "Transform2D" },
        { (int)GdScriptVariantKind.Vector3, "Vector3" },
        { (int)GdScriptVariantKind.Aabb, "AABB" },
        { (int)GdScriptVariantKind.Plane, "Plane" },
        { (int)GdScriptVariantKind.Quat, "Quat" },
        { (int)GdScriptVariantKind.Basis, "Basis" },
        { (int)GdScriptVariantKind.Transform, "Transform" },
        { (int)GdScriptVariantKind.Color, "Color" },
        { (int)GdScriptVariantKind.Rid, "RID" },
        { (int)GdScriptVariantKind.Object, "Object" },
        { (int)GdScriptVariantKind.NodePath, "NodePath" },
        { (int)GdScriptVariantKind.Dictionary, "Dictionary" },
        { (int)GdScriptVariantKind.Array, "Array" },
        { (int)GdScriptVariantKind.PoolByteArray, "PoolByteArray" },
        { (int)GdScriptVariantKind.PoolIntArray, "PoolIntArray" },
        { (int)GdScriptVariantKind.PoolRealArray, "PoolRealArray" },
        { (int)GdScriptVariantKind.PoolStringArray, "PoolStringArray" },
        { (int)GdScriptVariantKind.PoolVector2Array, "PoolVector2Array" },
        { (int)GdScriptVariantKind.PoolVector3Array, "PoolVector3Array" },
        { (int)GdScriptVariantKind.PoolColorArray, "PoolColorArray" },
    };

    public static readonly string[] Functions = new[]
    {
        "sin",
        "cos",
        "tan",
        "sinh",
        "cosh",
        "tanh",
        "asin",
        "acos",
        "atan",
        "atan2",
        "sqrt",
        "fmod",
        "fposmod",
        "posmod",
        "floor",
        "ceil",
        "round",
        "abs",
        "sign",
        "pow",
        "log",
        "exp",
        "is_nan",
        "is_inf",
        "is_equal_approx",
        "is_zero_approx",
        "ease",
        "decimals",
        "step_decimals",
        "stepify",
        "lerp",
        "lerp_angle",
        "inverse_lerp",
        "range_lerp",
        "smoothstep",
        "move_toward",
        "dectime",
        "randomize",
        "randi",
        "randf",
        "rand_range",
        "seed",
        "rand_seed",
        "deg2rad",
        "rad2deg",
        "linear2db",
        "db2linear",
        "polar2cartesian",
        "cartesian2polar",
        "wrapi",
        "wrapf",
        "max",
        "min",
        "clamp",
        "nearest_po2",
        "weakref",
        "funcref",
        "convert",
        "typeof",
        "type_exists",
        "char",
        "ord",
        "str",
        "print",
        "printt",
        "prints",
        "printerr",
        "printraw",
        "print_debug",
        "push_error",
        "push_warning",
        "var2str",
        "str2var",
        "var2bytes",
        "bytes2var",
        "range",
        "load",
        "inst2dict",
        "dict2inst",
        "validate_json",
        "parse_json",
        "to_json",
        "hash",
        "Color8",
        "ColorN",
        "print_stack",
        "get_stack",
        "instance_from_id",
        "len",
        "is_instance_valid",
        "deep_equal",
    };
}