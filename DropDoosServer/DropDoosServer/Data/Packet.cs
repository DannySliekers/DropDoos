using Newtonsoft.Json;
using System.Text;

namespace DropDoosServer.Data;

internal class Packet
{
    public required Command command { get; set; }
    public Dictionary<string, string>? optionalFields { get; set; }

    public byte[] ToByteArray()
    {
        string json = JsonConvert.SerializeObject(this);
        return Encoding.UTF8.GetBytes(json);
    }

    public static Packet ToPacket(byte[] buffer)
    {
        string jsonStr = Encoding.UTF8.GetString(buffer).Trim('\0');
        return JsonConvert.DeserializeObject<Packet>(jsonStr);
    }
}
