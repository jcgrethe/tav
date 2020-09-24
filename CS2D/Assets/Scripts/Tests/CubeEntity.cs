using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeEntity
{

    public String id;
    public Vector3 position;
    public Quaternion rotation;
    public GameObject cubeGameObject;

    public CubeEntity(GameObject cubeGameObject, String id)
    {
        this.cubeGameObject = cubeGameObject;
        this.id = id;
    }

    public CubeEntity()
    {
        
    }

    public void Serialize(BitBuffer buffer)
    {
        position = cubeGameObject.transform.position;
        rotation = cubeGameObject.transform.rotation;
        buffer.PutString(id);
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
    }
    
    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        rotation = new Quaternion();
        id = buffer.GetString();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        rotation.w = buffer.GetFloat();
        rotation.x = buffer.GetFloat();
        rotation.y = buffer.GetFloat();
        rotation.z = buffer.GetFloat();
        //Debug.Log(position);
        //Debug.Log(rotation);
    }

    public static Dictionary<String, CubeEntity> createInterpolated(Snapshot previousEntities, Snapshot nextEntities, float t, Dictionary<String, GameObject> players)
    {
        var newEntities = new Dictionary<string, CubeEntity>();
        foreach (var currentPlayer in previousEntities.cubeEntities)
        {
            var previous = currentPlayer.Value;
            var next = nextEntities.cubeEntities[previous.id];
            var cubeEntity = new CubeEntity(previous.cubeGameObject, next.id);
            cubeEntity.position = cubeEntity.position + Vector3.Lerp(previous.position, next.position, t);
            var deltaRot=  Quaternion.Lerp(previous.rotation, next.rotation, t);
            var rot = new Quaternion();
            rot.x = previous.rotation.x + deltaRot.x;
            rot.w = previous.rotation.w + deltaRot.w;
            rot.y = previous.rotation.y + deltaRot.y;
            rot.z = previous.rotation.z + deltaRot.z;
            cubeEntity.rotation = rot;
            Debug.Log("ARRANCA");
            foreach (var aux in players)
            {
                Debug.Log("KEY " + aux.Key);
                Debug.Log("VALUE " + aux.Value);
            }
            cubeEntity.cubeGameObject = players[currentPlayer.Key];
            newEntities.Add(cubeEntity.id, cubeEntity);
        }

        return newEntities;
    }

    public void Apply()
    {
        if (cubeGameObject != null)
        {
            cubeGameObject.transform.position = position;
            cubeGameObject.transform.rotation = rotation;
        }
    }
    
}
