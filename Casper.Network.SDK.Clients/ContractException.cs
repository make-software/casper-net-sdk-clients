using System;

namespace Casper.Network.SDK.Clients
{
    /// <summary>
    /// Represents errors related with the communication or the execution of the contract. 
    /// </summary>
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
