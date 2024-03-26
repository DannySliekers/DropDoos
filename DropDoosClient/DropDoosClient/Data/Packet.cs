using System.Text;

namespace DropDoosClient.Data;

internal class Packet
{
    public required Command Command;
    public File? File { get; set; }

    public Dictionary<string, string>? optionalFields;
    private const int COMMAND_SIZE = 4;
    private const int FILE_NAME_SIZE = 255;
    private const int FILE_SIZE_SIZE = 8;

    public byte[] ToByteArray()
    {
        List<byte> bytes = new List<byte>();

        foreach(var singleByte in BitConverter.GetBytes((int) Command))
        {
            bytes.Add(singleByte);
        }

        if (File != null)
        {
            EncodeFile(bytes);
        }


       return bytes.ToArray();
    }

    private void EncodeFile(List<byte> bytes)
    {
        var nameField = new byte[255];
        var nameBytes = Encoding.UTF8.GetBytes(File.Name);

        for (var i = 0; i < nameBytes.Length; i++)
        {
            nameField[i] = nameBytes[i];
        }

        foreach (var singleByte in nameField)
        {
            bytes.Add(singleByte);
        }

        foreach (var singleByte in BitConverter.GetBytes(File.Size))
        {
            bytes.Add(singleByte);
        }

        foreach (var singleByte in File.Content)
        {
            bytes.Add(singleByte);
        }
    }

    public static Packet ToPacket(byte[] bytes)
    {
        var command = bytes.Take(COMMAND_SIZE).ToArray();
        var packetCommand = (Command)BitConverter.ToInt32(command);

        var packet = new Packet()
        {
            Command = packetCommand
        };

        if (packetCommand == Command.Init || packetCommand == Command.Sync)
        {
            var fileName = bytes.Skip(COMMAND_SIZE).Take(FILE_NAME_SIZE).ToArray();
            var packetFileName = Encoding.UTF8.GetString(fileName).Trim('\0');
            var fileSize = bytes.Skip(COMMAND_SIZE + FILE_NAME_SIZE).Take(FILE_SIZE_SIZE).ToArray();
            var packetFileSize = BitConverter.ToInt64(fileSize);
            var fileContent = bytes.Skip(COMMAND_SIZE + FILE_NAME_SIZE + FILE_SIZE_SIZE)
                .Take(bytes.Length - COMMAND_SIZE + FILE_NAME_SIZE + FILE_SIZE_SIZE).ToArray();

            packet.File = new File()
            {
                Name = packetFileName,
                Size = packetFileSize,
                Content = fileContent
            };
        }

        return packet;
    }
}
