using System;
using System.Collections.Generic;
using UnityEngine;
using static AnimatorStates;
using static SendUtil;
using static ExecuteCommand;

using Random = UnityEngine.Random;

public class CsClient : MonoBehaviour
{

    private Channel channel;

    private float clientTime = 0f;
    public int pps = 100;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private int serverPort = 9000;
    public GameObject ClientPrefab;
    private GameObject client;
    public Material material;
    private GameObject conciliateGameObject;
    private Dictionary<String, GameObject> clients;
    private CharacterController characterController;
    private CharacterController conciliateCharacterController;
    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Command> commandServer = new List<Command>();
    private bool join = false;
    private bool waitJoin = true;
    public Transform cameraHolder;
    private float mouseSensitivity = 6f;
    public float upLimit = -50;
    public float downLimit = 50;
    public String serverIP = "192.168.1.137";
    private GameObject mainCamera;
    public GameObject cameraPrefab;
    private bool shooting;
    private bool crouch;


    private Animator animator;
    // Start is called before the first frame update
    void Start() {
        JoinPlayer();
    }

    private void OnDestroy() {
        channel.Disconnect();
    }

    public void Awake()
    {
        channel = new Channel(9001);

        clients = new Dictionary<string, GameObject>();
    }

    // Update is called once per frame

