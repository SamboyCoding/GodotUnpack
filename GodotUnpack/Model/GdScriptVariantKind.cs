namespace GodotUnpack.Model;

[Flags]
public enum GdScriptVariantKind : uint
{
    Nil,
    
    Bool,
    Int,
    Real,
    String,
    
    Vector2,
    Rect2,
    Vector3,
    Transform2D,
    Plane,
    Quat,
    Aabb,
    Basis,
    Transform,
    
    Color,
    NodePath,
    Rid,
    Object,
    Dictionary,
    Array,
    
    PoolByteArray,
    PoolIntArray,
    PoolRealArray,
    PoolStringArray,
    PoolVector2Array,
    PoolVector3Array,
    PoolColorArray,
    
    Flag64Bit = 1 << 16,
    FlagObjectAsId = Flag64Bit,
}