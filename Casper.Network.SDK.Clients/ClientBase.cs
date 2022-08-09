using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public abstract class ClientBase
    {
        protected const ulong DEFAULT_GAS_PRICE = 1;

        protected readonly string ChainName;
        protected readonly ICasperClient CasperClient;

        public HashKey ContractHash { get; protected set; }
        
        public HashKey ContractPackageHash { get; protected set; }

        public uint? ContractVersion { get; protected set; }

        protected ClientBase(ICasperClient casperClient, string chainName)
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
            
            throw new Exception("Unsupported StoredValue type: " + typeof(T));
        }
        
        public async Task<bool> SetContractHash(PublicKey publicKey, string namedKey, bool skipNamedkeysQuery=false)
        {
            var response = await CasperClient.GetAccountInfo(publicKey);
            var result = response.Parse();
            var nk = result.Account.NamedKeys.FirstOrDefault(k => k.Name == namedKey);
            if (nk != null)
                return await SetContractHash(nk.Key, skipNamedkeysQuery);

            throw new ContractException($"Named key '{namedKey}' not found.", (int)ERC20ClientErrors.ContractNotFound);
        }
        
        public async Task<bool> SetContractHash(string contractHash, bool skipNamedkeysQuery=false)
        {
            var key = GlobalStateKey.FromString(contractHash);
            return await SetContractHash(key, skipNamedkeysQuery);
        }

        public abstract Task<bool> SetContractHash(GlobalStateKey contractHash, bool skipNamedkeysQuery=false);

        public bool SetContractPackageHash(GlobalStateKey contractPackageHash, uint? contractVersion, bool skipNamedkeysQuery = false)
        {
            if (skipNamedkeysQuery == false)
                throw new NotImplementedException(
                    "SetContractPackageHash is only allowed with skipNamedKeysQuery=true");

            ContractPackageHash = contractPackageHash as HashKey;
            ContractVersion = contractVersion;
            
            return true;
        }
    }
}
