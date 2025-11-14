using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

public class MNGR_GroundDecors : MonoBehaviour
{
    [SerializeField] bool _Generate;
    [SerializeField] float2 _AreaSize;
    [SerializeField] float _Spacing;
    [SerializeField] float _Density;
    [SerializeField] [Range(0,100)] int _Ratio_Grass;
    [SerializeField] [Range(0,100)] int _Ratio_Rocks;
    [SerializeField] [Range(0,100)] int _Ratio_Mushrooms;
    [SerializeField][Range(0, 100)] int _Ratio_Flowers;

    void Update()
    {
        if (_Generate == true)
        {
            _Generate = false;
            Generate_GroundDecors();
        }
    }
    
    void Generate_GroundDecors()
    {
        EntityManager _EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity _REQ_Destroy = _EntityManager.CreateEntity();
        _EntityManager.SetName(_REQ_Destroy, "REQ_GroundDecors_Destroy");
        _EntityManager.AddComponentData(_REQ_Destroy, new REQ_GroundDecors_Destroy{});

        Entity _REQ_Generate = _EntityManager.CreateEntity();
        _EntityManager.SetName(_REQ_Generate, "REQ_GroundDecors_Spawn");
        REQ_GroundDecors_Spawn _REQ = new REQ_GroundDecors_Spawn
        {
            _AreaSize = _AreaSize,
            _Spacing = _Spacing,
            _Density = _Density,
            _Ratio_Grass = _Ratio_Grass,
            _Ratio_Flowers = _Ratio_Flowers,
            _Ratio_Mushrooms = _Ratio_Mushrooms,
            _Ratio_Rocks = _Ratio_Rocks
        };
        _EntityManager.AddComponentData(_REQ_Generate, _REQ);
    }
}
