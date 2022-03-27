using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public class ClientBase
    {
        protected readonly string ChainName;
        protected readonly ICasperClient CasperClient;

        public HashKey ContractHash { get; protected set; }

        public ClientBase(ICasperClient casperClient, string chainName)
        {
            ChainName = chainName;

            var loggingHandler = new RpcLoggingHandler(new HttpClientHandler())
            {
                LoggerStream = new StreamWriter(Console.OpenStandardOutput())
            };
            CasperClient = casperClient;
        }

        protected async Task<T> GetNamedKey<T>(string path)
        {
            var response = await CasperClient.QueryGlobalState(ContractHash, null, path);
            var result = response.Parse();

            if (typeof(T) == typeof(Contract))
                return (T) Convert.ChangeType(result.StoredValue?.Contract, typeof(T));
            if (typeof(T) == typeof(CLValue))
                return (T) Convert.ChangeType(result.StoredValue?.CLValue, typeof(T));
            if (typeof(T) == typeof(Account))
                return (T) Convert.ChangeType(result.StoredValue?.Account, typeof(T));
            if (typeof(T) == typeof(ContractPackage))
                return (T) Convert.ChangeType(result.StoredValue?.ContractPackage, typeof(T));
            
            throw new Exception("Unsupported StoredValue type: " + typeof(T).ToString());
        }

    }
}