using System.Net.Sockets;
using System.Net;
using DropDoosClient;

IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("localhost");
IPAddress ipAddress = ipHostInfo.AddressList[0];
IPEndPoint ipEndPoint = new(ipAddress, 5252);

using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

await client.ConnectAsync(ipEndPoint);
while (true)
{
    // Send message.
    Packet packet = new() { command = Command.Connect };
    await client.SendAsync(packet.ToByteArray(), SocketFlags.None);
    Console.WriteLine($"Socket client sent message: \"{packet}\"");

    // Receive ack.
    var buffer = new byte[1_024];
    await client.ReceiveAsync(buffer, SocketFlags.None);
    var response = Packet.ToPacket(buffer);
    if (response.command == Command.Connect_Resp)
    {
        Console.WriteLine($"Socket client received connect_resp: \"{response}\"");
        break;
    }
}

client.Shutdown(SocketShutdown.Both);