using System;

namespace CMI.Access.Repository
{
    public class RepositoryConnectionException : Exception
    {
        public RepositoryConnectionException(Exception innerException) : base("Unable to connect to repository", innerException)
        {
        }
    }
}