using DropDoosServer.Data;

namespace DropDoosServer.Managers;

internal interface IPacketManager
{
    Packet? HandlePacket(Packet packet);
}
