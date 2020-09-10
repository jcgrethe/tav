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

    public static List<CubeEntity> createInterpolated(List<CubeEntity> previousEntities, List<CubeEntity> nextEntities, float t, List<GameObject> players)
    {
        var newEntities = new List<CubeEntity>();
        for (int i = 0; i < previousEntities.Count ; i++)
        {
            var previous = previousEntities[i];
            var next = nextEntities[i];
            var cubeEntity = new CubeEntity(previous.cubeGameObject, next.id);
            cubeEntity.position = cubeEntity.position + Vector3.Lerp(previous.position, next.position, t);
            var deltaRot=  Quaternion.Lerp(previous.rotation, next.rotation, t);
            var rot = new Quaternion();
            rot.x = previous.rotation.x + deltaRot.x;
            rot.w = previous.rotation.w + deltaRot.w;
            rot.y = previous.rotation.y + deltaRot.y;
            rot.z = previous.rotation.z + deltaRot.z;
            cubeEntity.rotation = rot;
            foreach (var player in players)
            {
                if (player.GetComponent<ClientId>().Id.Equals(cubeEntity.id))
                {
                    cubeEntity.cubeGameObject = player;
                    break;
                }
            }
            newEntities.Add(cubeEntity);
        }

        return newEntities;
    }

    public void Apply()
    {
        cubeGameObject.transform.position = position;
        cubeGameObject.transform.rotation = rotation;
    }
    
}
