using DropDoosClient.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace DropDoosClient;

internal class Client : IHostedService
{
    private readonly ILogger<Client> _logger;
    private readonly Socket _client;
    private readonly IPEndPoint _endPoint;
    private readonly string[] _clientFolder;
    private const string CLIENT_FOLDER_PATH = "D:\\DropDoos\\ClientMap";

    public Client(ILogger<Client> logger)
    {
        _logger = logger;
        _endPoint = new(IPAddress.Parse("127.0.0.1"), 5252);

        _client = new(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        
        _clientFolder = Directory.GetFiles(CLIENT_FOLDER_PATH);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.ConnectAsync(_endPoint);
        Packet packet = new() { command = Command.Connect };
        await _client.SendAsync(packet.ToByteArray());
        _logger.LogInformation("Socket client sent message: {packet}", packet);
        var task = Task.Factory.StartNew(() =>
        {
            Receive(cancellationToken);
        });
    }

    private async Task Receive(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var buffer = new byte[7_000_000];
            await _client.ReceiveAsync(buffer);
            var response = Packet.ToPacket(buffer);
            if (response.command == Command.Connect_Resp)
            {
                await HandleConnectResp(response);
            }
            else if (response.command == Command.Init_Resp)
            {
                HandleInitResp(response);
            }
        }
    }

    private async Task HandleConnectResp(Packet response)
    {
        string uniqueid = response.optionalFields["unique_id"];
        Console.WriteLine($"Socket client received connect_resp: {response}, with unique id: {uniqueid}");

        var optionalFields = new Dictionary<string, string>();
        foreach (var file in _clientFolder)
        {
            optionalFields.Add(Path.GetFileName(file), Convert.ToBase64String(File.ReadAllBytes(file)));
        }
        var init = new Packet() { command = Command.Init, optionalFields = optionalFields };
        await _client.SendAsync(init.ToByteArray());
    } 

    private void HandleInitResp(Packet response)
    {
        foreach (var field in response.optionalFields)
        {
            try
            {
                using FileStream fs = File.Create(CLIENT_FOLDER_PATH + "\\" + field.Key);
                byte[] info = new UTF8Encoding(true).GetBytes(field.Value);
                fs.Write(info, 0, info.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while writing to file");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Shutdown(SocketShutdown.Both);
        _client.Dispose();
        return Task.CompletedTask;
    }
}
