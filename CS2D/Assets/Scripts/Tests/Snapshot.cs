using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot
{
    private List<CubeEntity> cubeEntities;
    public int packetNumber;

    public Snapshot(int packetNumber)
    {
        this.packetNumber = packetNumber;
        cubeEntities = new List<CubeEntity>();
    }
    
    public Snapshot(int packetNumber, List<CubeEntity> cubeEntities)
    {
        this.packetNumber = packetNumber;
        this.cubeEntities = cubeEntities;
    }

    public void Add(CubeEntity cubeEntity)
    {
        cubeEntities.Add(cubeEntity);
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(packetNumber);
        buffer.PutInt(cubeEntities.Count);
        foreach (var cubeEntity in cubeEntities)
        {
            cubeEntity.Serialize(buffer);
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
            cubeEntities.Add(cubeEntity);
        }
        
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t, List<GameObject> players)
    {
        var cubes = CubeEntity.createInterpolated(previous.cubeEntities, next.cubeEntities, t, players);
        return new Snapshot(-1, cubes);
    }

    public void Apply()
    {
        foreach (var cubeEntity in cubeEntities)
        { 
            cubeEntity.Apply();
        }
    }

}
