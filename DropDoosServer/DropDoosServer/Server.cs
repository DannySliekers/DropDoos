﻿using System.Net.Sockets;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DropDoosServer.Managers;
using DropDoosServer.Data;
using System.Text;
using DropDoosServer.Queue;

namespace DropDoosServer;

internal class Server : IHostedService
{
    private readonly ILogger<Server> _logger;
    private readonly IPacketManager _packetManager;
    private readonly IDownloadQueue _downloadQueue;

    public Server(IPacketManager packetManager, ILogger<Server> logger, IDownloadQueue downloadQueue)
    {
        _logger = logger;
        _packetManager = packetManager;
        _downloadQueue = downloadQueue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server starting up");
        IPEndPoint ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 5252);

        using Socket listener = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(ipEndPoint);
        listener.Listen(100);
        var handler = await listener.AcceptAsync();

        Task.Factory.StartNew(() => HandleDownloadQueue(handler));

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
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested) 
        {
            var bytesReceived = await handler.ReceiveAsync(buffer);
            var eomLength = Encoding.UTF8.GetBytes("||DropProto-EOM||").Length;
            var eomIndex = IndexOfEOM(buffer, eomLength);

            if (eomIndex > 0)
            {
                stream.Write(buffer[..eomIndex], 0, eomIndex);
                var packet = Packet.ToPacket(stream.ToArray());
                var response = _packetManager.HandlePacket(packet);
                stream.SetLength(0);
                stream.Write(buffer[(eomIndex + eomLength)..bytesReceived], 0, bytesReceived - (eomIndex + eomLength));

                if (response != null)
                {
                    await Send(handler, response);
                }
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

    private async Task Send(Socket handler, Packet packet)
    {
        var packetBytes = packet.ToByteArray();
        var eom = Encoding.UTF8.GetBytes("||DropProto-EOM||");
        var totalPackage = new byte[packetBytes.Length + eom.Length];

        Array.Copy(packetBytes, 0, totalPackage, 0, packetBytes.Length);
        Array.Copy(eom, 0, totalPackage, packetBytes.Length, eom.Length);

        using var stream = new NetworkStream(handler);
        await stream.WriteAsync(totalPackage, 0, totalPackage.Length);

        _logger.LogInformation("Finished sending: {command}", packet.Command);
    }

    private async Task HandleDownloadQueue(Socket handler)
    {
        foreach (var packet in _downloadQueue.Get()) 
        {
            await Send(handler, packet);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Server shutting down");
        return Task.CompletedTask;
    }
}
