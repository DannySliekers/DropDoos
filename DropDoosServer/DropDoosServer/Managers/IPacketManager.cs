using DropDoosServer.Data;

namespace DropDoosServer.Managers;

internal interface IPacketManager
{
    Task<Packet> HandlePacket(Packet packet);
}
