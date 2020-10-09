using System;
using System.Collections.Generic;
using UnityEngine;
using static SendUtil;
using Random = UnityEngine.Random;

public class CsClient : MonoBehaviour
{

    private Channel channel;

    private float accum2 = 0f;
    private float accum3 = 0f;
    private float clientTime = 0f;
    public int pps = 100;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private bool clientPlaying = false;
    private bool connected = true;
    private int countSpace = 0;
    private int serverPort = 9000;
    public GameObject ClientPrefab;
    public GameObject ServerPrefab;
    private GameObject client;
    public Material material;
    public Material conciliateMaterial;
    public GameObject conciliateGameObject;
    
    private Dictionary<String, GameObject> clients;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Commands> commandServer = new List<Commands>();
    private bool join = false;
    private bool waitJoin = true;

    private String serverIP = "192.168.0.11"; 
    
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

    public void JoinPlayer()
    {
        client = Instantiate(ClientPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        var id = RandomId();
        client.name = id;
        client.GetComponent<CubeId>().Id = id;
        clients.Add(id, client);
        client.GetComponent<MeshRenderer>().material = material;
        
        var packet4 = Packet.Obtain();
        packet4.buffer.PutEnum(MessageCsType.messagetype.newPlayer, 5);
        packet4.buffer.PutString(id);
        var cube = new CubeEntity(client, id);
        cube.Serialize(packet4.buffer);
        packet4.buffer.Flush();

        Send(serverIP, serverPort, channel, packet4);
        
        conciliateGameObject = Instantiate(ClientPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        conciliateGameObject.name = id;
        conciliateGameObject.GetComponent<CubeId>().Id = id;
        conciliateGameObject.GetComponent<MeshRenderer>().material = conciliateMaterial;
    }


    void Update() {
        
        
        accum2 += Time.deltaTime;
        
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



    }

    public void FixedUpdate()
    {
        if (!join) { return; }
        SendInput();
        InterpolateAndConciliate();

    }

    private void UpdateClient() {

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
            packet2.buffer.PutInt(commandServer.Count);
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
        var quan = packet.buffer.GetInt();
        for (int i = 0; i < quan; i++)
        {
            var enemyClient = Instantiate(ClientPrefab, new Vector3(3, 0.5f, 0), Quaternion.identity);
            enemyClient.name =  packet.buffer.GetString();
            enemyClient.GetComponent<CubeId>().Id = enemyClient.name;
            enemyClient.GetComponent<MeshRenderer>().material = material;
            clients.Add(enemyClient.name, enemyClient);  
        }

        Debug.Log("JOINED");
        join = true;
        
    }

    private void UpdateInterpolationBuffer(Packet packet)
    {
        var toDel = packet.buffer.GetInt();
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
        if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
            interpolationBuffer.Add(snapshot);
        }
    }

    private void InterpolateAndConciliate()
    {
        if (interpolationBuffer.Count >= requiredSnapshots) {
            clientPlaying = true;
        }
        else if (interpolationBuffer.Count <= 1) {
            clientPlaying = false;
        }
        if (clientPlaying) {
            clientTime += Time.deltaTime;
            Interpolate();
            Conciliate();
        }
    }
    
    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t, clients, client.name);
        interpolatedSnapshot.Apply();

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    private void Conciliate()
    {
        var auxClient = interpolationBuffer[interpolationBuffer.Count - 1].cubeEntities[client.name];
        conciliateGameObject.transform.position = auxClient.position;
        conciliateGameObject.transform.rotation = auxClient.rotation;
        foreach (var auxCommand in commandServer)
        {
            executeCommand(auxCommand, conciliateGameObject);
        }

        client.transform.position = conciliateGameObject.transform.position;
        client.transform.rotation = conciliateGameObject.transform.rotation;
    }

    private void ReadInput()
    {
        var timeout = Time.time + 2;
        var command = new Commands(packetNumber, Input.GetKeyDown(KeyCode.UpArrow), Input.GetKeyDown(KeyCode.DownArrow),
            Input.GetKeyDown(KeyCode.Space), timeout);
        if (command.isSendable())
        { 
            commandServer.Add(command);
            executeCommand(command, client);
            packetNumber++;
        }

    }

    private void executeCommand(Commands command, GameObject player)
    {

        if (command.space)
        {
            player.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 2, Vector3.zero, ForceMode.Impulse);
        }
        if (command.up)
        {
            player.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 10, Vector3.zero, ForceMode.Impulse);
        }

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
    

}