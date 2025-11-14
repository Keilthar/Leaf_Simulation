using System.ComponentModel;
using System.Linq;
using NUnit.Framework.Constraints;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SYS_Leaf_SetAnimation))]
partial struct SYS_Leaf_GridReallocation : ISystem
{
    EntityQuery _Query_LeafRealloc;

    public void OnCreate(ref SystemState state)
    {
        _Query_LeafRealloc = state.EntityManager.CreateEntityQuery(
            typeof(COMP_Leaf_Realloc),
            typeof(LocalTransform));

        state.RequireForUpdate<COMP_Leaf_Realloc>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float _DeltaTime = SystemAPI.Time.DeltaTime;

        NativeArray<Entity> _Leaves = _Query_LeafRealloc.ToEntityArray(Allocator.TempJob);
        NativeArray<COMP_Leaf_Realloc> _Reallocs = _Query_LeafRealloc.ToComponentDataArray<COMP_Leaf_Realloc>(Allocator.TempJob);
        NativeArray<LocalTransform> _Transforms = _Query_LeafRealloc.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);

        Job_Grid_Realloc _Job = new Job_Grid_Realloc
        {
            _DeltaTime = _DeltaTime,
            _Leaves = _Leaves,
            _Reallocs = _Reallocs,
            _Transforms = _Transforms,
            _ECB = _ECB.AsParallelWriter()
        };
        _Job.Schedule(_Leaves.Length, 64, state.Dependency).Complete();

        _ECB.Playback(state.EntityManager);
        _ECB.Dispose();
        _Leaves.Dispose();
        _Reallocs.Dispose();
        _Transforms.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    partial struct Job_Grid_Realloc : IJobParallelFor
    {
        public NativeArray<Entity> _Leaves;
        public NativeArray<COMP_Leaf_Realloc> _Reallocs;
        public NativeArray<LocalTransform> _Transforms;
        public EntityCommandBuffer.ParallelWriter _ECB;
        public float _DeltaTime;

        public void Execute(int _Index)
        {
            Entity _Leaf = _Leaves[_Index];
            LocalTransform _LocalTransform = _Transforms[_Index];
            COMP_Leaf_Realloc _Realloc = _Reallocs[_Index];

            _Realloc._Timer += _DeltaTime;

            if (_Realloc._Timer >= _Realloc._Duration)
            {
                // Animation complete
                _LocalTransform.Position = _Realloc._Position;
                _LocalTransform.Scale = _Realloc._Scale;
                _ECB.SetComponent(_Index, _Leaf, _LocalTransform);
                _ECB.RemoveComponent<COMP_Leaf_Realloc>(_Index, _Leaf);
            }
            else
            {
                // Animation in progress
                float _RespawnDuration = 0.5f;
                float _SkrinkDuration = _Realloc._Duration - _RespawnDuration;
                if (_Realloc._Timer <= _SkrinkDuration) // Shrink
                    _LocalTransform.Scale = math.lerp(_Realloc._Scale, 0, _Realloc._Timer / _SkrinkDuration);
                else // Growth
                {
                    _LocalTransform.Position = _Realloc._Position;
                    _LocalTransform.Scale = math.lerp(0, _Realloc._Scale, (_Realloc._Timer - _SkrinkDuration) / _RespawnDuration);
                }

                _ECB.SetComponent(_Index, _Leaf, _LocalTransform);
                _ECB.SetComponent(_Index, _Leaf, _Realloc);
            }
        }
    }
}
