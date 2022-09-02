using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    /// <summary>
    /// Base (and abstract) class to implement contract clients.
    /// </summary>
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

            CasperClient = casperClient;
        }

        /// <summary>
        /// Gets the stored value for a named key in the contract.
        /// </summary>
        /// <param name="path">Path of the named key</param>
        /// <typeparam name="T">Type of the stored value. Must be one of: Contract, CLValue, Account, ContractPackage</typeparam>
        /// <returns>The retrieved value. This method throws an exception if the named key does not exist or the type</returns>
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
        
        /// <summary>
        /// Looks up the contract hash to use for all calls to the contract in a named key of an account.
        /// </summary>
        /// <param name="publicKey">Account that contains a named key with the contract hash.</param>
        /// <param name="namedKey">Named key that contains the contract hash.</param>
        public async Task SetContractHash(PublicKey publicKey, string namedKey)
        {
            var response = await CasperClient.GetAccountInfo(publicKey);
            var result = response.Parse();
            var nk = result.Account.NamedKeys.FirstOrDefault(k => k.Name == namedKey);
            if (nk != null)
            {
                SetContractHash(nk.Key);
                return;
            }

            throw new ContractException($"Named key '{namedKey}' not found.", (int)ERC20ClientErrors.ContractNotFound);
        }
        
        /// <summary>
        /// Sets the contract hash to use for all calls to the contract
        /// </summary>
        /// <param name="contractHash">Contract hash of the contract</param>
        public void SetContractHash(string contractHash)
        {
            var key = GlobalStateKey.FromString(contractHash);
            SetContractHash(key);
        }

        /// <summary>
        /// Sets the contract hash to use for all calls to the contract
        /// </summary>
        /// <param name="contractHash">A valid Contract hash.</param>
        public void SetContractHash(GlobalStateKey contractHash)
        {
            ContractHash = contractHash as HashKey;
        }

        /// <summary>
        /// Initialization of the client with a contract package hash allows to make contract method calls, but it does not allow
        /// to retrieve values from named keys (e.g. balances, token information, etc.). Use a contract hash when this information is needed.
        /// </summary>
        /// <param name="contractPackageHash">The contract package hash to use for all calls to the contract.</param>
        /// <param name="contractVersion">The version number of the contract to call. Use null to call latest version.</param>
        public void SetContractPackageHash(GlobalStateKey contractPackageHash, uint? contractVersion)
        {
            ContractPackageHash = contractPackageHash as HashKey;
            ContractVersion = contractVersion;
        }

        protected ProcessDeployResult ProcessDeployResult;
        
        /// <summary>
        /// Internal method to builds a call to the contract with the parameters given as input. 
        /// </summary>
        /// <param name="entryPoint">Method of the contract to call.</param>
        /// <param name="namedArgs">List of input arguments for the contract call.</param>
        /// <param name="senderPk">Public key of the caller.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        protected DeployHelper BuildDeployHelper(string entryPoint,
            List<NamedArg> namedArgs,
            PublicKey senderPk,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            if (ContractHash != null)
            {
                var deploy = DeployTemplates.ContractCall(ContractHash,
                    entryPoint,
                    namedArgs,
                    senderPk,
                    paymentMotes,
                    ChainName,
                    DEFAULT_GAS_PRICE,
                    ttl);

                return new DeployHelper(deploy, CasperClient, ProcessDeployResult);
            }

            if (ContractPackageHash != null)
            {
                var deploy = DeployTemplates.VersionedContractCall(ContractPackageHash,
                    ContractVersion, // use 'null' to call latest version
                    entryPoint,
                    namedArgs,
                    senderPk,
                    paymentMotes,
                    ChainName,
                    DEFAULT_GAS_PRICE,
                    ttl);

                return new DeployHelper(deploy, CasperClient, ProcessDeployResult);
            }

            throw new Exception(
                "Neither Contract nor ContractPackage hashes are available. Check object initialization.");
        }
    }
}
