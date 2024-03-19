﻿using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

        var task = Task.Factory.StartNew(() =>
        {
            Listen(handler, cancellationToken);
        });
    }

    private async Task Listen(Socket handler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) 
        {
            var buffer = new byte[7_000_000];
            var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
            var packet = Packet.ToPacket(buffer);
            var response = _packetManager.HandlePacket(packet);

            if (response != null)
            {
                await handler.SendAsync(response, 0);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server shutting down");
        return Task.CompletedTask;
    }
}
