
using System;

public class Shoot
{
  private String id;
  private int damage;

  public string Id => id;

  public int Damage => damage;

  public Shoot(string id, int damage)
  {
    this.id = id;
    this.damage = damage;
  }
  
  public Shoot() {}
  
  public void Serialize(BitBuffer buffer)
  {
    buffer.PutString(id);
    buffer.PutBits(damage, 0, 100);
  }
  
  public void Deserialize(BitBuffer buffer)
  {
    id = buffer.GetString();
    damage = buffer.GetBits(0, 100);
  }
}
