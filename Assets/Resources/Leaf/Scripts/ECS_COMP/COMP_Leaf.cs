using Unity.Entities;
using Unity.Mathematics;

#region Components

public struct COMP_Leaf_Animation : IComponentData
{
    public float _Timer;
    public float _Duration;
    public float _Height;
    public float3 _Position_Start;
    public float3 _Position_End;
    public quaternion _Rotation_Start;
    public float3 _RotationSpeed;
    public float _LateralMoveFactor;
}

public struct COMP_Leaf_Realloc : IComponentData
{
    public float3 _Position;
    public quaternion _Rotation;
    public float _Scale;
    public float _Timer;
    public float _Duration;
}

public struct COMP_Grid_Data : IComponentData
{
    public int _Grid_ID;
    public float3 _Grid_Position;
    public float _Grid_Size;
}

public struct COMP_Grid_Leafs : IBufferElementData
{
    public Entity _Leaf;
}

public struct COMP_Force_Sphere : IComponentData
{
    public float3 _Position;
    public float _Radius;
    public float _Strength;
}

#endregion Components



#region TAGs

public struct TAG_Leaf : IComponentData { }
public struct TAG_Leaf_Spawned : IComponentData { }

#endregion TAGs



#region Referentials

public struct REF_Leaf : IBufferElementData
{
    public Entity _Prefab;
}

public struct REF_Leaf_Animation : IBufferElementData
{
    public float _Height;
    public float _Fly;
}

#endregion Referentials



#region Requests

public struct REQ_Grid_Destroy : IComponentData
{
    public int _Grid_ID;
}

public struct REQ_Grid_Generate : IComponentData
{
    public int _Grid_ID;
    public int3 _Grid_Position;
    public int _Grid_Size;

    public float _Leaf_Density;
    public float _Leaf_Spacing;
    public float _Leaf_SizeVariation;
}

#endregion Requests