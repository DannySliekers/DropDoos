using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DropDoosServer.Managers;
using DropDoosServer.Data;

namespace DropDoosServer;

internal class Server : IHostedService
{
    private readonly ILogger<Server> _logger;
    private readonly IPacketManager _packetManager;

    public Server(IPacketManager packetManager, ILogger<Server> logger)
    {
        _logger = logger;
        _packetManager = packetManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server starting up");
        IPEndPoint ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 5252);

        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(100);
        var handler = await listener.AcceptAsync();

        try
        {
            await Receive(handler, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while receiving");
        }
    }

    private async Task Receive(Socket handler, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new MemoryStream();
        while (!cancellationToken.IsCancellationRequested) 
        {
            var buffer = new byte[4096];
            var bytesReceived = await handler.ReceiveAsync(buffer, SocketFlags.None);
            var packetSize = BitConverter.ToInt32(buffer.Take(4).ToArray());
            stream.Write(buffer, 4, bytesReceived - 4);

            if (packetSize <= stream.Length)
            {
                var packet = Packet.ToPacket(stream.ToArray());
                stream.SetLength(0);
                var response = _packetManager.HandlePacket(packet);

                if (response != null)
                {
                    await Send(handler, response);
                }
            }
        }
    }

    private async Task Send(Socket handler, Packet packet)
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

            await handler.SendAsync(buffer);
            position += bytesLeft < 4092 ? bytesLeft : 4092;
        }

        _logger.LogInformation("Finished sending: {command}", packet.Command);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server shutting down");
        return Task.CompletedTask;
    }
}
