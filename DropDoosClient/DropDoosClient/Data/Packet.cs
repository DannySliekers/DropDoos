using Newtonsoft.Json;
using System.Text;

namespace DropDoosClient.Data;

internal class Packet
{
    public required Command command;
    public Dictionary<string, string>? optionalFields;

    public byte[] ToByteArray()
    {
        string json = JsonConvert.SerializeObject(this);
        return Encoding.UTF8.GetBytes(json);
    }

    public static Packet ToPacket(byte[] buffer)
    {
        string json = Encoding.UTF8.GetString(buffer).Trim('\0');
        return JsonConvert.DeserializeObject<Packet>(json);
    }
}
