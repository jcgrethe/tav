using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot
{
    public Dictionary<String, PlayerEntity> playerEntities;
    public int packetNumber;

    public Snapshot(int packetNumber)
    {
        this.packetNumber = packetNumber;
        playerEntities = new Dictionary<string, PlayerEntity>();
    }

    public Snapshot()
    {
        playerEntities = new Dictionary<string, PlayerEntity>();
    }

    public Snapshot(int packetNumber, Dictionary<String, PlayerEntity> playerEntities)
    {
        this.packetNumber = packetNumber;
        this.playerEntities = playerEntities;
    }

    public void Add(PlayerEntity playerEntity)
    {
        playerEntities.Add(playerEntity.id, playerEntity);
    }
    
    public void Serialize(BitBuffer buffer)
    {
        buffer.PutUInt(packetNumber);
        buffer.PutBits(playerEntities.Count, 0, 50);
        foreach (var playerEntity in playerEntities)
        {
            playerEntity.Value.Serialize(buffer);
        }
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        packetNumber = buffer.GetUInt();
        var quatity = buffer.GetBits(0, 50);
        for (int i = 0; i < quatity; i++)
        {
            var playerEntity = new PlayerEntity();
            playerEntity.Deserialize(buffer);
            playerEntities.Add(playerEntity.id, playerEntity);
        }
        
    }
    
    public static Snapshot CreateInterpolated(Snapshot previous, Snapshot next, float t, Dictionary<String, GameObject> players, String id)
    {
        var playersMap = PlayerEntity.createInterpolated(previous, next, t, players, id);
        return new Snapshot(-1, playersMap);
    }

    public void Apply()
    {
        foreach (var playerEntity in playerEntities)
        { 
            playerEntity.Value.Apply();
        }
    }

}
