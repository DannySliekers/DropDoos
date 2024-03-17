using System.Net.Sockets;
using System.Net;
using DropDoosServer;

IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("localhost");
IPAddress ipAddress = ipHostInfo.AddressList[0];
IPEndPoint ipEndPoint = new(ipAddress, 5252);

using Socket listener = new(
    ipEndPoint.AddressFamily,
    SocketType.Stream,
    ProtocolType.Tcp);

listener.Bind(ipEndPoint);
listener.Listen(100);

var handler = await listener.AcceptAsync();
PacketManager packetManager = new PacketManager();
while (true)
{
    // Receive message.
    var buffer = new byte[7_000_000];
    var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
    var packet = Packet.ToPacket(buffer);
    var response = packetManager.HandlePacket(packet);

    if (response != null)
    {
        await handler.SendAsync(response, 0);
    }
}