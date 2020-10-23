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

    //private bool isJumping;", ;
    private bool shooting;
    private bool crouch;
    private bool isWalking;
    private bool isWalkingBackward;
    private bool isWalkingRight;
    private bool isWalkingLeft;
    
    public PlayerEntity(GameObject playerGameObject, Command command, String id)
    {
        this.playerGameObject = playerGameObject;
        //isJumping = isJumping(characterController);
        shooting = IsShooting(command);
        crouch = IsCrouch(command);
        isWalking = VerticalMovePos(command);
        isWalkingBackward = VerticalMoveNeg(command);
        isWalkingRight = HorizontalMovePos(command);
        isWalkingLeft = HorizontalMoveNeg(command);
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
      
        buffer.PutBit(shooting);
        buffer.PutBit(crouch);
        buffer.PutBit(isWalking);
        buffer.PutBit(isWalkingBackward);
        buffer.PutBit(isWalkingRight);
        buffer.PutBit(isWalkingLeft);

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

        shooting = buffer.GetBit();
        crouch = buffer.GetBit();
        isWalking = buffer.GetBit();
        isWalkingBackward = buffer.GetBit();
        isWalkingRight = buffer.GetBit();
        isWalkingLeft = buffer.GetBit();
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
                var playerEntity = new PlayerEntity(previous.playerGameObject, next.id);
                playerEntity.position = playerEntity.position + Vector3.Lerp(previous.position, next.position, t);
                var deltaRot = Quaternion.Lerp(previous.rotation, next.rotation, t);
                var rot = new Quaternion();
                rot.x = previous.rotation.x + deltaRot.x;
                rot.w = previous.rotation.w + deltaRot.w;
                rot.y = previous.rotation.y + deltaRot.y;
                rot.z = previous.rotation.z + deltaRot.z;
                playerEntity.rotation = rot;
                playerEntity.playerGameObject = players[currentPlayer.Key];
                newEntities.Add(playerEntity.id, playerEntity);
            }
        }

        return newEntities;
    }

    public void Apply(Animator animator)
    {
        if (playerGameObject != null)
        {
            playerGameObject.transform.position = position;
            playerGameObject.transform.rotation = rotation;
            animator.SetBool("shooting", shooting);
            animator.SetBool("crouch", crouch);
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isWalkingBackward", isWalkingBackward);
            animator.SetBool("isWalkingRight", isWalkingRight);
            animator.SetBool("isWalkingLeft", isWalkingLeft);
        }
    }
    
}
