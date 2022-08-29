using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;
using Org.BouncyCastle.Utilities.Encoders;

namespace Casper.Network.SDK.Clients
{
    public class ERC20Client : ClientBase, IERC20Client
    {
        /// <summary>
        /// Name of the ERC20 token
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Symbol of the ERC20 token
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Decimals of the ERC20 token.
        /// </summary>
        public byte Decimals { get; private set; }

        /// <summary>
        /// Total supply of the ERC20 token.
        /// </summary>
        public BigInteger TotalSupply { get; private set; }

        /// <summary>
        /// Constructor of the client. Call SetContractHash or SetContractPackageHash before any other method. 
        /// </summary>
        /// <param name="casperClient">A valid ICasperClient object.</param>
        /// <param name="chainName">Name of the network being used.</param>
        public ERC20Client(ICasperClient casperClient, string chainName)
            : base(casperClient, chainName)
        {
            ProcessDeployResult = result =>
            {
                var executionResult = result.ExecutionResults.FirstOrDefault();
                if (executionResult is null)
                    throw new ContractException("ExecutionResults null for processed deploy.",
                        (long) ERC20ClientErrors.GenericError);

                if (executionResult.IsSuccess)
                    return;

                if (executionResult.ErrorMessage.Contains(((UInt16) ERC20ClientErrors.InsufficientBalance).ToString()))
                    throw new ContractException("Deploy not executed. Insufficient balance.",
                        (long) ERC20ClientErrors.InsufficientBalance);

                if (executionResult.ErrorMessage.Contains(((UInt16) ERC20ClientErrors.InsufficientAllowance)
                        .ToString()))
                    throw new ContractException("Deploy not executed. Insufficient allowance.",
                        (long) ERC20ClientErrors.InsufficientAllowance);

                throw new ContractException("Deploy not executed. " + executionResult.ErrorMessage,
                    (long) ERC20ClientErrors.GenericError);
            };
        }

        /// <summary>
        /// Stores the contract hash and, optionally, retrieves the ERC20 contract main details.
        /// </summary>
        /// <param name="contractHash">Contract hash of the contract</param>
        /// <param name="skipNamedkeysQuery">Set this to true to skip the retrieval of the default named keys during initialization.</param>
        /// <returns>False in case of an error retrieving the contract named keys. True otherwise.</returns>
        public override async Task<bool> SetContractHash(GlobalStateKey contractHash, bool skipNamedkeysQuery = false)
        {
            ContractHash = contractHash as HashKey;

            if (!skipNamedkeysQuery)
            {
                var result = await GetNamedKey<CLValue>("name");
                Name = result.ToString();

                result = await GetNamedKey<CLValue>("symbol");
                Symbol = result.ToString();

                result = await GetNamedKey<CLValue>("decimals");
                Decimals = result.ToByte();

                result = await GetNamedKey<CLValue>("total_supply");
                TotalSupply = result.ToBigInteger();
            }

            return true;
        }

        /// <summary>
        /// Prepares a Deploy to make a new install of the ERC20 contract with the given details.
        /// </summary>
        /// <param name="wasmBytes">Contract to deploy in WASM format.</param>
        /// <param name="name">Name of the ERC20 token.</param>
        /// <param name="symbol">Symbol of the ERC20 token.</param>
        /// <param name="decimals">Number of decimals of the ERC20 token.</param>
        /// <param name="totalSupply">Total supply for the new ERC20 contract.</param>
        /// <param name="accountPK">Caller account and owner of the contract.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        public DeployHelper InstallContract(byte[] wasmBytes,
            string name,
            string symbol,
            byte decimals,
            BigInteger totalSupply,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000
        )
        {
            var header = new DeployHeader()
            {
                Account = accountPK,
                Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                Ttl = ttl,
                ChainName = ChainName,
                GasPrice = DEFAULT_GAS_PRICE
            };
            var payment = new ModuleBytesDeployItem(paymentMotes);

            var runtimeArgs = new List<NamedArg>()
            {
                new NamedArg("name", name),
                new NamedArg("symbol", symbol),
                new NamedArg("decimals", decimals),
                new NamedArg("total_supply", CLValue.U256(totalSupply))
            };

            var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

            var deploy = new Deploy(header, payment, session);

            return new DeployHelper(deploy, CasperClient);
        }

        private void _isValidKey(GlobalStateKey key)
        {
            if (key is not AccountHashKey && key is not HashKey)
                throw new ContractException("Only AccountHash or Hash keys are allowed.",
                    (long) ERC20ClientErrors.AccountNotValid);
        }

