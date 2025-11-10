using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class MNGR_Leaf : MonoBehaviour
{
    public static MNGR_Leaf Singleton;

    [Header("Grid config")]
    public bool _GenerateGrids;
    [SerializeField] int2 _MapSize;
    [SerializeField] int _GridSize;
    [SerializeField][Range(0, 100)] float _LeafDensity;
    [SerializeField][Range(0.1f, 1f)] float _LeafSpacing;
    [SerializeField][Range(0f, 0.5f)] float _LeafSizeVariation;

    [Header("Leaf animation")]
    [SerializeField] public int _AnimMaxHeight;
    [SerializeField] public AnimationCurve _AnimHeight;
    [SerializeField] public AnimationCurve _AnimFly;

    [Header("UI")]
    [SerializeField] TMP_Text _TXT_FPS;
    float _DeltaTime;
    [SerializeField] TMP_Text _TXT_LeavesTotal;
    [SerializeField] TMP_Text _TXT_LeavesAnimated;


    void Awake()
    {
        Singleton = this;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 300;
        Display.main.Activate();
    }

    void Start()
    {
        Init_REF_Animation();
    }

    void Update()
    {
        Set_UI();

        if (_GenerateGrids == true)
        {
            _GenerateGrids = false;
            Generate_Leaves();
        }
    }

    public void Generate_Leaves()
    {
        Destroy_LeafsAll();
        int _XSize = _MapSize.x / _GridSize;
        int _ZSize = _MapSize.y / _GridSize;
        int _GridCount = _XSize * _ZSize;
        for (int _GridID = 0; _GridID < _GridCount; _GridID++)
        {
            int3 _Position = new int3
            {
                x = (int)(_GridID % _XSize * _GridSize),
                y = 0,
                z = (int)(_GridID / _XSize * _GridSize)
            };
            Genetate_Leafs(_Position, _GridID);
        } 
    }

    void Genetate_Leafs(int3 _Grid_Position, int _Grid_ID)
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity _REQ_Generate = _EntityManager.CreateEntity();
        _EntityManager.SetName(_REQ_Generate, "REQ_Leaf_Generate");
        _EntityManager.AddComponentData(_REQ_Generate, new REQ_Grid_Generate
        {
            _Grid_ID = _Grid_ID,
            _Grid_Position = _Grid_Position,
            _Grid_Size = _GridSize,

            _Leaf_Density = _LeafDensity,
            _Leaf_Spacing = _LeafSpacing,
            _Leaf_SizeVariation = _LeafSizeVariation
        });
    }

    void Destroy_Leafs(int _GridID)
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity _REQ_Destroy = _EntityManager.CreateEntity();
        _EntityManager.SetName(_REQ_Destroy, "REQ_Leaf_Destroy");
        _EntityManager.AddComponentData(_REQ_Destroy, new REQ_Grid_Destroy
        {
            _Grid_ID = _GridID
        });
    }

    void Destroy_LeafsAll()
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity _REQ_Destroy = _EntityManager.CreateEntity();
        _EntityManager.SetName(_REQ_Destroy, "REQ_Leaf_Destroy");
        _EntityManager.AddComponentData(_REQ_Destroy, new REQ_Grid_Destroy
        {
            _Grid_ID = -1
        });
    }

    void Set_UI()
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery _LeavesCount = _EntityManager.CreateEntityQuery(typeof(TAG_Leaf));
        _TXT_LeavesTotal.text = $"Leaves (Total) : {_LeavesCount.CalculateEntityCount()}";

        EntityQuery _LeavesAnimated = _EntityManager.CreateEntityQuery(typeof(COMP_Leaf_Animation));
        _TXT_LeavesAnimated.text = $"Leaves (Animated) : {_LeavesAnimated.CalculateEntityCount()}";

        _DeltaTime += (Time.unscaledDeltaTime - _DeltaTime) * 0.1f;
        int _FPS = (int)math.round(1f / _DeltaTime);
        _TXT_FPS.text = $"FPS : {_FPS}";
    }

    void Init_REF_Animation()
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        
        // Create entity container
        Entity _REF_Entity = _EntityManager.CreateEntity();
        _EntityManager.SetName(_REF_Entity, "REF_Leaf_Animation");
        _EntityManager.AddBuffer<REF_Leaf_Animation>(_REF_Entity);

        // Convert animation curve into an array stored in component buffer
        EntityCommandBuffer _ECB = new EntityCommandBuffer(Allocator.Temp);
        int _ArraySize = 50;
        for (int _ID = 0; _ID < _ArraySize; _ID++)
        {
            float _Step = (float)_ID / (_ArraySize - 1);
            _ECB.AppendToBuffer(_REF_Entity, new REF_Leaf_Animation
            {
                _Height = MNGR_Leaf.Singleton._AnimHeight.Evaluate(_Step),
                _Fly = MNGR_Leaf.Singleton._AnimFly.Evaluate(_Step)
            });
        }
        _ECB.Playback(_EntityManager);
        _ECB.Dispose();
    }
    
}
