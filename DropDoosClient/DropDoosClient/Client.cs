using DropDoosClient.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace DropDoosClient;

internal class Client : IHostedService, IDisposable
{
    private readonly ILogger<Client> _logger;
    private readonly PathOptions _config;
    private readonly Socket _client;
    private readonly IPEndPoint _endPoint;
    private Timer? _timer;

    public Client(IOptions<PathOptions> config, ILogger<Client> logger)
    {
        _logger = logger;
        _endPoint = new(IPAddress.Parse("127.0.0.1"), 5252);
        _client = new(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _config = config.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting client");
        _timer = new Timer(Sync, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        await _client.ConnectAsync(_endPoint);
        Packet packet = new() { command = Command.Connect };
        await _client.SendAsync(packet.ToByteArray());
        _logger.LogInformation("Socket client sent message: {packet}", packet);

        try
        {
            await Receive(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while receiving");
        }
    }

    private async void Sync(object? state)
    {
        _logger.LogInformation("Syncing client with server");
        var optionalFields = GetClientFiles();
        var sync = new Packet() { command = Command.Sync, optionalFields = optionalFields };

        await _client.SendAsync(sync.ToByteArray());
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
            else if (response.command == Command.Init_Resp || response.command == Command.Sync_Resp)
            {
                WriteToFiles(response);
            }
        }
    }

    private async Task HandleConnectResp(Packet response)
    {
        string uniqueid = response.optionalFields["unique_id"];
        Console.WriteLine($"Socket client received connect_resp: {response}, with unique id: {uniqueid}");

        var optionalFields = GetClientFiles();
        var init = new Packet() { command = Command.Init, optionalFields = optionalFields };

        await _client.SendAsync(init.ToByteArray());
    } 

    private void WriteToFiles(Packet response)
    {
        foreach (var field in response.optionalFields)
        {
            try
            {
                using FileStream fs = File.Create(_config.ClientFolder + "\\" + field.Key);
                byte[] info = new UTF8Encoding(true).GetBytes(field.Value);
                fs.Write(info, 0, info.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while writing to file");
            }
        }
    }

    private Dictionary<string, string> GetClientFiles()
    {
        var optionalFields = new Dictionary<string, string>();
        string[] clientFolder = Directory.GetFiles(_config.ClientFolder);


        try
        {
            foreach (var file in clientFolder)
            {
                optionalFields.Add(Path.GetFileName(file), Convert.ToBase64String(File.ReadAllBytes(file)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while getting client files");
        }

        return optionalFields;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Shutdown(SocketShutdown.Both);
        _client.Dispose();
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
