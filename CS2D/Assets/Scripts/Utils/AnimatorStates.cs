
using UnityEngine;

public class AnimatorStates
{
    public static bool VerticalMovePos(Command command)
    {
        return command.VerticalMove > 0;
    }
    
    public static bool VerticalMoveNeg(Command command)
    {
        return command.VerticalMove < 0;
    }
    
    public static bool HorizontalMovePos(Command command)
    {
        return command.HorizontalMove > 0;
    }
    
    public static bool HorizontalMoveNeg(Command command)
    {
        return command.VerticalMove < 0;
    }
    
    public static bool IsCrouch(Command command)
    {
        return command.Crouch;
    }
    
    public static bool IsShooting(Command command)
    {
        return command.Shoot;
    }
    
    public static bool isJumping(CharacterController cc)
    {
        return !cc.isGrounded;
    }
    
    
    
    
    
    
}
