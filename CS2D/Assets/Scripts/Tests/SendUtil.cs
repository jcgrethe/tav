using System;
using System.Net;

public class SendUtil
{
    public static void Send(String serverIP, int port, Channel channel, Packet packet)
    {
        var remoteEp = new IPEndPoint(IPAddress.Parse(serverIP), port);
        channel.Send(packet, remoteEp);
        packet.Free();
    }
}
