using UnityEngine;
using static AnimatorStates;

public class ExecuteCommand
{

    private static float mouseSensitivity = 12;
    // gravity

    public static void Execute(Command command, GameObject client, CharacterController characterController)
    {
        float gravity = 18000f;
        float jumpSpeed = 3000;
        float verticalSpeed = 0;
        float speed = 250;

        float horizontalMove = command.HorizontalMove;
        float verticalMove = command.VerticalMove;
        if (characterController.isGrounded  && command.Jump)
        {
            verticalSpeed = jumpSpeed;
        }
        verticalSpeed -= gravity * Time.deltaTime;
        if (IsCrouch(command))
        {
            speed = speed / 2;
        }
        else if (IsShooting(command))
        {
            speed = speed / 1.5f;   
        }
        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);
        Vector3 move = client.transform.forward * verticalMove + client.transform.right * horizontalMove;
        characterController.Move(speed * command.deltaT * move + gravityMove * command.deltaT);
        //Rotate(client, command);
    }
    
    public static void Rotate(GameObject client, float horizontalRotation)
    {
        client.transform.Rotate(0, horizontalRotation * mouseSensitivity, 0);
    }
}
