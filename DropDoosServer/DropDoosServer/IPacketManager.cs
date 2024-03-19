namespace DropDoosServer;

internal interface IPacketManager
{
    byte[]? HandlePacket(Packet packet);
}
