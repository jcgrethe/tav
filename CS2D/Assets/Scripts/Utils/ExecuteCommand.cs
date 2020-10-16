using UnityEngine;

public class ExecuteCommand
{

    private static float mouseSensitivity = 6;
    public static void Execute(Command command, GameObject client, CharacterController characterController)
    {
        Vector3 move = client.transform.forward * command.VerticalMove + client.transform.right * command.HorizontalMove;
        characterController.Move(6 * Time.deltaTime * move); //+ gravityMove * Time.deltaTime);
        Rotate(client, command);
    }
    
    private static void Rotate(GameObject client, Command command)
    {
        client.transform.Rotate(0, command.HorizontalRotation * mouseSensitivity, 0);
    }
}
