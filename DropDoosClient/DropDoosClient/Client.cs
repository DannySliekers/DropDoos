using DropDoosClient.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using File = DropDoosClient.Data.File;
using Newtonsoft.Json;

namespace DropDoosClient;

internal class Client : IHostedService, IDisposable
{
    private readonly ILogger<Client> _logger;
    private readonly PathOptions _config;
    private readonly Socket _client;
    private readonly IPEndPoint _endPoint;
    private Timer? _timer;
    private bool initCompleted;

    public Client(IOptions<PathOptions> config, ILogger<Client> logger)
    {
        _logger = logger;
        _endPoint = new(IPAddress.Parse("127.0.0.1"), 5252);
        _client = new(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _config = config.Value;
        initCompleted = false;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting client");
        _timer = new Timer(Sync, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        await _client.ConnectAsync(_endPoint);

        Packet connect = new() { Command = Command.Connect };
        await Send(connect);

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
        if (initCompleted)
        {
            _logger.LogInformation("Syncing client with server");
            var sync = new Packet() { Command = Command.Sync };
            await Send(sync);
        } else
        {
            _logger.LogInformation("Waiting for init to complete before syncing with server");
        }

    }

    private async Task Receive(CancellationToken cancellationToken)
    {
        using MemoryStream stream = new MemoryStream();
        while (!cancellationToken.IsCancellationRequested)
        {
            var buffer = new byte[4096];
            var bytesReceived = await _client.ReceiveAsync(buffer, SocketFlags.None);
            var packetSize = BitConverter.ToInt32(buffer.Take(4).ToArray());
            stream.Write(buffer, 4, bytesReceived - 4);

            if (packetSize <= stream.Length)
            {
                var response = Packet.ToPacket(stream.ToArray());
                stream.SetLength(0);
                await HandleResponse(response);
            }
        }
    }

    private async Task HandleResponse(Packet response)
    {
        try
        {
            switch (response.Command)
            {
                case Command.Connect_Resp:
                    await HandleInit();
                    break;
                case Command.Init_Resp:
                    initCompleted = true;
                    WriteToFiles(response);
                    break;
                case Command.Sync:
                    WriteToFiles(response);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while getting client files");
        }

    }

    private async Task Send(Packet packet)
    {
        var packetBytes = packet.ToByteArray();
        var packetSize = packetBytes.Length;
        var position = 0;

        while (position < packetBytes.Length)
        {
            var bytesLeft = packetSize - position;
            var buffer = new byte[4096];

            // add total packet size to buffer
            Array.Copy(BitConverter.GetBytes(packetSize), buffer, 4);

            // add packet content to buffer
            Array.Copy(packetBytes, position, buffer, 4, bytesLeft < 4092 ? bytesLeft : 4092);

            await _client.SendAsync(buffer);
            position += bytesLeft < 4092 ? bytesLeft : 4092;
        }

        _logger.LogInformation("Finished sending: {command}", packet.Command);
    }

    private void WriteToFiles(Packet response)
    {
        foreach (var field in response.optionalFields)
        {
            try
            {
                using FileStream fs = System.IO.File.Create(_config.ClientFolder + "\\" + field.Key);
                byte[] info = new UTF8Encoding(true).GetBytes(field.Value);
                fs.Write(info, 0, info.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while writing to file");
            }
        }
    }

    private List<File> GetClientFiles()
    {
        List<File> files = new List<File>();



        return files;
    }

    private async Task HandleInit()
    {
        string[] clientFolder = Directory.GetFiles(_config.ClientFolder);

        foreach (var file in clientFolder)
        {
            var position = 0;
            var fileSize = new FileInfo(file).Length;
            while (position < fileSize)
            {
                using MemoryStream memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    while (memoryStream.Length < 100_000_000 && memoryStream.Length < fileSize)
                    {
                        fileStream.Seek(position, SeekOrigin.Begin);
                        byte[] buffer = new byte[4096];
                        int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                        memoryStream.Write(buffer, 0, bytesRead);
                        position += bytesRead;
                    }

                    var fileToSend = new File()
                    {
                        Name = Path.GetFileName(file),
                        Content = memoryStream.ToArray(),
                        Size = new FileInfo(file).Length
                    };

                    var init = new Packet() { Command = Command.Init, File = fileToSend };
                    await Send(init);
                }
            }
        }
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
