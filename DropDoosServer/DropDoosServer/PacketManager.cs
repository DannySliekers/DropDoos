namespace DropDoosServer;

internal class PacketManager
{
    public byte[]? HandlePacket(Packet packet)
    {
        if (packet == null)
        {
            return null;
        }

        if (packet.command == Command.Connect)
        {
            return HandleConnectPacket(packet);
        } 
        else if (packet.command == Command.Init)
        {
            return HandleInitPacket(packet);
        }

        return null;
    }

    private byte[]? HandleConnectPacket(Packet packet)
    {
        Console.WriteLine($"Socket server received message: {packet.command}");
        var optionalFields = new Dictionary<string, string>() { 
            { "unique_id", Guid.NewGuid().ToString() }
        };
        Packet response = new() { command = Command.Connect_Resp, optionalFields = optionalFields };
        return response.ToByteArray();
    }

    private byte[]? HandleInitPacket(Packet packet)
    {
        Console.WriteLine(packet.optionalFields["1"]);
        return null;
    }
}
