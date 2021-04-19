using DotCMIS.Client;

namespace CMI.Access.Repository
{
    public interface IRepositoryConnectionFactory
    {
        ISession ConnectToFirstRepository();
    }
}