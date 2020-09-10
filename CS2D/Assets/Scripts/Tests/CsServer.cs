
using System.Net;
using UnityEngine;

public class CsServer
{
    
    private Channel channel;
    private Channel channel2;
    private Channel channel3;
    private int pps;
    private float accum;
    private GameObject cubeServer;
    private int packetNumber;
    public CsServer(Channel channel, Channel channel2, Channel channel3, int pps, GameObject cubeServer)
    {
        this.channel = channel;
        this.channel2 = channel2;
        this.channel3 = channel3;
        this.pps = pps;
        this.cubeServer = cubeServer;
    }

    public void UpdateServer()
    {
        accum += Time.deltaTime;
        
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
                    cubeServer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 2, Vector3.zero, ForceMode.Impulse);
                }
                if (commands.up)
                {
                    cubeServer.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 10, Vector3.zero, ForceMode.Impulse);
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
            channel.Send(packet3, remoteEp);
            packet3.Free();
        }
    }


    public void updateChannels(Channel channel2, Channel channel3)
    {
        this.channel2 = channel2;
        this.channel3 = channel3;
    }
}
