using System.Net.Sockets;
using System.Net;
using System.Text;
using DropDoosClient;
using System.Runtime.Serialization.Formatters.Binary;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("localhost");
IPAddress ipAddress = ipHostInfo.AddressList[0];
IPEndPoint ipEndPoint = new(ipAddress, 5252);

using Socket client = new(
    ipEndPoint.AddressFamily,
    SocketType.Stream,
    ProtocolType.Tcp);

while (true)
{
    // Send message.
    Packet packet = new() { command = "connect" };
    string json = JsonConvert.SerializeObject(packet);
    byte[] bytes = Encoding.Default.GetBytes(json);
    var test = Convert.ToBase64String(bytes);
    var package = Encoding.UTF8.GetBytes(test);
    _ = await client.SendAsync(package, SocketFlags.None);
    Console.WriteLine($"Socket client sent message: \"{packet}\"");

    // Receive ack.
    var buffer = new byte[1_024];
    var received = await client.ReceiveAsync(buffer, SocketFlags.None);
    var response = Encoding.UTF8.GetString(buffer, 0, received);
    if (response == "connect_ack")
    {
        Console.WriteLine(
            $"Socket client received connect_resp: \"{response}\"");
        break;
    }
    // Sample output:
    //     Socket client sent message: "Hi friends 👋!<|EOM|>"
    //     Socket client received acknowledgment: "<|ACK|>"
}

client.Shutdown(SocketShutdown.Both);