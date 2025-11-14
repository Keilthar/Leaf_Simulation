using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Baker_GroundDecors : MonoBehaviour
{
    public GameObject _Prefab_Grass;
    public GameObject _Prefab_Flower;
    public GameObject _Prefab_Mushroom;
    public GameObject _Prefab_Rock;

    class Baker : Baker<Baker_GroundDecors>
    {
        public override void Bake(Baker_GroundDecors _Authoring)
        {
            // Create referential entity which will hold all prefabs
            Entity _REF_Entity = GetEntity(TransformUsageFlags.None);

            // Add prefabs to referential
            REF_GroundDecors_Prefabs _REF = new REF_GroundDecors_Prefabs
            {
                _Prefab_Grass = GetEntity(_Authoring._Prefab_Grass, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic),
                _Prefab_Flower = GetEntity(_Authoring._Prefab_Flower, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic),
                _Prefab_Mushroom = GetEntity(_Authoring._Prefab_Mushroom, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic),
                _Prefab_Rock = GetEntity(_Authoring._Prefab_Rock, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic)
            };
            AddComponent(_REF_Entity, _REF);
        }
    }
}

