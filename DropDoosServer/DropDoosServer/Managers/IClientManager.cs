namespace DropDoosServer.Managers;

public interface IClientManager
{

    public Guid ConnectClient();
    public List<Guid> GetClients();
    public void Disconnectclient(Guid clientId);
}
