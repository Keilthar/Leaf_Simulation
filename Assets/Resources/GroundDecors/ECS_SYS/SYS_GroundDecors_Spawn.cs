using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SYS_GroundDecors_Destroy))]
partial struct SYS_GroundDecors_Spawn : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<REQ_GroundDecors_Spawn>();
        state.RequireForUpdate<REF_GroundDecors_Prefabs>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity _REQEntity = SystemAPI.GetSingletonEntity<REQ_GroundDecors_Spawn>();
        REQ_GroundDecors_Spawn _REQ = SystemAPI.GetSingleton<REQ_GroundDecors_Spawn>();
        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);

        int _GroundDecorsCount = (int) (_REQ._AreaSize.x / _REQ._Spacing * _REQ._AreaSize.y / _REQ._Spacing);
        Job_GroundDecors_Spawn _Job = new Job_GroundDecors_Spawn
        {
            _ECB = _ECB.AsParallelWriter(),
            _REF = SystemAPI.GetSingleton<REF_GroundDecors_Prefabs>(),
            _REQ = _REQ
        };
        _Job.Schedule(_GroundDecorsCount, 64).Complete();

        _ECB.Playback(state.EntityManager);
        _ECB.Dispose();

        state.EntityManager.DestroyEntity(_REQEntity);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    partial struct Job_GroundDecors_Spawn : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        public REQ_GroundDecors_Spawn _REQ;
        public REF_GroundDecors_Prefabs _REF;

        public void Execute(int _Index)
        {
            Random _RNG = new Random((uint)(123456 + _Index * 1567));
    
            int _Roll_Density = _RNG.NextInt(0, 100);
            if (_Roll_Density >= _REQ._Density)
                return;

            int _Ratio_Total = _REQ._Ratio_Grass + _REQ._Ratio_Rocks + _REQ._Ratio_Mushrooms + _REQ._Ratio_Flowers;
            int _Roll_Prefab = _RNG.NextInt(0, _Ratio_Total);
            Entity _PrefabToInstantiate;
            if (_Roll_Prefab < _REQ._Ratio_Grass)
                _PrefabToInstantiate = _REF._Prefab_Grass;
            else if (_Roll_Prefab < (_REQ._Ratio_Grass + _REQ._Ratio_Flowers))
                _PrefabToInstantiate = _REF._Prefab_Flower;
            else if (_Roll_Prefab < (_REQ._Ratio_Grass + _REQ._Ratio_Flowers + _REQ._Ratio_Rocks))
                _PrefabToInstantiate = _REF._Prefab_Rock;
            else
                _PrefabToInstantiate = _REF._Prefab_Mushroom;

            int _GridWidth = (int)(_REQ._AreaSize.x / _REQ._Spacing);
            int _GridX = _Index % _GridWidth;
            int _GridZ = _Index / _GridWidth;
            float3 _Position = new float3
            {
                x = _GridX * _REQ._Spacing,
                y = 0,
                z = _GridZ * _REQ._Spacing
            };
            quaternion _Rotation = quaternion.Euler(
                0,
                math.radians(_RNG.NextInt(0, 360)),
                0
            );

            Entity _GroundDecors = _ECB.Instantiate(_Index, _PrefabToInstantiate);
            _ECB.SetComponent(_Index, _GroundDecors, new LocalTransform
            {
                Position = _Position,
                Rotation = _Rotation,
                Scale = _RNG.NextFloat(0.8f, 1.2f)
            });
            _ECB.AddComponent(_Index, _GroundDecors, new TAG_GroundDecors { });
            _ECB.AddComponent(_Index, _GroundDecors, new RenderBounds
            {
                Value = new AABB
                {
                    Center = float3.zero,
                    Extents = new float3(1f)
                }
            });
        }
    }
}
