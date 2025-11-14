using Unity.Entities;
using Unity.Mathematics;

public struct REF_GroundDecors_Prefabs : IComponentData
{
    public Entity _Prefab_Grass;
    public Entity _Prefab_Flower;
    public Entity _Prefab_Mushroom;
    public Entity _Prefab_Rock;
}

public struct TAG_GroundDecors : IComponentData{}

public struct REQ_GroundDecors_Destroy : IComponentData {}

public struct REQ_GroundDecors_Spawn : IComponentData
{
    public float2 _AreaSize;
    public float _Spacing;
    public float _Density;
    public int _Ratio_Grass;
    public int _Ratio_Rocks;
    public int _Ratio_Mushrooms;
    public int _Ratio_Flowers;
}
