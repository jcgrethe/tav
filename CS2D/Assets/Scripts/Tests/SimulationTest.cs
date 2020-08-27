﻿using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{

    private Channel channel;
    private Channel channel2;
    private Channel channel3;

    private float accum = 0f;
    private float accum2 = 0f;

    private float clientTime = 0f;
    public int pps = 10;
    public int requiredSnapshots = 3;
    private int packetNumber = 0;
    private bool clientPlaying = false;


    [SerializeField] private GameObject cubeServer;
    [SerializeField] private GameObject cubeClient;

    List<Snapshot> interpolationBuffer = new List<Snapshot>();
    List<Commands> commandServer = new List<Commands>();

    
    
    // Start is called before the first frame update
    void Start() {
        channel = new Channel(9000);
        channel2 = new Channel(9001);
        channel3 = new Channel(9002);
    }

    private void OnDestroy() {
        channel.Disconnect();
        channel2.Disconnect();
        channel3.Disconnect();
    }

    // Update is called once per frame
    void Update() {
        accum += Time.deltaTime;
        accum2 += Time.deltaTime;

        UpdateServer();
        UpdateClient();
    }

    private void UpdateServer()
    {
        //send position
        float sendRate = (1f / pps);
        if (accum >= sendRate)
        {
            packetNumber += 1;
            //serialize
            var packet = Packet.Obtain();
            var cubeEntity = new CubeEntity(cubeServer);
            var snapshot = new Snapshot(packetNumber, cubeEntity);
            snapshot.Serialize(packet.buffer);
            packet.buffer.Flush();

            string serverIP = "127.0.0.1";
            int port = 9000;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet, remoteEp);
            packet.Free();
            // Restart accum
            accum -= sendRate;
        }
        
        
        //receive input
        var packet2 = channel2.GetPacket();
        if (packet2 != null)
        {
            List<Commands> commandsReceived = new List<Commands>();
            int quantity = packet2.buffer.GetInt();
            for (int i = 0; i < quantity; i++){
                var commands = new Commands();
                commands.Deserialize(packet2.buffer);
                commandsReceived.Add(commands);
            }

            var packet3 = Packet.Obtain();
            int maxTime = 0;
            foreach (var currentCommand in commandsReceived)
            {
                if (currentCommand.time > maxTime)
                {
                    maxTime = currentCommand.time;
                }
                if (currentCommand.space)
                {
                    cubeServer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 2, Vector3.zero, ForceMode.Impulse);
                }
                if (currentCommand.up)
                {
                    cubeServer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 10, Vector3.zero, ForceMode.Impulse);
                }
            }

            //send ack
            packet3.buffer.PutInt(maxTime);
            packet3.buffer.Flush();
            string serverIP = "127.0.0.1";
            int port = 9002;
            var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
            channel.Send(packet3, remoteEp);
            packet3.Free();
        }
    }

    private void UpdateClient() {
        
        //delete from list
        var packet3 = channel3.GetPacket();
        if (packet3 != null)
        {
            var toDel = packet3.buffer.GetInt();
            var i = 0;
            while(commandServer.Count != 0)
            {
              if (commandServer[0].time <= toDel)
              {
                  commandServer.RemoveAt(0);
              }
              else
              {
                  break;
              }
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
            var cubeEntity = new CubeEntity(cubeClient);
            var snapshot = new Snapshot(-1, cubeEntity);
            var buffer = packet.buffer;

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
        var command = new Commands(packetNumber, Input.GetKeyDown(KeyCode.UpArrow), Input.GetKeyDown(KeyCode.DownArrow),
            Input.GetKeyDown(KeyCode.Space));
        commandServer.Add(command);
    }

    private void Interpolate() {
        var previousTime = (interpolationBuffer[0]).packetNumber * (1f/pps);
        var nextTime =  interpolationBuffer[1].packetNumber * (1f/pps);
        var t =  (clientTime - previousTime) / (nextTime - previousTime); 
        var interpolatedSnapshot = Snapshot.CreateInterpolated(interpolationBuffer[0], interpolationBuffer[1], t);
        interpolatedSnapshot.Apply();

        if(clientTime > nextTime) {
            interpolationBuffer.RemoveAt(0);
        }
    }
}