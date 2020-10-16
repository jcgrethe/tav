using UnityEngine;

public class Command
{

    public int commandNumber;
    private float horizontalMove;
    private float verticalMove;
    public float timestamp;
    private float horizontalRotation;

    public float HorizontalRotation => horizontalRotation;

    public float HorizontalMove => horizontalMove;

    public float VerticalMove => verticalMove;



    
    public Command()
    {
    }

    public Command(int commandNumber, float horizontalMove, float verticalMove, float timestamp, float horizontalRotation)
    {
        this.commandNumber = commandNumber;
        this.horizontalMove = horizontalMove;
        this.verticalMove = verticalMove;
        this.timestamp = timestamp;
        this.horizontalRotation = horizontalRotation;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(commandNumber);
        buffer.PutFloat(horizontalMove);
        buffer.PutFloat(verticalMove);
        buffer.PutFloat(horizontalRotation);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        commandNumber = buffer.GetInt();
        horizontalMove = buffer.GetFloat();
        verticalMove = buffer.GetFloat();
        horizontalRotation = buffer.GetFloat();
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
