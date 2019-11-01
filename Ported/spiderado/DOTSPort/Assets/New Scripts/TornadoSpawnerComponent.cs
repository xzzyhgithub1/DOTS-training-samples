﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct Spawner : IComponentData
{
    // Fields you can modify in the editor.
    public Entity particlePrefab;
    
    public float gridX;
    public float gridY;
    public float gridZ;
}

[RequiresEntityConversion]
public class TornadoSpawnerComponent : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject prefab;
    public float3 grid;
    
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(prefab);
    }
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new Spawner
        {
            // Make an entity reference to the game object/prefab we got from the editor
            particlePrefab = conversionSystem.GetPrimaryEntity(prefab),
            
            gridX = grid.x,
            gridY = grid.y,
            gridZ = grid.z
        };
        dstManager.AddComponentData(entity, data);
        
    }
}