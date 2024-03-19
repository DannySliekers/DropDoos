using DropDoosServer.Data;

namespace DropDoosServer.Managers;

internal interface IPacketManager
{
    byte[]? HandlePacket(Packet packet);
}
