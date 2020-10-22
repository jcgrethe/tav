using UnityEngine;

public class ExecuteCommand
{

    private static float mouseSensitivity = 6;
    // gravity

    public static void Execute(Command command, GameObject client, CharacterController characterController)
    {
        
        float gravity = 18000f;
        float jumpSpeed = 3000;
        float verticalSpeed = 0;
        float horizontalMove = command.HorizontalMove;
        float verticalMove = command.VerticalMove;
        if (characterController.isGrounded  && command.Jump)
        {
            verticalSpeed = jumpSpeed;
        }
        verticalSpeed -= gravity * Time.deltaTime;
        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);
        Vector3 move = client.transform.forward * verticalMove + client.transform.right * horizontalMove;
        characterController.Move(300 * Time.deltaTime * move + gravityMove * Time.deltaTime);
        Rotate(client, command);
    }
    
    private static void Rotate(GameObject client, Command command)
    {
        client.transform.Rotate(0, command.HorizontalRotation * mouseSensitivity, 0);
    }
}
