namespace DropDoosServer;

internal class PacketManager
{
    public byte[] HandlePacket(Packet packet)
    {
        if (packet.command == Command.Connect)
        {
            return HandleConnectPacket(packet);
        }

        return null;
    }

    private byte[] HandleConnectPacket(Packet packet)
    {
        Console.WriteLine($"Socket server received message: {packet.command}");

        Packet response = new() { command = Command.Connect_Resp };
        return response.ToByteArray();
    }
}
