using System.Text;

namespace DropDoosServer.Data;

internal class Packet
{
    public required Command Command { get; set; }
    public File? File { get; set; }
    public int TotalNumberOfFiles { get; set; }
    private const int COMMAND_SIZE = 4;
    private const int FILE_NAME_SIZE = 255;
    private const int FILE_SIZE_SIZE = 8;
    private const int TOTAL_NUMBER_OF_FILES_SIZE = 4;
    private const int FILE_NUMBER_SIZE = 4;

    public byte[] ToByteArray()
    {
        List<byte> bytes = new List<byte>();


        AddToBytes(bytes, BitConverter.GetBytes((int)Command));
        AddToBytes(bytes, BitConverter.GetBytes(TotalNumberOfFiles));

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

        AddToBytes(bytes, nameField);
        AddToBytes(bytes, BitConverter.GetBytes(File.Size));
        AddToBytes(bytes, BitConverter.GetBytes(File.FileNumber));
        AddToBytes(bytes, File.Content);
    }

    private void AddToBytes(List<byte> bytes, byte[] bytesToAdd)
    {
        foreach (var singleByte in bytesToAdd)
        {
            bytes.Add(singleByte);
        }
    }

    public static Packet ToPacket(byte[] bytes)
    {
        var command = bytes.Take(COMMAND_SIZE).ToArray();
        var packetCommand = (Command) BitConverter.ToInt32(command);

        var totalNumberOfFiles = bytes.Skip(COMMAND_SIZE).Take(TOTAL_NUMBER_OF_FILES_SIZE).ToArray();
        var packetTotalNumberOfFiles = BitConverter.ToInt32(totalNumberOfFiles);

        var packet = new Packet()
        {
            Command = packetCommand,
            TotalNumberOfFiles = packetTotalNumberOfFiles
        };

        if (packetCommand == Command.Init || packetCommand == Command.Sync)
        {
            var fileName = bytes.Skip(COMMAND_SIZE + TOTAL_NUMBER_OF_FILES_SIZE).Take(FILE_NAME_SIZE).ToArray();
            var packetFileName = Encoding.UTF8.GetString(fileName).Trim('\0');
            var fileSize = bytes.Skip(COMMAND_SIZE + TOTAL_NUMBER_OF_FILES_SIZE + FILE_NAME_SIZE).Take(FILE_SIZE_SIZE).ToArray();
            var packetFileSize = BitConverter.ToInt64(fileSize);
            var fileNumber = bytes.Skip(COMMAND_SIZE + TOTAL_NUMBER_OF_FILES_SIZE + FILE_NAME_SIZE).Take(FILE_NUMBER_SIZE).ToArray();
            var packetFileNumber = BitConverter.ToInt32(fileNumber);
            var fileContent = bytes.Skip(COMMAND_SIZE + TOTAL_NUMBER_OF_FILES_SIZE + FILE_NAME_SIZE + FILE_SIZE_SIZE + FILE_NUMBER_SIZE)
                .Take(bytes.Length - COMMAND_SIZE + TOTAL_NUMBER_OF_FILES_SIZE + FILE_NAME_SIZE + FILE_SIZE_SIZE + FILE_NUMBER_SIZE).ToArray();

            packet.File = new File()
            {
                Name = packetFileName,
                Size = packetFileSize,
                Content = fileContent,
                FileNumber = packetFileNumber
            };
        }

        return packet;
    }
}
