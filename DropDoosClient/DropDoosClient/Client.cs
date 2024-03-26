using DropDoosClient.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using File = DropDoosClient.Data.File;

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
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesReceived = await _client.ReceiveAsync(buffer);
            var eomLength = Encoding.UTF8.GetBytes("||DropProto-EOM||").Length;
            var eomIndex = IndexOfEOM(buffer, eomLength);

            if (eomIndex > 0)
            {
                stream.Write(buffer[..eomIndex], 0, eomIndex);
                var packet = Packet.ToPacket(stream.ToArray());
                await HandleResponse(packet);
                stream.SetLength(0);
                stream.Write(buffer[(eomIndex + eomLength)..bytesReceived], 0, bytesReceived - (eomIndex + eomLength));


            }
            else if (bytesReceived > 0)
            {
                stream.Write(buffer[..bytesReceived], 0, bytesReceived);
            }
        }
    }

    private int IndexOfEOM(byte[] buffer, int eomLength)
    {
        //Native String.IndexOf() doesn't really work for the EOM so this is a custom one
        // basically starts searching in the buffer for the bytes 124 and 124 because thats what the EOM starts with
        // and then checks if the remainder of the 124 124 start matches the EOM bytes and returns the index
        // if not found returns -1
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == 124 && (i + eomLength) < buffer.Length && buffer[i + 1] == 124)
            {
                var potentialEomBytes = buffer[i..(i + eomLength)];
                var potentialEomString = Encoding.UTF8.GetString(potentialEomBytes);

                if (potentialEomString.Equals("||DropProto-EOM||"))
                {
                    return i;
                }
            }
        }

        return -1;
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
                    if (initCompleted)
                    {
                        WriteToFiles(response);
                    }
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
        var eom = Encoding.UTF8.GetBytes("||DropProto-EOM||");
        var totalPackage = new byte[packetBytes.Length + eom.Length];

        Array.Copy(packetBytes, 0, totalPackage, 0, packetBytes.Length);
        Array.Copy(eom, 0, totalPackage, packetBytes.Length, eom.Length);

        using var stream = new NetworkStream(_client);
        await stream.WriteAsync(totalPackage, 0, totalPackage.Length);

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

    private async Task HandleInit()
    {
        string[] clientFolder = Directory.GetFiles(_config.ClientFolder);

        foreach (var file in clientFolder)
        {
            long position = 0;
            var fileSize = new FileInfo(file).Length;
            while (position <= fileSize)
            {
                if (position == fileSize)
                {
                    break;
                }

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

                        if(bytesRead == 0)
                        {
                            break;
                        }
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

        initCompleted = true;
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
