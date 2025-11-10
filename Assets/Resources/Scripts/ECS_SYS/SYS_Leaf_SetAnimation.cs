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
    float3 _MousePositionKO;
    EntityQuery _Query_Grids;
    ComponentLookup<LocalTransform> _Lookup_Transform;
    ComponentLookup<COMP_Leaf_Animation> _Lookup_Animation;
    ComponentLookup<COMP_Leaf_Realloc> _Lookup_Realloc;

    public void OnCreate(ref SystemState state)
    {
        _MousePositionKO = new float3(-999, -999, -999);
        _Query_Grids = state.EntityManager.CreateEntityQuery(typeof(COMP_Grid_Data));
        _Lookup_Transform = state.GetComponentLookup<LocalTransform>(true);
        _Lookup_Animation = state.GetComponentLookup<COMP_Leaf_Animation>(true);
        _Lookup_Realloc = state.GetComponentLookup<COMP_Leaf_Realloc>(true); 
    }

    public void OnUpdate(ref SystemState state)
    {
        if (Mouse.current.leftButton.isPressed == false)
            return;

        float3 _MousePosition = Get_MousePosition();
        if (_MousePosition.Equals(_MousePositionKO))
            return;

        NativeArray<Entity> _Grids = _Query_Grids.ToEntityArray(Allocator.TempJob);
        _Lookup_Animation.Update(ref state);
        _Lookup_Transform.Update(ref state);
        _Lookup_Realloc.Update(ref state);

        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);
        foreach (Entity _Grid in _Grids)
        {
            COMP_Grid_Data _Grid_Data = state.EntityManager.GetComponentData<COMP_Grid_Data>(_Grid);
            float3 _Offset = math.abs(_MousePosition - _Grid_Data._Grid_Position);
            bool _IsInsideGrid = _Offset.x <= _Grid_Data._Grid_Size && _Offset.z <= _Grid_Data._Grid_Size;
            if (_IsInsideGrid == true)
            {
                NativeArray<COMP_Grid_Leafs> _Leafs = state.EntityManager.GetBuffer<COMP_Grid_Leafs>(_Grid).ToNativeArray(Allocator.TempJob);

                Job_Grid_Destroy _Job = new Job_Grid_Destroy
                {
                    _ECB = _ECB.AsParallelWriter(),
                    _Leafs = _Leafs,
                    _Lookup_Animation = _Lookup_Animation,
                    _Lookup_Transform = _Lookup_Transform,
                    _Lookup_Realloc = _Lookup_Realloc,
                    _Position_Target = _MousePosition
                };
                _Job.Schedule(_Leafs.Length, 200).Complete();
                _Leafs.Dispose();
            }
        }

        _ECB.Playback(state.EntityManager);
        _ECB.Dispose();
        _Grids.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    float3 Get_MousePosition()
    {
        Vector2 _MousePos = Mouse.current.position.ReadValue();
        float3 _MousePosition = new float3(_MousePos.x, _MousePos.y, 0);
        Camera _Camera = Camera.main;
        Ray _Ray = _Camera.ScreenPointToRay(_MousePosition);
        Plane _Plane = new Plane(Vector3.up, Vector3.zero);
        float3 _Position = _MousePositionKO;
        if (_Plane.Raycast(_Ray, out float _Distance))
            _Position = _Ray.GetPoint(_Distance);
        return _Position;
    }

    [BurstCompile]
    partial struct Job_Grid_Destroy : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        [ReadOnly] public ComponentLookup<LocalTransform> _Lookup_Transform;
        [ReadOnly] public ComponentLookup<COMP_Leaf_Animation> _Lookup_Animation;
        [ReadOnly] public ComponentLookup<COMP_Leaf_Realloc> _Lookup_Realloc;
        public NativeArray<COMP_Grid_Leafs> _Leafs;
        public float3 _Position_Target;

        public void Execute(int _Index)
        {
            Entity _Leaf = _Leafs[_Index]._Leaf;
            LocalTransform _Transform = _Lookup_Transform[_Leaf];
            float3 _Position_Leaf = _Transform.Position;

            float _Distance = math.distance(_Position_Leaf, _Position_Target);
            if (_Distance > 2)
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
            float _Height = (5.0f + _RNG.NextFloat(-0.5f, 0.5f)) * math.clamp(1.0f - (_Distance / 2.0f), 0f, 1f);
            float3 _PushDirection = math.normalize(_Position_Leaf - _Position_Target);
            if (_Lookup_Animation.HasComponent(_Leaf))
            {
                COMP_Leaf_Animation _Animation = _Lookup_Animation[_Leaf];
                if (_Height > _Animation._Height)
                {
                    _Animation._Height = _Height;
                    _Animation._Duration = (1 + _RNG.NextFloat(0f, 0.2f)) * _Height;
                    _Animation._Position_End = _Animation._Position_Start + _RNG.NextFloat(0f, 1.5f) * _Height * _PushDirection;
                    _ECB.SetComponent(_Index, _Leaf, _Animation);
                }
            }
            else
            {
                _ECB.AddComponent(_Index, _Leaf, new COMP_Leaf_Animation
                {
                    _Height = _Height,
                    _Timer = 0f,
                    _Duration = 0.5f * _Height + _RNG.NextFloat(-0.1f, 0.2f),
                    _Position_Start = _Position_Leaf,
                    _Position_End = _Position_Leaf + _RNG.NextFloat(0f, 1.5f) * _Height * _PushDirection
                });
            }
        }
    }
}
