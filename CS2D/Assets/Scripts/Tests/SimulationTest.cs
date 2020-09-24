using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Random = System.Random;

public class SimulationTest : MonoBehaviour
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
        var client = Instantiate(ClientPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
        client.name = "1";
        client.GetComponent<CubeId>().Id = "1";
        clients.Add("1", client);
        var packet4 = Packet.Obtain();
        packet4.buffer.PutString("1");
        var cube = new CubeEntity(client, "1");
        cube.Serialize(packet4.buffer);
        packet4.buffer.Flush();

        string serverIP = "127.0.0.1";
        int port = 9003;
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        channel.Send(packet4, remoteEp);
        packet4.Free();
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
            var packet5 = channel5.GetPacket();
            if (packet5 != null)
            {
                var quan = packet5.buffer.GetInt();
                Debug.Log("TO ADD: " + quan);
                for (int i = 0; i < quan; i++)
                {
                    var client = Instantiate(ClientPrefab, new Vector3(3, 0.5f, 0), Quaternion.identity);
                    client.name =  packet5.buffer.GetString();
                    client.GetComponent<CubeId>().Id = client.name;
                    clients.Add(client.name, client);  
                }

                join = true;
            } 
            return;
        }

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

        //send input
        float sendRate = (1f / 100);
        if (accum2 >= sendRate)
        {
            ReadInput();
            var packet2 = Packet.Obtain();
            packet2.buffer.PutInt(commandServer.Count);
            foreach (var currentCommand in commandServer)
            {
                currentCommand.Serialize(packet2.buffer);
            }

            packet2.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9001;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet2, remoteEp);
            packet2.Free();
            accum2 -= sendRate;

        }

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
        }
    }
    
    private void ReadInput()
    {
        var timeout = Time.time + 2;
        var command = new Commands(packetNumber, Input.GetKeyDown(KeyCode.UpArrow), Input.GetKeyDown(KeyCode.DownArrow),
            Input.GetKeyDown(KeyCode.Space), timeout);
        commandServer.Add(command);
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

    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t, clients);
        interpolatedSnapshot.Apply();

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }

    public GameObject createServerCube(Vector3 pos)
    {
        return Instantiate(ServerPrefab, pos, Quaternion.identity);
    }
}