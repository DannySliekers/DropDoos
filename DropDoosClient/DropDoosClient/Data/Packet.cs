using Newtonsoft.Json;
using System.Text;

namespace DropDoosClient.Data;

internal class Packet
{
    public required Command Command { get; set; }
    public Guid ClientId { get; set; }
    public File? File { get; set; }
    public List<string> FileList { get; set; } = new List<string>();
    public List<string> ClientEditedFiles { get; set; } = new List<string>();
    public List<string> ServerEditedFiles { get; set; } = new List<string>();


    public byte[] ToByteArray()
    {
        string json = JsonConvert.SerializeObject(this);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        return bytes;
    }

    public static Packet ToPacket(byte[] buffer)
    {
        string json = Encoding.UTF8.GetString(buffer);
        return JsonConvert.DeserializeObject<Packet>(json);
    }
}
