namespace DropDoosServer.Managers;

public class ClientManager : IClientManager
{
    private readonly List<Guid> _clients;

    public ClientManager() 
    {
        _clients = new List<Guid>();
    }

    public Guid ConnectClient()
    {
        var clientId = Guid.NewGuid();
        _clients.Add(clientId);
        return clientId;
    }

    public List<Guid> GetClients()
    {
        return _clients;
    }

    public void DisconnectClient(Guid clientId)
    {
        _clients.Remove(clientId);
    }
}
