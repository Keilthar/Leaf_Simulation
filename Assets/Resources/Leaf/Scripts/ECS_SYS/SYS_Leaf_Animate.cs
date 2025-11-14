using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SYS_Leaf_SetAnimation))]
partial struct SYS_Leaf_Animate : ISystem
{
    EntityQuery _Query_LeafAnimted;

    public void OnCreate(ref SystemState state)
    {
        _Query_LeafAnimted = state.EntityManager.CreateEntityQuery(
            typeof(COMP_Leaf_Animation),
            typeof(LocalTransform));

        state.RequireForUpdate<COMP_Leaf_Animation>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float _DeltaTime = SystemAPI.Time.DeltaTime;

        NativeArray<Entity> _Entities = _Query_LeafAnimted.ToEntityArray(Allocator.TempJob);
        NativeArray<COMP_Leaf_Animation> _COMP_Animations = _Query_LeafAnimted.ToComponentDataArray<COMP_Leaf_Animation>(Allocator.TempJob);
        NativeArray<LocalTransform> _Transforms = _Query_LeafAnimted.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);
        DynamicBuffer<REF_Leaf_Animation> _REF_Animations = SystemAPI.GetSingletonBuffer<REF_Leaf_Animation>();
        
        Job_Animate_Leafs _Job = new Job_Animate_Leafs
        {
            _ECB = _ECB.AsParallelWriter(),
            _REF_Animations = _REF_Animations,
            _Entities = _Entities,
            _COMP_Animations = _COMP_Animations,
            _Transforms = _Transforms,
            _DeltaTime = _DeltaTime
        };
        
        _Job.Schedule(_Entities.Length, 64).Complete();
        
        _ECB.Playback(state.EntityManager);
        _ECB.Dispose();
        _Entities.Dispose();
        _COMP_Animations.Dispose();
        _Transforms.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    partial struct Job_Animate_Leafs : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        [ReadOnly] public DynamicBuffer<REF_Leaf_Animation> _REF_Animations;
        [ReadOnly] public NativeArray<Entity> _Entities;
        [ReadOnly] public NativeArray<COMP_Leaf_Animation> _COMP_Animations;
        [ReadOnly] public NativeArray<LocalTransform> _Transforms;
        public float _DeltaTime;

        public void Execute(int _Index)
        {
            Entity _Leaf = _Entities[_Index];
            COMP_Leaf_Animation _COMP_Animation = _COMP_Animations[_Index];
            LocalTransform _Transform = _Transforms[_Index];

            _COMP_Animation._Timer += _DeltaTime;
            int _AnimationWaypointsCount = _REF_Animations.Length;
            int _AnimationWaypointID = (int)math.floor(_COMP_Animation._Timer / _COMP_Animation._Duration * _AnimationWaypointsCount);
            if (_COMP_Animation._Timer > _COMP_Animation._Duration || _AnimationWaypointID >= _AnimationWaypointsCount)
            {
                // End animation and prepare reallocation to starting place
                _Transform.Position = _COMP_Animation._Position_End;
                _ECB.SetComponent(_Index, _Leaf, _Transform);
        
                _ECB.RemoveComponent<COMP_Leaf_Animation>(_Index, _Leaf);
                _ECB.AddComponent(_Index, _Leaf, new COMP_Leaf_Realloc
                {
                    _Position = _COMP_Animation._Position_Start,
                    _Rotation = _COMP_Animation._Rotation_Start,
                    _Scale = _Transform.Scale,
                    _Timer = 0f,
                    _Duration = 5f
                });
            }
            else
            {
                // Animation of leaf
                float3 _Position_Direction = math.lerp(_COMP_Animation._Position_Start, _COMP_Animation._Position_End, _COMP_Animation._Timer / _COMP_Animation._Duration);
                float3 _Position_Height = _COMP_Animation._Height * new float3(0, _REF_Animations[_AnimationWaypointID]._Height, 0);
                float3 _Normal = math.cross(new float3(0,1,0), _COMP_Animation._Position_End - _COMP_Animation._Position_Start);
                float3 _Position_Lateral = _COMP_Animation._LateralMoveFactor * _COMP_Animation._Height * _REF_Animations[_AnimationWaypointID]._Fly * _Normal;
                _Transform.Position = _Position_Direction + _Position_Lateral + _Position_Height;
                
                float3 _Rotation = _COMP_Animation._RotationSpeed * _DeltaTime;
                quaternion _DeltaRotation = quaternion.Euler(math.radians(_Rotation));
                _Transform.Rotation = math.mul(_Transform.Rotation, _DeltaRotation);
                
                _ECB.SetComponent(_Index, _Leaf, _Transform);
                _ECB.SetComponent(_Index, _Leaf, _COMP_Animation);
            }
        }
    }
}