    private void JoinPlayer()
    {
        client = Instantiate(ClientPrefab, new Vector3(343.2f, 1209.8f, 650 ), Quaternion.identity);
        var id = RandomId();
        client.name = id;
        client.GetComponent<PlayerId>().Id = id;
        clients.Add(id, client);
        //client.GetComponent<MeshRenderer>().material = material;
        characterController = client.GetComponent<CharacterController>();
        animator = client.GetComponent<Animator>();
        
        GameObject main = Instantiate(cameraPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        main.transform.parent = client.transform;
        main.transform.localPosition = new Vector3(-0.14f, 2.29f, -6.02f);
        cameraHolder = main.GetComponent<Camera>().transform;
        
        //mainCamera = main;
        var packet4 = Packet.Obtain();
        packet4.buffer.PutEnum(MessageCsType.messagetype.newPlayer, 5);
        packet4.buffer.PutString(id);
        var player = new PlayerEntity(client, id);
        player.Serialize(packet4.buffer);
        packet4.buffer.Flush();

        Send(serverIP, serverPort, channel, packet4);
        
        conciliateGameObject = Instantiate(ClientPrefab, new Vector3(0, 0f, 0), Quaternion.identity);
        conciliateGameObject.name = id;
        conciliateGameObject.GetComponent<PlayerId>().Id = id;
        conciliateCharacterController = conciliateGameObject.GetComponent<CharacterController>();
        Destroy(conciliateGameObject.GetComponent<Animator>());
        conciliateCharacterController.transform.GetChild(1).gameObject.active = false;
        conciliateCharacterController.transform.GetChild(0).gameObject.active = false;


    }


    void Update() 
    {
        clientTime += Time.deltaTime;
        //remove old commands
        while(commandServer.Count != 0)
        {
            if (commandServer[0].timestamp < Time.time)
            {
                commandServer.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
        UpdateClient();
        InterpolateAndConciliate();
        
        if (Input.GetMouseButtonDown(0))
        {
            shooting = true;
        }
        if(Input.GetMouseButtonUp(0))
        {
            shooting = false;
        }
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            crouch = true;
        }
        if(Input.GetKeyUp(KeyCode.LeftControl))
        {
            crouch = false;
        }
    }

    public void FixedUpdate()
    {
        SendInput();
    }

    private void UpdateClient() 
    {
        Packet packet;
        while ((packet = channel.GetPacket()) != null)
        {
            switch (packet.buffer.GetEnum<MessageCsType.messagetype>(5))
            {
                case MessageCsType.messagetype.ackInput:
                    UpdateInterpolationBuffer(packet);
                    break;
                case MessageCsType.messagetype.ackJoin:
                    AwaitJoinGame(packet);
                    break;
                case MessageCsType.messagetype.updateWorld:
                    UpdateWord(packet);
                    break;
                default:
                    break;
            }
        }
    }

    private void SendInput()
    {
        ReadInput();
        if (commandServer.Count != 0)
        {
            var packet2 = Packet.Obtain();
            packet2.buffer.PutEnum(MessageCsType.messagetype.input, 5);
            packet2.buffer.PutString(client.name);
            packet2.buffer.PutUInt(commandServer.Count);
            foreach (var currentCommand in commandServer)
            {
                currentCommand.Serialize(packet2.buffer);
            }
            packet2.buffer.Flush();

            Send(serverIP, serverPort, channel, packet2);
        }
        
    }
    
    


    private void AwaitJoinGame(Packet packet)
    {
        var quan = packet.buffer.GetBits(0, 50);
        Debug.LogError(" TO JOIN " + quan);
        for (int i = 0; i < quan; i++)
        {
            var enemyClient = Instantiate(ClientPrefab, new Vector3(3, 0.5f, 0), Quaternion.identity);
            enemyClient.name =  packet.buffer.GetString();
            enemyClient.GetComponent<PlayerId>().Id = enemyClient.name;
            //enemyClient.GetComponent<MeshRenderer>().material = material;
            clients.Add(enemyClient.name, enemyClient);  
        }

        Debug.LogError("JOINED");
        join = true;
        
    }

    private void UpdateInterpolationBuffer(Packet packet)
    {
        var toDel = packet.buffer.GetUInt();
        while (commandServer.Count != 0)
        {
            if (commandServer[0].commandNumber <= toDel)
            {
                commandServer.RemoveAt(0);
            }
            else
            {
                break;
            }
        }
    }


    private void UpdateWord(Packet packet)
    {

        var buffer = packet.buffer;
        var snapshot = new Snapshot(-1);
        snapshot.Deserialize(buffer);
        
        int size = interpolationBuffer.Count;
        if((size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) && size < requiredSnapshots + 1 ) {
            interpolationBuffer.Add(snapshot);
        }
    }

    private void InterpolateAndConciliate()
    {
        while (interpolationBuffer.Count >= requiredSnapshots) {
            Interpolate();
            Conciliate();
        }
    }
    
    private void Interpolate() 
    {
        if (!join) return;
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t, clients, client.name);
        interpolatedSnapshot.Apply();

        if (clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Conciliate()
    {
        var auxClient = interpolationBuffer[interpolationBuffer.Count - 1].playerEntities[client.name];
        conciliateGameObject.transform.position = auxClient.position;
        conciliateGameObject.transform.rotation = auxClient.rotation;
        foreach (var auxCommand in commandServer)
        {
            Execute(auxCommand, conciliateGameObject, conciliateCharacterController);
        }

        var svPos = conciliateGameObject.transform.position;

        var yPos = Math.Abs(svPos.y - client.transform.position.y) > 4 ? svPos.y : client.transform.position.y; 
        var clientPos = new Vector3( svPos.x, yPos, svPos.z);

        client.transform.position = clientPos;
        client.transform.rotation = conciliateGameObject.transform.rotation;
    }

    private void ReadInput()
    {
        var timeout = Time.time + 2;
        Command command = new Command(packetNumber, Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical"), timeout,  Input.GetAxis("Mouse X"), 
            Input.GetKey(KeyCode.Space), shooting, crouch);
        commandServer.Add(command);
        packetNumber++;

        animator.SetBool("isJumping", isJumping(characterController));
        animator.SetBool("shooting", IsShooting(command));
        animator.SetBool("crouch", IsCrouch(command));
        animator.SetBool("isWalking", VerticalMovePos(command));
        animator.SetBool("isWalkingBackward", VerticalMoveNeg(command));
        animator.SetBool("isWalkingRight", HorizontalMovePos(command));
        animator.SetBool("isWalkingLeft", HorizontalMoveNeg(command));

        Execute(command, client, characterController);
        LocalCameraRotate();
    }




    private String RandomId()
    {
        Random.seed = System.DateTime.Now.Millisecond;
        var id = "";
        for(int i=0; i<10; i++)
        {
            id += Random.Range(0, 9).ToString();
        }

        return id;
    }

    private void LocalCameraRotate()
    {
        float verticalRotation = Input.GetAxis("Mouse Y");
        cameraHolder.Rotate(-verticalRotation*mouseSensitivity,0,0);
        Vector3 currentRotation = cameraHolder.localEulerAngles;
        if (currentRotation.x > 180) currentRotation.x -= 360;
        currentRotation.x = Mathf.Clamp(currentRotation.x, upLimit, downLimit);
        cameraHolder.localRotation = Quaternion.Euler(currentRotation);
    }
    

}