using System;

namespace Casper.Network.SDK.Clients
{
    public class ContractException : Exception
    {
        public int Code { get; }
        
        public ContractException(string message, int code) : base(message)
        {
            Code = code;
        }

        public ContractException(string message, int code, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}