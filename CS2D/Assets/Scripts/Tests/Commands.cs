using UnityEngine;

public class Commands
{

    public int time;
    public bool up;
    public bool down;
    public bool space;

    public Commands(int time, bool up, bool down, bool space)
    {
        this.time = time;
        this.up = up;
        this.down = down;
        this.space = space;
    }

    public Commands()
    {
    }
    

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(time);
        buffer.PutBit(up);
        buffer.PutBit(down);
        buffer.PutBit(space);
    }
    
    public void Deserialize(BitBuffer buffer)
    {
        
        time = buffer.GetInt();
        up = buffer.GetBit();
        down = buffer.GetBit();
        space = buffer.GetBit();
    }
}
