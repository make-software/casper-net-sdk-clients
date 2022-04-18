using System;

namespace Casper.Network.SDK.Clients
{
    public class ContractException : Exception
    {
        public long Code { get; }
        
        public ContractException(string message, long code) : base(message)
        {
            Code = code;
        }

        public ContractException(string message, long code, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}
