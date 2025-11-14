using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SYS_Grid_Generate))]
partial struct SYS_Leaf_SetAnimation : ISystem
{
    EntityQuery _Query_Grids;
    EntityQuery _Query_Forces;
    ComponentLookup<LocalTransform> _Lookup_Transform;
    ComponentLookup<COMP_Leaf_Animation> _Lookup_Animation;
    ComponentLookup<COMP_Leaf_Realloc> _Lookup_Realloc;

    public void OnCreate(ref SystemState state)
    {
        _Query_Grids = state.EntityManager.CreateEntityQuery(typeof(COMP_Grid_Data));
        _Query_Forces = state.EntityManager.CreateEntityQuery(typeof(COMP_Force_Sphere));

        _Lookup_Transform = state.GetComponentLookup<LocalTransform>(true);
        _Lookup_Animation = state.GetComponentLookup<COMP_Leaf_Animation>(true);
        _Lookup_Realloc = state.GetComponentLookup<COMP_Leaf_Realloc>(true);

        state.RequireForUpdate<COMP_Force_Sphere>();
    }

    public void OnUpdate(ref SystemState state)
    {
        NativeArray<Entity> _ForceEntities = _Query_Forces.ToEntityArray(Allocator.TempJob);
        foreach(Entity _ForceEntity in _ForceEntities)
        {
            COMP_Force_Sphere _Force_Sphere = state.EntityManager.GetComponentData<COMP_Force_Sphere>(_ForceEntity);

            NativeArray<Entity> _Grids = _Query_Grids.ToEntityArray(Allocator.TempJob);
            _Lookup_Animation.Update(ref state);
            _Lookup_Transform.Update(ref state);
            _Lookup_Realloc.Update(ref state);

            EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);
            foreach (Entity _Grid in _Grids)
            {
                COMP_Grid_Data _Grid_Data = state.EntityManager.GetComponentData<COMP_Grid_Data>(_Grid);
                float3 _Offset = math.abs(_Force_Sphere._Position - _Grid_Data._Grid_Position);
                bool _IsInsideGrid = _Offset.x <= _Grid_Data._Grid_Size && _Offset.z <= _Grid_Data._Grid_Size;
                if (_IsInsideGrid == true)
                {
                    NativeArray<COMP_Grid_Leafs> _Leafs = state.EntityManager.GetBuffer<COMP_Grid_Leafs>(_Grid).ToNativeArray(Allocator.TempJob);

                    Job_Set_Animation _Job = new Job_Set_Animation
                    {
                        _ECB = _ECB.AsParallelWriter(),
                        _Leafs = _Leafs,
                        _Lookup_Animation = _Lookup_Animation,
                        _Lookup_Transform = _Lookup_Transform,
                        _Lookup_Realloc = _Lookup_Realloc,
                        _Force_Sphere = _Force_Sphere
                    };
                    _Job.Schedule(_Leafs.Length, 200).Complete();
                    _Leafs.Dispose();
                }
            }

            _ECB.Playback(state.EntityManager);
            _ECB.Dispose();
            _Grids.Dispose();

            state.EntityManager.DestroyEntity(_ForceEntity);
        }
        _ForceEntities.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }



    [BurstCompile]
    partial struct Job_Set_Animation : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> _Lookup_Transform;
        [ReadOnly] public ComponentLookup<COMP_Leaf_Animation> _Lookup_Animation;
        [ReadOnly] public ComponentLookup<COMP_Leaf_Realloc> _Lookup_Realloc;
        public NativeArray<COMP_Grid_Leafs> _Leafs;
        public COMP_Force_Sphere _Force_Sphere;

        public void Execute(int _Index)
        {
            Entity _Leaf = _Leafs[_Index]._Leaf;
            LocalTransform _Transform = _Lookup_Transform[_Leaf];
            float3 _Position_Leaf = _Transform.Position;

            float _Distance = math.distance(_Position_Leaf, _Force_Sphere._Position);
            if (_Distance > _Force_Sphere._Radius)
                return;
                
            if (_Lookup_Realloc.HasComponent(_Leaf))
            {
                COMP_Leaf_Realloc _Realloc = _Lookup_Realloc[_Leaf];
                if (_Transform.Position.Equals(_Realloc._Position))
                {
                    _Transform.Scale = _Realloc._Scale;
                    _ECB.SetComponent(_Index, _Leaf, _Transform);
                    _ECB.RemoveComponent<COMP_Leaf_Realloc>(_Index, _Leaf);
                }
                else
                    return;
            }
            
            Unity.Mathematics.Random _RNG = new Unity.Mathematics.Random((uint)(1 + _Index * 15678));
            float _Height = (1 + _RNG.NextFloat(-0.2f, 0.2f)) * _Force_Sphere._Strength * math.clamp(1.0f - (_Distance / _Force_Sphere._Radius), 0f, 1f);
            float3 _PushDirection = math.normalize(_Position_Leaf - _Force_Sphere._Position);
            _PushDirection.y = 0;
            if (_Lookup_Animation.HasComponent(_Leaf))
            {
                // Already have animation : update only if strength is superior
                COMP_Leaf_Animation _Animation = _Lookup_Animation[_Leaf];
                if (_Height > _Animation._Height)
                {
                    _Animation._Height = _Height;
                    _Animation._Duration = math.clamp((1 + _RNG.NextFloat(0f, 0.2f)) * _Height, 0f, 3f);
                    _Animation._Position_End = _Animation._Position_Start + _RNG.NextFloat(0f, 1.5f) * _Height * _PushDirection;
                    _ECB.SetComponent(_Index, _Leaf, _Animation);
                }
            }
            else
            {
                // Add new animation
                float _RotationSpeed = _RNG.NextFloat(0f, 50f);
                _ECB.AddComponent(_Index, _Leaf, new COMP_Leaf_Animation
                {
                    _Height = _Height,
                    _Timer = 0f,
                    _Duration = (1 + _RNG.NextFloat(0f, 0.2f)) * _Height,
                    _Position_Start = _Position_Leaf,
                    _Position_End = _Position_Leaf + _RNG.NextFloat(0f, 1.5f) * _Height * _PushDirection,
                    _Rotation_Start = _Transform.Rotation,
                    _RotationSpeed = new float3(
                        _RNG.NextFloat(-1f, 1f) * _RotationSpeed,
                        _RNG.NextFloat(-1f, 1f) * _RotationSpeed,
                        _RNG.NextFloat(-1f, 1f) * _RotationSpeed),
                    _LateralMoveFactor = 0.2f * _RNG.NextFloat(-1f, 1f)
                });
            }
        }
    }
}
