using System.Net.Sockets;
using System.Net;
using DropDoosClient;
using System.Text;

var clientFolderPath = "D:\\DropDoos\\ClientMap";
var clientFolder = Directory.GetFiles(clientFolderPath);
IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("localhost");
IPAddress ipAddress = ipHostInfo.AddressList[0];
IPEndPoint ipEndPoint = new(ipAddress, 5252);

using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

await client.ConnectAsync(ipEndPoint);
Packet packet = new() { command = Command.Connect };
await client.SendAsync(packet.ToByteArray());
Console.WriteLine($"Socket client sent message: {packet}");
while (true)
{
    var buffer = new byte[7_000_000];
    await client.ReceiveAsync(buffer);
    var response = Packet.ToPacket(buffer);
    if (response.command == Command.Connect_Resp)
    {
        string uniqueid = response.optionalFields["unique_id"];
        Console.WriteLine($"Socket client received connect_resp: {response}, with unique id: {uniqueid}");

        var optionalFields = new Dictionary<string, string>();
        foreach (var file in clientFolder)
        {
            optionalFields.Add(Path.GetFileName(file), Convert.ToBase64String(File.ReadAllBytes(file)));
        }
        var init = new Packet () { command = Command.Init, optionalFields = optionalFields };
        await client.SendAsync(init.ToByteArray());
    }
    else if (response.command == Command.Init_Resp)
    {
        foreach (var field in response.optionalFields)
        {
            try
            {
                using FileStream fs = File.Create(clientFolderPath + "\\" + field.Key);
                byte[] info = new UTF8Encoding(true).GetBytes(field.Value);
                fs.Write(info, 0, info.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        break;
    }
}

client.Shutdown(SocketShutdown.Both);