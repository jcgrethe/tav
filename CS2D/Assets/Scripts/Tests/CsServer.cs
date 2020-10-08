
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static SendUtil;

public class CsServer
{
    
    private Channel channel2;
    private Channel channel4;
    private Channel channel5;

    public int pps;
    private float accum;
    private Dictionary<String, GameObject> cubeServer;
    private Dictionary<String, int> lastCommand;
    private Dictionary<String, String> playerIps;
    private float accum3;
    private CsClient csClient;
    
    //TODO DELETE
    private List<GameObject> bots;
    private int quantityOnfIntitialPlayer = 5;

    public CsServer(Channel channel, Channel channel2, Channel channel3, Channel channel4, Channel channel5, int pps,  CsClient csClient)
    {
        //this.channel = channel;
        this.channel2 = channel2;
        //this.channel3 = channel3;
        this.channel4 = channel4;
        this.channel5 = channel5;
        this.pps = pps; 
        this.csClient = csClient;
        cubeServer = new Dictionary<string, GameObject>();
        lastCommand = new Dictionary<string, int>();
        bots = new List<GameObject>();
        playerIps = new Dictionary<string, string>();
        
        for (int i = 2; i < 2 + quantityOnfIntitialPlayer; i++)
        {
            var client = csClient.createServerCube(new Vector3(3 ,0.5f, i * 1.5f - 6));
            var otherPlayer = client;
            otherPlayer.GetComponent<CubeId>().Id = i.ToString();
            otherPlayer.name = i.ToString();
            cubeServer.Add(i.ToString(), client); 
            bots.Add(client);
        }
        
    }

    public void UpdateServer()
    {
        //salto de bots
        accum3 += Time.deltaTime;
        if (accum3 > 2)
        {
            for (int i = 0; i < quantityOnfIntitialPlayer; i++)
            {
                bots[i].GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 5, bots[i].transform.position, ForceMode.Impulse);
            }
            
            accum3 = 0;
        }
        
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
            var client = csClient.createServerCube(new Vector3(0, 0.5f,0));
            
            client.GetComponent<CubeId>().Id = packet4.buffer.GetString();
            client.name = client.GetComponent<CubeId>().Id;
            cubeServer.Add(client.name, client);
            lastCommand[client.name] = 0;
            playerIps[client.name] = packet4.fromEndPoint.Address.ToString();
            
            //send ack
            var packet = Packet.Obtain();
            packet.buffer.PutInt(quantityOnfIntitialPlayer);
            for (int i = 0; i < bots.Count; i++)
            {
                packet.buffer.PutString(bots[i].name);
            }
            packet.buffer.Flush();
            string serverIP = playerIps[client.name];
            int port = 9004;
            Send(serverIP, port, channel5, packet);
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel5.Send(packet, remoteEp);
            packet.Free();
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
                snapshot.packetNumber = lastCommand[auxPlayerId];
                
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
            int max = 0;
            String id = packet2.buffer.GetString();
            int quantity = packet2.buffer.GetInt();
            var realPlayer = cubeServer[id];
            var currentLastCommand = lastCommand[id];
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


    
}
