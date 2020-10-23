using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AnimatorStates;

public class PlayerEntity
{

    public String id;
    public Vector3 position;
    public Quaternion rotation;
    public GameObject playerGameObject;
    private Command command;

    public PlayerEntity(GameObject playerGameObject, Command command, String id)
    {
        this.playerGameObject = playerGameObject;
        //isJumping = isJumping(characterController);
        this.command = command;
        this.id = id;
    }
    
    public PlayerEntity(GameObject playerGameObject, String id)
    {
        this.playerGameObject = playerGameObject;
        //isJumping = isJumping(characterController);
        this.id = id;
    }
    
    

    public PlayerEntity()
    {
        
    }

    public void Serialize(BitBuffer buffer)
    {
        position = playerGameObject.transform.position;
        rotation = playerGameObject.transform.rotation;
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
    }
    
    public void SerializeWithCommand(BitBuffer buffer)
    {
        position = playerGameObject.transform.position;
        rotation = playerGameObject.transform.rotation;
        buffer.PutString(id);
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(rotation.w);
        buffer.PutFloat(rotation.x);
        buffer.PutFloat(rotation.y);
        buffer.PutFloat(rotation.z);
      
        command.Serialize(buffer);

    }
    
    public void DeserializeWithCommand(BitBuffer buffer) {
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

        command = new Command();
        command.Deserialize(buffer);
    }

    public static Dictionary<String, PlayerEntity> createInterpolated(Snapshot previousEntities, Snapshot nextEntities,
        float t, Dictionary<String, GameObject> players, String id)
    {
        var newEntities = new Dictionary<string, PlayerEntity>();
        foreach (var currentPlayer in previousEntities.playerEntities)
        {
            if (!currentPlayer.Key.Equals(id))
            {
                var previous = currentPlayer.Value;
                var next = nextEntities.playerEntities[previous.id];
                var playerEntity = new PlayerEntity(previous.playerGameObject, previous.command, next.id);
                playerEntity.position = playerEntity.position + Vector3.Lerp(previous.position, next.position, t);
                var deltaRot = Quaternion.Lerp(previous.rotation, next.rotation, t);
                var rot = new Quaternion();
                rot.x = previous.rotation.x + deltaRot.x;
                rot.w = previous.rotation.w + deltaRot.w;
                rot.y = previous.rotation.y + deltaRot.y;
                rot.z = previous.rotation.z + deltaRot.z;
                playerEntity.rotation = rot;
                if (players.ContainsKey(currentPlayer.Key))
                {
                    playerEntity.playerGameObject = players[currentPlayer.Key];
                }
                else
                {
                    Debug.LogError("KEY" + currentPlayer.Key);
                    //Debug.LogError("KEY" + currentPlayer.Key);

                }

                newEntities.Add(playerEntity.id, playerEntity);
            }
        }

        return newEntities;
    }

    public void Apply()
    {
        if (playerGameObject != null)
        {
            var animator = playerGameObject.GetComponent<Animator>();
            playerGameObject.transform.position = position;
            playerGameObject.transform.rotation = rotation;
            //animator.SetBool("isJumping", isJumping(characterController));
            animator.SetBool("shooting", IsShooting(command));
            animator.SetBool("crouch", IsCrouch(command));
            animator.SetBool("isWalking", VerticalMovePos(command));
            animator.SetBool("isWalkingBackward", VerticalMoveNeg(command));
            animator.SetBool("isWalkingRight", HorizontalMovePos(command));
            animator.SetBool("isWalkingLeft", HorizontalMoveNeg(command));
        }
    }
    
}
