using UnityEngine;

public class Command
{

    public int commandNumber;
    private float horizontalMove;
    private float verticalMove;
    public float timestamp;
    private float horizontalRotation;
    private bool jump;
    private bool shoot;
    
    public float HorizontalRotation => horizontalRotation;

    public float HorizontalMove => horizontalMove;

    public float VerticalMove => verticalMove;
    public bool Jump => jump;

    public bool Shoot => shoot;


    
    public Command()
    {
    }

    public Command(int commandNumber, float horizontalMove, float verticalMove, float timestamp,
        float horizontalRotation, bool jump, bool shoot)
    {
        this.commandNumber = commandNumber;
        this.horizontalMove = horizontalMove;
        this.verticalMove = verticalMove;
        this.timestamp = timestamp;
        this.horizontalRotation = horizontalRotation;
        this.jump = jump;
        this.shoot = shoot;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(commandNumber);
        buffer.PutFloat(horizontalMove);
        buffer.PutFloat(verticalMove);
        buffer.PutFloat(horizontalRotation);
        buffer.PutBit(jump);
        buffer.PutBit(shoot);

    }
    
    
    public void Deserialize(BitBuffer buffer)
    {
        commandNumber = buffer.GetInt();
        horizontalMove = buffer.GetFloat();
        verticalMove = buffer.GetFloat();
        horizontalRotation = buffer.GetFloat();
        jump = buffer.GetBit();
        shoot = buffer.GetBit();
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
