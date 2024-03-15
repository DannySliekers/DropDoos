using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using DropDoosServer;
using Newtonsoft.Json;

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
while (true)
{
    // Receive message.
    var buffer = new byte[1_024];
    var received = await handler.ReceiveAsync(buffer, SocketFlags.None);

    string base64 = Encoding.Default.GetString(buffer).Trim('\0');
    byte[] bytes = Convert.FromBase64String(base64);
    string json = Encoding.Default.GetString(bytes);
    var packet = JsonConvert.DeserializeObject<Packet>(json);
    
    if (packet.command.Equals("connect"))
    {
        Console.WriteLine(
            $"Socket server received message: {packet.command}");

        var ackMessage = "connect_ack";
        var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
        await handler.SendAsync(echoBytes, 0);
        Console.WriteLine(
            $"Socket server sent acknowledgment: \"{ackMessage}\"");

        break;
    }
}