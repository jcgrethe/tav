using System;
using UnityEngine;

public class Command
{

    public int commandNumber;
    private float horizontalMove;
    private float verticalMove;
    public float timestamp;
    private bool jump;
    private bool shoot;
    private bool crouch;
    public bool hasHit = false;
    public Shoot damage;
    public Quaternion quaternion;

    public float HorizontalMove => horizontalMove;

    public float VerticalMove => verticalMove;
    public bool Jump => jump;

    public bool Shoot => shoot;

    public bool Crouch => crouch;
    
    
    public Command()
    {
    }

    public Command(int commandNumber, float horizontalMove, float verticalMove, float timestamp
        , bool jump, bool shoot, bool crouch, Quaternion quaternion)
    {
        this.commandNumber = commandNumber;
        this.horizontalMove = horizontalMove;
        this.verticalMove = verticalMove;
        this.timestamp = timestamp;
        this.jump = jump;
        this.shoot = shoot;
        this.crouch = crouch;
        this.quaternion = quaternion;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutUInt(commandNumber);
        buffer.PutFloat(horizontalMove);
        buffer.PutFloat(verticalMove);
        buffer.PutBit(jump);
        buffer.PutBit(shoot);
        buffer.PutBit(crouch);
        buffer.PutBit(hasHit);
        if (hasHit)
        {
            damage.Serialize(buffer);
        }
        buffer.PutFloat(quaternion.x);
        buffer.PutFloat(quaternion.y);
        buffer.PutFloat(quaternion.z);
        buffer.PutFloat(quaternion.w);

    }
    
    
    public void Deserialize(BitBuffer buffer)
    {
        commandNumber = buffer.GetUInt();
        horizontalMove = buffer.GetFloat();
        verticalMove = buffer.GetFloat();
        jump = buffer.GetBit();
        shoot = buffer.GetBit();
        crouch = buffer.GetBit();
        hasHit = buffer.GetBit();
        if (hasHit)
        {
            damage = new Shoot();
            damage.Deserialize(buffer);
            
        }
        quaternion = new Quaternion(buffer.GetFloat(),buffer.GetFloat(),buffer.GetFloat(),buffer.GetFloat());

    }

    public bool isSendable()
    {
       // if (!up && !down && !space)
        //{
         //   return false;
        //}

        return true;
    }
}
