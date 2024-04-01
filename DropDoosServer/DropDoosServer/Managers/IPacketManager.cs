using DropDoosServer.Data;

namespace DropDoosServer.Managers;

public interface IPacketManager
{
    Packet? HandlePacket(Packet packet);
}
