
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class CsServer
{
    
    private Channel channel2;
    private Channel channel4;
    private Channel channel5;

    public int pps;
    private float accum;
    private Dictionary<String, GameObject> cubeServer;
    private int packetNumber;
    private float accum3;
    private SimulationTest simulationTest;
    private int quantityOnfIntitialPlayer = 2;
    
    //TODO DELETE
    private List<GameObject> bots;
    private GameObject realPlayer;
    
    
    public CsServer(Channel channel, Channel channel2, Channel channel3, Channel channel4, Channel channel5, int pps,  SimulationTest simulationTest)
    {
        //this.channel = channel;
        this.channel2 = channel2;
        //this.channel3 = channel3;
        this.channel4 = channel4;
        this.channel5 = channel5;
        this.pps = pps; 
        this.simulationTest = simulationTest;
        cubeServer = new Dictionary<string, GameObject>();
        bots = new List<GameObject>();
        
        for (int i = 2; i < 2 + quantityOnfIntitialPlayer; i++)
        {
            var client = simulationTest.createServerCube(new Vector3(3 ,0.5f, (i - 2) * 3));
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
        
        //join player y send player alreeady in server
        var packet4 = channel4.GetPacket();
        if (packet4 != null)
        {
            Debug.Log("JOIN");
            var client = simulationTest.createServerCube(new Vector3(0, 0.5f,0));
            client.GetComponent<CubeId>().Id = packet4.buffer.GetString();
            client.name = client.GetComponent<CubeId>().Id;
            cubeServer.Add(client.name, client);
            realPlayer = client;
            
            var packet = Packet.Obtain();
            packet.buffer.PutInt(quantityOnfIntitialPlayer);
            for (int i = 0; i < bots.Count; i++)
            {
                packet.buffer.PutString(bots[i].name);
            }
            packet.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9004;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel5.Send(packet, remoteEp);
            packet.Free();
        }
        
        accum += Time.deltaTime;
        
        //send position
        float sendRate = (1f / pps);
        if (accum >= sendRate)
        {
            packetNumber += 1;
            //serialize
            var packet = Packet.Obtain();
            var snapshot = new Snapshot(packetNumber);
            foreach (var auxCubeEntity in cubeServer)
            {
                var cubeEntity = new CubeEntity(auxCubeEntity.Value, auxCubeEntity.Value.GetComponent<CubeId>().Id);
                snapshot.Add(cubeEntity);
            }
            
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel2.Send(packet, remoteEp);
            packet.Free();
            // Restart accum
            accum -= sendRate;
        }
        
        
        //receive input
        Packet packet2;
        while ( (packet2 = channel2.GetPacket()) != null)
        {
            int max = 0;
            int quantity = packet2.buffer.GetInt();
            for (int i = 0; i < quantity; i++){
                var commands = new Commands();
                commands.Deserialize(packet2.buffer);
                if (commands.space)
                {
                    realPlayer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 2, Vector3.zero, ForceMode.Impulse);
                }
                if (commands.up)
                {
                    realPlayer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 10, Vector3.zero, ForceMode.Impulse);
                }

                max = commands.commandNumber;
            }

            //send ack
            var packet3 = Packet.Obtain();
            packet3.buffer.PutInt(max);
            packet3.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9002;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel2.Send(packet3, remoteEp);
            packet3.Free();
        }
    }


    public void updateChannels(Channel channel2, Channel channel3)
    {
        this.channel2 = channel2;
        //this.channel3 = channel3;
    }
}
