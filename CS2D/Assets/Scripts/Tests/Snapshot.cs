using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot
{
    private CubeEntity cubeEntity;
    public int packetNumber;

    public Snapshot(int packetNumber, CubeEntity cubeEntity)
    {
        this.cubeEntity = cubeEntity;
        this.packetNumber = packetNumber;
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(packetNumber);
        cubeEntity.Serialize(buffer);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetInt();
        cubeEntity.Deserialize(buffer);
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t)
    {
        var cubeEntity = CubeEntity.createInterpolated(previous.cubeEntity, next.cubeEntity, t);
        return new Snapshot(-1, cubeEntity);
    }

    public void Apply()
    {
        cubeEntity.Apply();
    }

}
