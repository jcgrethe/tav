
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static MessageCsType;
using static SendUtil;
using Random = UnityEngine.Random;

public class CsServer : MonoBehaviour
{
    
    private Channel channel;

    public int maxPlayers = 6;  

    public int pps;
    private float accum;
    private Dictionary<String, GameObject> playerServer;
    private Dictionary<String, int> lastCommand;
    private Dictionary<String, String> playerIps;
    private Dictionary<String, int> kills;
    private float accum3;
    public GameObject ServerPrefab;
    private Dictionary<String, int> lastSnapshot;
    private Dictionary<String, Command> lastCommandObject;
    private int pos = 648;
    private Dictionary<String, float> timeToRespawn;

    private int clientPort = 9001;
    private int packetNumber = 0;
    private Dictionary<String, int> playersLife;
    public List<GameObject> spots;
    private bool win = false;
    private String winnerId;
    public void Awake()
    {
        channel = new Channel(9000);
        playerServer = new Dictionary<string, GameObject>();
        lastCommand = new Dictionary<string, int>();
        playerIps = new Dictionary<string, string>();
        lastSnapshot = new Dictionary<string, int>();
        lastCommandObject = new Dictionary<string, Command>();
        playersLife = new Dictionary<string, int>();
        timeToRespawn = new Dictionary<string, float>();
        kills = new Dictionary<string, int>();
    }

    public void Update()
    {
        if (win)
        {
            SendWin(winnerId);
            return;
        }
        Packet packet; 
        while ((packet = channel.GetPacket()) != null)
        {

            switch (packet.buffer.GetEnum<messagetype>(quantityOfMessages))
            {
                case messagetype.newPlayer:
                    Debug.Log("NEW");
                    NewPlayer(packet);
                    break;
                case messagetype.input:
                    ReceiveInput(packet);
                    break;
                //case messagetype.sendDamage:
                  //  ReceiveDamage(packet);
                   // break;
                default:
                    break;
            }
        }

        List<String> playersToRespawn = new List<string>();

        foreach (var player in playersLife)
        {
            if (player.Value <= 0)
            {
                timeToRespawn[player.Key] += Time.deltaTime;
                if (timeToRespawn[player.Key] > 5)
                {
                    playerServer[player.Key].transform.position = GetRandomSpot();
                    playersToRespawn.Add(player.Key);
                    timeToRespawn[player.Key] = 0;
                }
            }
        }

        foreach (var id in playersToRespawn)
        {
            playersLife[id] = 100;
        }
        
        
        
    }

    public void FixedUpdate()
    {
        accum += Time.deltaTime;
        UpdateClientWorld();

    }


