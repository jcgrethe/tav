
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static SendUtil;

public class CsServer : MonoBehaviour
{
    
    private Channel channel;

    public int pps;
    private float accum;
    private Dictionary<String, GameObject> cubeServer;
    private Dictionary<String, int> lastCommand;
    private Dictionary<String, String> playerIps;
    private float accum3;
    public GameObject ServerPrefab;
    private Dictionary<String, int> lastSnapshot;

    private int pos = 648;

    private int clientPort = 9001;
    private int packetNumber = 0;

    public void Awake()
    {
        channel = new Channel(9000);
        cubeServer = new Dictionary<string, GameObject>();
        lastCommand = new Dictionary<string, int>();
        playerIps = new Dictionary<string, string>();
        lastSnapshot = new Dictionary<string, int>();

    }

    public void Update()
    {
        Packet packet; 
        while ((packet = channel.GetPacket()) != null)
        {

            switch (packet.buffer.GetEnum<MessageCsType.messagetype>(5))
            {
                case MessageCsType.messagetype.newPlayer:
                    NewPlayer(packet);
                    break;
                case MessageCsType.messagetype.input:
                    ReceiveInput(packet);
                    break;
                default:
                    break;
            }
        }
        
        
        
    }

    public void FixedUpdate()
    {
        accum += Time.deltaTime;
        UpdateClientWord();

    }


    private void NewPlayer(Packet packet)
    {
        //join player y send player alreeady in server
        Debug.Log("NEW PLAYER");
        var client = createPlayer(new Vector3(343.2f, 1109.8f, pos + 2 ));
        pos += 2;
        client.GetComponent<PlayerId>().Id = packet.buffer.GetString();
        client.name = client.GetComponent<PlayerId>().Id;
        Destroy(client.GetComponent<Animator>());
        client.transform.GetChild(1).gameObject.active = false;
        cubeServer.Add(client.name, client);
        lastCommand[client.name] = 0;
        lastSnapshot[client.name] = 0;

        //send new player to all clients
        foreach (var kv in playerIps)
        {
            var auxPacket = Packet.Obtain();
            auxPacket.buffer.PutEnum(MessageCsType.messagetype.ackJoin, 5);
            auxPacket.buffer.PutBits(1, 0, 50);
            auxPacket.buffer.PutString(client.name);
            auxPacket.buffer.Flush();
            Send(kv.Value, clientPort, channel, auxPacket);
        }
        
        playerIps[client.name] = packet.fromEndPoint.Address.ToString();

        
        //send ack and current players
        var packetToSend = Packet.Obtain();
        packetToSend.buffer.PutEnum(MessageCsType.messagetype.ackJoin, 5);
        packetToSend.buffer.PutUInt(cubeServer.Count - 1);
        foreach (var kv in cubeServer)
        {
            if (!kv.Key.Equals(client.name))
            {
                packetToSend.buffer.PutString(kv.Key);
            }
            
        }
        packetToSend.buffer.Flush();
        string serverIP = playerIps[client.name];
        Send(serverIP, clientPort, channel, packetToSend);
        
    }

    private void UpdateClientWord()
    {
        var snapshot = new Snapshot();
        //generate word
        foreach (var auxCubeEntity in cubeServer)
        {
            var cubeEntity = new CubeEntity(auxCubeEntity.Value, auxCubeEntity.Value.GetComponent<PlayerId>().Id);
            snapshot.Add(cubeEntity);
        }
        foreach (var kv in playerIps)
        {
            var auxPlayerId = kv.Key;	
            snapshot.packetNumber = lastSnapshot[auxPlayerId];
            lastSnapshot[auxPlayerId]++;
            //serialize
            var updatePacket = Packet.Obtain();
            updatePacket.buffer.PutEnum(MessageCsType.messagetype.updateWorld, 5);
            snapshot.Serialize(updatePacket.buffer);
            updatePacket.buffer.Flush();

            string serverIP = kv.Value;
            Send(serverIP, clientPort, channel, updatePacket);
        }

        packetNumber++;
    }

    private void ReceiveInput(Packet packet)
    {
        //receive input
        
        String id = packet.buffer.GetString();
        int quantity = packet.buffer.GetUInt();
        var player = cubeServer[id];
        var currentLastCommand = lastCommand[id];
        int max = currentLastCommand;
        for (int i = 0; i < quantity; i++){
            var commands = new Command();
            commands.Deserialize(packet.buffer);
            if (commands.commandNumber > currentLastCommand)
            {
                ExecuteCommand.Execute(commands, player, player.GetComponent<CharacterController>());
                max = commands.commandNumber;
            }
        }

        lastCommand[id] = max;

        //send ack
        var packet3 = Packet.Obtain();
        packet3.buffer.PutEnum(MessageCsType.messagetype.ackInput, 5);
        packet3.buffer.PutUInt(max);
        packet3.buffer.Flush();
        string serverIP = playerIps[id];
        Send(serverIP, clientPort, channel, packet3);
        
    }
    

    public GameObject createPlayer(Vector3 pos)
    {
        return Instantiate(ServerPrefab, pos, Quaternion.identity);
    }
    
}