        /// <summary>
        /// Prepares a Deploy to make a transfer of tokens to a recipient account.
        /// </summary>
        /// <param name="ownerPK">Caller account and owner of the tokens to send.</param>
        /// <param name="recipientKey">Recipient account of the tokens.</param>
        /// <param name="amount">Amount of tokens to send (with decimals).</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        public DeployHelper TransferTokens(PublicKey ownerPK,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            _isValidKey(recipientKey);

            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("recipient", CLValue.Key(recipientKey)),
                new NamedArg("amount", CLValue.U256(amount))
            };

            return BuildDeployHelper("transfer",
                namedArgs,
                ownerPK,
                paymentMotes,
                ttl);
        }

        /// <summary>
        /// Prepares a Deploy to approve a spender to make transfers of tokens on behalf of the owner.
        /// </summary>
        /// <param name="ownerPK">Caller account and owner of the tokens that the spender will be able to transfer after the approval.</param>
        /// <param name="spenderKey">The account that is being approved to transfer tokens from the owner.</param>
        /// <param name="amount">Amount of tokens to approve for transfers.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        public DeployHelper ApproveSpender(PublicKey ownerPK,
            GlobalStateKey spenderKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            _isValidKey(spenderKey);

            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("spender", CLValue.Key(spenderKey)),
                new NamedArg("amount", CLValue.U256(amount))
            };

            return BuildDeployHelper("approve",
                namedArgs,
                ownerPK,
                paymentMotes,
                ttl);
        }

        /// <summary>
        /// Prepares a Deploy to transfer tokens to a recipient on behalf of the tokens owner. 
        /// </summary>
        /// <param name="spenderPK">The approved account to transfer tokens on behal of the owner.</param>
        /// <param name="ownerKey">The tokens owner account.</param>
        /// <param name="recipientKey">The recipient account of the tokens. </param>
        /// <param name="amount">Amount of tokens to transfer (with decimals).</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        public DeployHelper TransferTokensFromOwner(PublicKey spenderPK,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            _isValidKey(ownerKey);
            _isValidKey(recipientKey);

            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("owner", CLValue.Key(ownerKey)),
                new NamedArg("recipient", CLValue.Key(recipientKey)),
                new NamedArg("amount", CLValue.U256(amount))
            };

            return BuildDeployHelper("transfer_from",
                namedArgs,
                spenderPK,
                paymentMotes,
                ttl);
        }

        /// <summary>
        /// Retrieves the balance of tokens for the given account.
        /// </summary>
        /// <param name="ownerKey">Account to check the balance of.</param>
        /// <returns>The balance of tokens with decimals.</returns>
        public async Task<BigInteger> GetBalance(GlobalStateKey ownerKey)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query an account balance.",
                    (long) ERC20ClientErrors.ContractNotFound);
            
            _isValidKey(ownerKey);

            var dictItem = Convert.ToBase64String(ownerKey.GetBytes());

            try
            {
                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "balances", dictItem);
                var result = response.Parse();
                return result.StoredValue.CLValue.ToBigInteger();
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Account not found in the balances entries.",
                        (long) ERC20ClientErrors.UnknownAccount, e);

                throw;
            }
        }

        /// <summary>
        /// Retrieves the remaining amount of tokens that the owner approved to the spender to transfer on his behalf.
        /// </summary>
        /// <param name="ownerKey">The owner's account.</param>
        /// <param name="spenderKey">The spender's account.</param>
        /// <returns>The amount of tokens with decimals that the spender may transfer on behalf of the owner.</returns>
        public async Task<BigInteger> GetAllowance(GlobalStateKey ownerKey, GlobalStateKey spenderKey)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query an allowance.",
                    (long) ERC20ClientErrors.ContractNotFound);
            
            _isValidKey(ownerKey);
            _isValidKey(spenderKey);

            var bytes = new byte[ownerKey.GetBytes().Length + spenderKey.GetBytes().Length];
            Array.Copy(ownerKey.GetBytes(), 0, bytes, 0, ownerKey.GetBytes().Length);
            Array.Copy(spenderKey.GetBytes(), 0, bytes, ownerKey.GetBytes().Length,
                spenderKey.GetBytes().Length);

            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(bytes, 0, bytes.Length);
            var hash = new byte[bcBl2bdigest.GetDigestSize()];
            bcBl2bdigest.DoFinal(hash, 0);

            var dictItem = Hex.ToHexString(hash);

            try
            {
                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "allowances", dictItem);
                var result = response.Parse();
                return result.StoredValue.CLValue.ToBigInteger();
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Account not found in the allowances entries.",
                        (long) ERC20ClientErrors.UnknownAccount, e);

                throw;
            }
        }
    }

    /// <summary>
    /// Enumeration with common ERC20 related errors that can be returned by the ERC20 client.
    /// </summary>
    public enum ERC20ClientErrors
    {
        InvalidContext = UInt16.MaxValue,
        InsufficientBalance = UInt16.MaxValue - 1,
        InsufficientAllowance = UInt16.MaxValue - 2,
        Overflow = UInt16.MaxValue - 3,
        GenericError = UInt16.MaxValue - 100,
        AccountNotValid = UInt16.MaxValue - 101,
        UnknownAccount = UInt16.MaxValue - 102,
        ContractNotFound = UInt16.MaxValue - 103
    }
}
