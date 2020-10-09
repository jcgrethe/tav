using UnityEngine;

public class Commands
{

    public int commandNumber;
    public bool up;
    public bool down;
    public bool space;
    public float timestamp;

    public Commands(int commandNumber, bool up, bool down, bool space)
    {
        this.commandNumber = commandNumber;
        this.up = up;
        this.down = down;
        this.space = space;
    }
    
    public Commands(int commandNumber, bool up, bool down, bool space, float timestamp)
    {
        this.commandNumber = commandNumber;
        this.up = up;
        this.down = down;
        this.space = space;
        this.timestamp = timestamp;
    }

    public Commands()
    {
    }
    

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(commandNumber);
        buffer.PutBit(up);
        buffer.PutBit(down);
        buffer.PutBit(space);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        commandNumber = buffer.GetInt();
        up = buffer.GetBit();
        down = buffer.GetBit();
        space = buffer.GetBit();
    }

    public bool isSendable()
    {
        if (!up && !down && !space)
        {
            return false;
        }

        return true;
    }
}
