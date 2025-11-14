using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

partial struct SYS_GroundDecors_Destroy : ISystem
{
    EntityQuery _Query_REQs;

    public void OnCreate(ref SystemState state)
    {
        _Query_REQs = state.EntityManager.CreateEntityQuery(typeof(REQ_GroundDecors_Destroy));
        state.RequireForUpdate<REQ_GroundDecors_Destroy>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeArray<Entity> _REQs = _Query_REQs.ToEntityArray(Allocator.Temp);
        foreach (Entity _REQ in _REQs)
            state.EntityManager.DestroyEntity(_REQ);

        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);
        Job_GroundDecors_Destroy _Job = new Job_GroundDecors_Destroy
        {
            _ECB = _ECB.AsParallelWriter()
        };
        _Job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
        
        _ECB.Playback(state.EntityManager);
        _ECB.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    private partial struct Job_GroundDecors_Destroy : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        public void Execute([EntityIndexInQuery] int _Index,
                            Entity _Entity,
                            in TAG_GroundDecors _Tag)
        {
            _ECB.DestroyEntity(_Index, _Entity);
        }
    }    
}
