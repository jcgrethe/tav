using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = System.Random;
using static SendUtil;

public class CsClient : MonoBehaviour
{

    private Channel channel;
    private Channel channel2;
    private Channel channel3;
    private Channel channel4;
    private Channel channel5;

    private CsServer server;


    private float accum2 = 0f;
    private float accum3 = 0f;
    private float clientTime = 0f;
    public int pps = 100;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private bool clientPlaying = false;
    private bool connected = true;
    private int countSpace = 0;
    
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

    
    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000); //visual
        channel2 = new Channel(9001); // input
        channel3 = new Channel(9002); // ack input
        channel4 = new Channel(9003); // join
        channel5 = new Channel(9004); //ack join

        server = new CsServer(channel, channel2, channel3, channel4, channel5, pps, this);
        clients = new Dictionary<string, GameObject>();
        JoinPlayer();
    }

    private void OnDestroy() {
        channel.Disconnect();
        channel2.Disconnect();
        channel3.Disconnect();
        channel4.Disconnect();
    }

    // Update is called once per frame

    public void JoinPlayer()
    {
        client = Instantiate(ClientPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        client.name = "1";
        client.GetComponent<CubeId>().Id = "1";
        clients.Add("1", client);
        client.GetComponent<MeshRenderer>().material = material;
        var packet4 = Packet.Obtain();
        packet4.buffer.PutString("1");
        var cube = new CubeEntity(client, "1");
        cube.Serialize(packet4.buffer);
        packet4.buffer.Flush();

        string serverIP = "127.0.0.1";
        int port = 9003;
        Send(serverIP, port, channel, packet4);
        
        conciliateGameObject = Instantiate(ClientPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        conciliateGameObject.name = "1";
        conciliateGameObject.GetComponent<CubeId>().Id = "1";
        conciliateGameObject.GetComponent<MeshRenderer>().material = conciliateMaterial;
    }


    void Update() {
        
        
        accum2 += Time.deltaTime;
        if (connected)
        {
            server.UpdateServer();
        }
        UpdateClient();

    }

    private void UpdateClient() {

        //join and get quantity of players
        if (!join)
        {
            AwaitJoinGame();
            return;
        }

        UpdateInterpolationBuffer();

        //send input
        float sendRate = (1f / 100);
        if (accum2 >= sendRate)
        {
            ReadInput();
            
            var packet2 = Packet.Obtain();
            packet2.buffer.PutString(client.name);
            packet2.buffer.PutInt(commandServer.Count);
            foreach (var currentCommand in commandServer)
            {
                currentCommand.Serialize(packet2.buffer);
            }

            packet2.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9001;
            Send(serverIP, port, channel, packet2);
            accum2 -= sendRate;

        }
            
        UpdateWord();
    }
    
    


    private void AwaitJoinGame()
    {
        var packet5 = channel5.GetPacket();
        if (packet5 != null)
        {
            var quan = packet5.buffer.GetInt();
            for (int i = 0; i < quan; i++)
            {
                var enemyClient = Instantiate(ClientPrefab, new Vector3(3, 0.5f, 0), Quaternion.identity);
                enemyClient.name =  packet5.buffer.GetString();
                enemyClient.GetComponent<CubeId>().Id = enemyClient.name;
                enemyClient.GetComponent<MeshRenderer>().material = material;
                clients.Add(enemyClient.name, enemyClient);  
            }

            join = true;
        } 
    }

    private void UpdateInterpolationBuffer()
    {
        //delete from list
        Packet packet3; 
        while ( (packet3=channel3.GetPacket()) != null)
        {
            var toDel = packet3.buffer.GetInt();
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

    }


    private void UpdateWord()
    {
        //visual
        var packet = channel.GetPacket();
        if (packet != null) {
            var buffer = packet.buffer;
            var snapshot = new Snapshot(-1);
            snapshot.Deserialize(buffer);

            int size = interpolationBuffer.Count;
            if(size == 0 || snapshot.packetNumber > interpolationBuffer[size - 1].packetNumber) {
                interpolationBuffer.Add(snapshot);
            }
        }

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
        commandServer.Add(command);
        executeCommand(command, client);

        
        if (Input.GetKeyDown(KeyCode.D))
        {
            connected = false;
            channel2.Disconnect();
            channel3.Disconnect();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            connected = true;
            channel2 = new Channel(9001);
            channel3 = new Channel(9002);
            server.updateChannels(channel2, channel3);
        }

        packetNumber++;
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
    
    public GameObject createServerCube(Vector3 pos)
    {
        return Instantiate(ServerPrefab, pos, Quaternion.identity);
    }
}