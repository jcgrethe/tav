
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static SendUtil;

public class CsServer : MonoBehaviour
{
    
    private Channel channel2;
    private Channel channel4;

    public int pps;
    private float accum;
    private Dictionary<String, GameObject> cubeServer;
    private Dictionary<String, int> lastCommand;
    private Dictionary<String, String> playerIps;
    private float accum3;
    public GameObject ServerPrefab;

    private int pos = -2;
    private int packetNumber = 0;
    private Dictionary<String, int> lastSnapshot;
    
    public void Awake()
    {
        channel2 = new Channel(9001);
        channel4 = new Channel(9003);
        cubeServer = new Dictionary<string, GameObject>();
        lastCommand = new Dictionary<string, int>();
        playerIps = new Dictionary<string, string>();
        lastSnapshot = new Dictionary<string, int>();

    }



    public void Update()
    {
        NewPlayer();
        
        accum += Time.deltaTime;
        
        UpdateClientWord();
        ReceiveInput();

    }

    private void NewPlayer()
    {
        //join player y send player alreeady in server
        var packet4 = channel4.GetPacket();
        if (packet4 != null)
        {
            var client = createServerCube(new Vector3(0, 0.5f, pos + 2 ));
            pos += 2;
            client.GetComponent<CubeId>().Id = packet4.buffer.GetString();
            client.name = client.GetComponent<CubeId>().Id;
            cubeServer.Add(client.name, client);
            lastCommand[client.name] = 0;
            lastSnapshot[client.name] = 0;
            //send new player to all clients
            foreach (var kv in playerIps)
            {
                var auxPacket = Packet.Obtain();
                auxPacket.buffer.PutInt(1);
                auxPacket.buffer.PutString(client.name);
                auxPacket.buffer.Flush();
                Send(kv.Value, 9004, channel4, auxPacket);
            }
            
            playerIps[client.name] = packet4.fromEndPoint.Address.ToString();

            
            //send ack and current players
            var packet = Packet.Obtain();
            packet.buffer.PutInt(cubeServer.Count - 1);
            foreach (var kv in cubeServer)
            {
                if (!kv.Key.Equals(client.name))
                {
                    packet.buffer.PutString(kv.Key);
                }
                
            }
            packet.buffer.Flush();
            string serverIP = playerIps[client.name];
            int port = 9004;
            Send(serverIP, port, channel4, packet);
        }
    }

    private void UpdateClientWord()
    {        
        //send position
        float sendRate = (1f / pps);
        if (accum >= sendRate)
        {
            var snapshot = new Snapshot();
            //generate word
            foreach (var auxCubeEntity in cubeServer)
            {
                var cubeEntity = new CubeEntity(auxCubeEntity.Value, auxCubeEntity.Value.GetComponent<CubeId>().Id);
                snapshot.Add(cubeEntity);
            }
            foreach (var kv in playerIps)
            {
                var auxPlayerId = kv.Key;	
                snapshot.packetNumber = lastSnapshot[auxPlayerId];
                lastSnapshot[auxPlayerId]++;
                //serialize
                var packet = Packet.Obtain();
                snapshot.Serialize(packet.buffer);
                packet.buffer.Flush();

                string serverIP = kv.Value;
                int port = 9000;
                Send(serverIP, port, channel2, packet);
                // Restart accum
            }
            accum -= sendRate;
        }

    }

    private void ReceiveInput()
    {
        //receive input
        Packet packet2;
        while ( (packet2 = channel2.GetPacket()) != null)
        {
            String id = packet2.buffer.GetString();
            int quantity = packet2.buffer.GetInt();
            var realPlayer = cubeServer[id];
            var currentLastCommand = lastCommand[id];
            var max = currentLastCommand;
            for (int i = 0; i < quantity; i++){
                var commands = new Commands();
                commands.Deserialize(packet2.buffer);
                if (commands.commandNumber > currentLastCommand)
                {
                    if (commands.space)
                    {
                        realPlayer.GetComponent<Rigidbody>()
                            .AddForceAtPosition(Vector3.up * 2, Vector3.zero, ForceMode.Impulse);
                    }

                    if (commands.up)
                    {
                        realPlayer.GetComponent<Rigidbody>()
                            .AddForceAtPosition(Vector3.up * 10, Vector3.zero, ForceMode.Impulse);
                    }

                    max = commands.commandNumber;
                }
            }

            lastCommand[id] = max;

            //send ack
            var packet3 = Packet.Obtain();
            packet3.buffer.PutInt(max);
            packet3.buffer.Flush();
            string serverIP = playerIps[id];
            int port = 9002;
            Send(serverIP,port, channel2, packet3);
        }
    }


    public void updateChannels(Channel channel2, Channel channel3)
    {
        this.channel2 = channel2;
        //this.channel3 = channel3;
    }

    public GameObject createServerCube(Vector3 pos)
    {
        return Instantiate(ServerPrefab, pos, Quaternion.identity);
    }
    
}
