using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Baker_Leaf : MonoBehaviour
{
    public List<GameObject> _GameObjects;

    class Baker : Baker<Baker_Leaf>
    {
        public override void Bake(Baker_Leaf _Authoring)
        {
            if (_Authoring._GameObjects.Count > 0)
            {
                // Create referential entity which will hold all prefabs
                Entity _REF_Entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<REF_Leaf>(_REF_Entity);

                // Add prefabs to referential
                for (int _GameObjectID = 0; _GameObjectID < _Authoring._GameObjects.Count; _GameObjectID++)
                {
                    GameObject _GameObject = _Authoring._GameObjects[_GameObjectID];
                    Entity _PrefabEntity = GetEntity(_GameObject, TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
                    AppendToBuffer(_REF_Entity, new REF_Leaf{_Prefab = _PrefabEntity});
                }
            }
        }
    }
}
