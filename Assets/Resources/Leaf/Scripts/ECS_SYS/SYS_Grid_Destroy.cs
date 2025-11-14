using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct SYS_Grid_Destroy : ISystem
{
    EntityQuery _Query_REQs;
    EntityQuery _Query_Grids;

    public void OnCreate(ref SystemState state)
    {
        _Query_REQs = state.EntityManager.CreateEntityQuery(typeof(REQ_Grid_Destroy));
        _Query_Grids = state.EntityManager.CreateEntityQuery(typeof(COMP_Grid_Data));

        state.RequireForUpdate<REQ_Grid_Destroy>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeArray<Entity> _REQs = _Query_REQs.ToEntityArray(Allocator.Temp);
        NativeArray<Entity> _Grids = _Query_Grids.ToEntityArray(Allocator.Temp);
        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.TempJob);
        int _LoopPerBatch = 1000;
        foreach (Entity _REQ_Entity in _REQs)
        {
            REQ_Grid_Destroy _REQ = state.EntityManager.GetComponentData<REQ_Grid_Destroy>(_REQ_Entity);
            foreach (Entity _Grid in _Grids)
            {
                COMP_Grid_Data _GridData = state.EntityManager.GetComponentData<COMP_Grid_Data>(_Grid);
                NativeArray<COMP_Grid_Leafs> _Leafs = state.EntityManager.GetBuffer<COMP_Grid_Leafs>(_Grid).ToNativeArray(Allocator.TempJob);
                if (_GridData._Grid_ID == _REQ._Grid_ID || _REQ._Grid_ID == -1)
                {
                    Job_Grid_Destroy _Job = new Job_Grid_Destroy
                    {
                        _ECB = _ECB.AsParallelWriter(),
                        _Leafs = _Leafs,
                    };
                    _Job.Schedule(_Leafs.Length, _LoopPerBatch).Complete();
                    _Leafs.Dispose();
                    _ECB.DestroyEntity(_Grid);
                }
            }

            _ECB.DestroyEntity(_REQ_Entity);
        }

        _ECB.Playback(state.EntityManager);
        _ECB.Dispose();
        _Grids.Dispose();
        _REQs.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }

    [BurstCompile]
    partial struct Job_Grid_Destroy : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter _ECB;
        public NativeArray<COMP_Grid_Leafs> _Leafs;

        public void Execute(int _Index)
        {
            _ECB.DestroyEntity(_Index, _Leafs[_Index]._Leaf);
        }
    }
}
