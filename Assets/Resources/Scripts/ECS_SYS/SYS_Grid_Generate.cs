using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SYS_Grid_Destroy))]
partial struct SYS_Grid_Generate : ISystem
{
    EntityQuery _Query_Leafs_Spawned;
    EntityQuery _Query_REQs;
    NativeArray<Entity> _Leaf_Prefabs;

    public void OnCreate(ref SystemState state)
    {
        _Query_REQs = state.EntityManager.CreateEntityQuery(typeof(REQ_Grid_Generate));
        _Query_Leafs_Spawned = state.EntityManager.CreateEntityQuery(typeof(TAG_Leaf_Spawned));
        state.RequireForUpdate<REQ_Grid_Generate>();
        state.RequireForUpdate<REF_Leaf>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (_Leaf_Prefabs.IsCreated == false)
            if (TrySet_Prefabs() == false)
                return;

        NativeArray<Entity> _REQs = _Query_REQs.ToEntityArray(Allocator.TempJob);
        foreach (Entity _REQ_Entity in _REQs)
        {
            REQ_Grid_Generate _REQ = state.EntityManager.GetComponentData<REQ_Grid_Generate>(_REQ_Entity);

            // Create Grid entity
            Entity _GridEntity = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(_GridEntity, $"Grid[{_REQ._Grid_ID}]");
            COMP_Grid_Data _GridData = new COMP_Grid_Data
            {
                _Grid_ID = _REQ._Grid_ID,
                _Grid_Position = _REQ._Grid_Position,
                _Grid_Size = _REQ._Grid_Size
            };
            state.EntityManager.AddComponentData(_GridEntity, _GridData);

            // Create leaf entities
            int _LeafCount = (int) (_REQ._Grid_Size / _REQ._Leaf_Spacing * _REQ._Grid_Size / _REQ._Leaf_Spacing);
            int _LeafPerBatch = 64;
            EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);
            Job_Generate_Grid _Job = new Job_Generate_Grid
            {
                _ECB = _ECB.AsParallelWriter(),
                _Leaf_Prefabs = _Leaf_Prefabs,
                _Grid = _GridData,
                _REQ = _REQ
            };
            _Job.Schedule(_LeafCount, _LeafPerBatch, state.Dependency).Complete();
            _ECB.Playback(state.EntityManager);
            _ECB.Dispose();

            // Store all leaf in grid buffer
            state.EntityManager.AddBuffer<COMP_Grid_Leafs>(_GridEntity);
            NativeArray<Entity> _Leafs = _Query_Leafs_Spawned.ToEntityArray(Allocator.Temp);
            _ECB = new EntityCommandBuffer(Allocator.Temp);
            foreach (Entity _Leaf in _Leafs)
            {
                _ECB.AppendToBuffer(_GridEntity, new COMP_Grid_Leafs { _Leaf = _Leaf });
                _ECB.RemoveComponent<TAG_Leaf_Spawned>(_Leaf);
            }
            _ECB.Playback(state.EntityManager);
            _ECB.Dispose();

            _Leafs.Dispose();
            state.EntityManager.DestroyEntity(_REQ_Entity);
        }

        _REQs.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_Leaf_Prefabs.IsCreated)
            _Leaf_Prefabs.Dispose();
    }

    [BurstCompile]
    public bool TrySet_Prefabs()
    {
        DynamicBuffer<REF_Leaf> _REFs = SystemAPI.GetSingletonBuffer<REF_Leaf>();
        if (_REFs.Length == 0)
            return false;

        _Leaf_Prefabs = new NativeArray<Entity>(_REFs.Length, Allocator.Persistent);
        for (int _RefID = 0; _RefID < _REFs.Length; _RefID++)
        {
            if (_REFs[_RefID]._Prefab != Entity.Null)
                _Leaf_Prefabs[_RefID] = _REFs[_RefID]._Prefab;
            else
            {
                _Leaf_Prefabs.Dispose();
                return false;
            }
        }

        return true;
    }


    [BurstCompile]
    partial struct Job_Generate_Grid : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        [ReadOnly] public NativeArray<Entity> _Leaf_Prefabs;
        public COMP_Grid_Data _Grid;
        public REQ_Grid_Generate _REQ;

        public void Execute(int _Index)
        {
            Random _RNG = new Random((uint)(123456 + _Index * 1567 + _Grid._Grid_Position.x * 9874 + _Grid._Grid_Position.z * 6547));

            int _Roll_Density = _RNG.NextInt(0, 100);
            if (_Roll_Density >= _REQ._Leaf_Density)
                return;

            int _Roll_Prefab = _RNG.NextInt(0, _Leaf_Prefabs.Length);
            Entity _Leaf = _ECB.Instantiate(_Index, _Leaf_Prefabs[_Roll_Prefab]);
            _ECB.SetName(_Index, _Leaf, $"Leaf[{_Grid._Grid_ID},{_Index}]");

            int _GridWidth = (int)(_REQ._Grid_Size / _REQ._Leaf_Spacing);
            int _GridX = _Index % _GridWidth;
            int _GridZ = _Index / _GridWidth;
            float3 _Position = _Grid._Grid_Position + new float3
            {
                x = _GridX * _REQ._Leaf_Spacing,
                y = _RNG.NextFloat(-0.05f, 0.05f),
                z = _GridZ * _REQ._Leaf_Spacing
            };
            _ECB.AddComponent(_Index, _Leaf, new LocalTransform
            {
                Position = _Position,
                Rotation = quaternion.Euler(new float3(0, math.radians(_RNG.NextInt(0, 360)), 0)),
                Scale = _RNG.NextFloat(1 - _REQ._Leaf_SizeVariation, 1 + _REQ._Leaf_SizeVariation)
            });
            _ECB.AddComponent(_Index, _Leaf, new TAG_Leaf());
            _ECB.AddComponent(_Index, _Leaf, new TAG_Leaf_Spawned());
        }
    }
}
