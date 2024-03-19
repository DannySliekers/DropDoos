﻿using Newtonsoft.Json;
using System.Text;

namespace DropDoosClient.Data;

internal class Packet
{
    public required Command command;
    public Dictionary<string, string>? optionalFields;

    public byte[] ToByteArray()
    {
        string json = JsonConvert.SerializeObject(this);
        byte[] bytes = Encoding.Default.GetBytes(json);
        var base64 = Convert.ToBase64String(bytes);
        return Encoding.UTF8.GetBytes(base64);
    }

    public static Packet ToPacket(byte[] buffer)
    {
        string base64 = Encoding.Default.GetString(buffer).Trim('\0');
        byte[] bytes = Convert.FromBase64String(base64);
        string json = Encoding.Default.GetString(bytes);
        return JsonConvert.DeserializeObject<Packet>(json);
    }
}