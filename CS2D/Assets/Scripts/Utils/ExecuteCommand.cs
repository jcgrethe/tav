using UnityEngine;

public class ExecuteCommand
{
    public static void Execute(Command command, Transform transform, CharacterController characterController)
    {
        Vector3 move = transform.forward * command.VerticalMove + transform.right * command.HorizontalMove;
        characterController.Move(6 * Time.deltaTime * move); //+ gravityMove * Time.deltaTime);


    }
}
