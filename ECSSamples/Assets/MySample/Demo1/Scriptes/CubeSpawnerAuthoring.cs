using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CubeSpawnerAuthoring : MonoBehaviour,IConvertGameObjectToEntity,IDeclareReferencedPrefabs
{
    public GameObject cube;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(cube);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CubeSpawnerComponent()
        {
            cube = conversionSystem.GetPrimaryEntity(this.cube),
        });
    }

}