    private void NewPlayer(Packet packet)
    {
        //if (playerServer.Count > maxPlayers) return;
        //join player y send player alreeady in server
        var client = createPlayer(GetRandomSpot());
        pos += 2;
        client.GetComponent<PlayerId>().Id = packet.buffer.GetString();
        client.name = client.GetComponent<PlayerId>().Id;
        Destroy(client.GetComponent<Animator>());
        client.transform.GetChild(1).gameObject.active = false;
        client.transform.GetChild(0).gameObject.active = false;
        playerServer.Add(client.name, client);
        lastCommand[client.name] = 0;
        lastSnapshot[client.name] = 0;
        playersLife[client.name] = 100;
        timeToRespawn[client.name] = 0;
        kills[client.name] = 0;
        //send new player to all clients
        foreach (var kv in playerIps)
        {
            var auxPacket = Packet.Obtain();
            auxPacket.buffer.PutEnum(messagetype.ackJoin, quantityOfMessages);
            auxPacket.buffer.PutBits(1, 0, 50);
            auxPacket.buffer.PutString(client.name);
            auxPacket.buffer.Flush();
            Send(kv.Value, clientPort, channel, auxPacket);
        }
        
        playerIps[client.name] = packet.fromEndPoint.Address.ToString();

        
        //send ack and current players
        var packetToSend = Packet.Obtain();
        packetToSend.buffer.PutEnum(messagetype.ackJoin, quantityOfMessages);
        packetToSend.buffer.PutBits(playerServer.Count - 1, 0, 50);
        foreach (var kv in playerServer)
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

    private Vector3 GetRandomSpot()
    {
        int i = 0;
        while (i < 100)
        {
            var posibleSpot = spots[Random.Range(0, 6)];
            if (posibleSpot.GetComponent<Spot>().IsSpotable())
            {
                return posibleSpot.transform.position;
            }

            i++;
        }

        return spots[Random.Range(0, 5)].transform.position;
    }

    private void UpdateClientWorld()
    {
        var snapshot = new Snapshot();
        //generate word
        foreach (var auxPlayerEntity in playerServer)
        {
            var auxCommand = 
                lastCommandObject.ContainsKey(auxPlayerEntity.Key) ? lastCommandObject[auxPlayerEntity.Key] : new Command();
            var playerEntity = new PlayerEntity(auxPlayerEntity.Value, auxCommand,
                auxPlayerEntity.Value.GetComponent<PlayerId>().Id, playersLife[auxPlayerEntity.Key]);
            snapshot.Add(playerEntity);
            
        }
        foreach (var kv in playerIps)
        {
            var auxPlayerId = kv.Key;
            snapshot.packetNumber = lastSnapshot[auxPlayerId];
            snapshot.life = playersLife[auxPlayerId];
            snapshot.life = playersLife[auxPlayerId];
            snapshot.kills = kills[auxPlayerId];
            snapshot.lastCommand = lastCommand[auxPlayerId];
            lastSnapshot[auxPlayerId]++;
            //serialize
            var updatePacket = Packet.Obtain();
            updatePacket.buffer.PutEnum(messagetype.updateWorld, quantityOfMessages);
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
        var player = playerServer[id];
        var currentLastCommand = lastCommand[id];
        int max = currentLastCommand;
        for (int i = 0; i < quantity; i++){
            var commands = new Command();
            commands.Deserialize(packet.buffer);
            if (commands.commandNumber >= currentLastCommand)
            {
                //Debug.Log("COMMANDO" + commands.commandNumber);
                //Debug.Log("SERVER" + commands.commandNumber );
                ExecuteCommand.Execute(commands, player, player.GetComponent<CharacterController>());
                player.transform.rotation = commands.quaternion;
                if (commands.hasHit)
                {
                    ReceiveDamage(commands.damage, id);
                }
                max = commands.commandNumber;
                lastCommandObject[id] = commands;
            }
            else
            {
                Debug.Log("currentLastCommand" + currentLastCommand);
                Debug.Log("NUMBER" + commands.commandNumber);
                Debug.Log("hasHit" + commands.hasHit);

            }
        }

        lastCommand[id] = max;

        //send ack
        var packet3 = Packet.Obtain();
        packet3.buffer.PutEnum(messagetype.ackInput, quantityOfMessages);
        packet3.buffer.PutUInt(max);
        packet3.buffer.Flush();
        string serverIP = playerIps[id];
        Send(serverIP, clientPort, channel, packet3);
        
    }

    private void ReceiveDamage(Shoot damage, String id)
    {
        
        if (playersLife[damage.Id] > 0)
        {
            playersLife[damage.Id] -= damage.Damage;
            if (playersLife[damage.Id] <= 0)
            {
                kills[id] = kills[id] + 1;
                if (kills[id] >= 2)
                {
                    winnerId = id;
                    SendWin(id);
                }
            }
        }
        Debug.Log("Receive damage to: " + damage.Id);
    }

    private void SendWin(string id)
    {
        foreach (var ip in playerIps)
        {
            var packet3 = Packet.Obtain();
            packet3.buffer.PutEnum(messagetype.win, quantityOfMessages);
            packet3.buffer.PutString(id);
            packet3.buffer.Flush();
            Send(ip.Value, clientPort, channel, packet3);
        }
    }

    public GameObject createPlayer(Vector3 pos)
    {
        return Instantiate(ServerPrefab, pos, Quaternion.identity);
    }

    void OnDestroy()
    {
        channel.Disconnect();
    }

}
