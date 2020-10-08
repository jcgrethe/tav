using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot
{
    public Dictionary<String, CubeEntity> cubeEntities;
    public int packetNumber;

    public Snapshot(int packetNumber)
    {
        this.packetNumber = packetNumber;
        cubeEntities = new Dictionary<string, CubeEntity>();
    }

    public Snapshot()
    {
        cubeEntities = new Dictionary<string, CubeEntity>();
    }

    public Snapshot(int packetNumber, Dictionary<String, CubeEntity> cubeEntities)
    {
        this.packetNumber = packetNumber;
        this.cubeEntities = cubeEntities;
    }

    public void Add(CubeEntity cubeEntity)
    {
        cubeEntities.Add(cubeEntity.id, cubeEntity);
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(packetNumber);
        buffer.PutInt(cubeEntities.Count);
        foreach (var cubeEntity in cubeEntities)
        {
            cubeEntity.Value.Serialize(buffer);
        }
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetInt();
        var quatity = buffer.GetInt();
        for (int i = 0; i < quatity; i++)
        {
            var cubeEntity = new CubeEntity();
            cubeEntity.Deserialize(buffer);
            cubeEntities.Add(cubeEntity.id, cubeEntity);
        }
        
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t, Dictionary<String, GameObject> players, String id)
    {
        var cubes = CubeEntity.createInterpolated(previous, next, t, players, id);
        return new Snapshot(-1, cubes);
    }

    public void Apply()
    {
        foreach (var cubeEntity in cubeEntities)
        { 
            cubeEntity.Value.Apply();
        }
    }

}
