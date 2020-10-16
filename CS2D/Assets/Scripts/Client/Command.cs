using UnityEngine;

public class Command
{

    public int commandNumber;
    private float horizontalMove;

    public float HorizontalMove => horizontalMove;

    public float VerticalMove => verticalMove;

    private float verticalMove;
    public float timestamp;

    
    public Command()
    {
    }

    public Command(int commandNumber, float horizontalMove, float verticalMove, float timestamp)
    {
        this.commandNumber = commandNumber;
        this.horizontalMove = horizontalMove;
        this.verticalMove = verticalMove;
        this.timestamp = timestamp;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(commandNumber);
        buffer.PutFloat(horizontalMove);
        buffer.PutFloat(verticalMove);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        commandNumber = buffer.GetInt();
        horizontalMove = buffer.GetFloat();
        verticalMove = buffer.GetFloat();
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
