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
        public string Name { get; private set; }

        public string Symbol { get; private set; }

        public byte Decimals { get; private set; }

        public BigInteger TotalSupply { get; private set; }

        public ERC20Client(ICasperClient casperClient, string chainName)
            : base(casperClient, chainName)
        {
        }

        public override async Task<bool> SetContractHash(GlobalStateKey contractHash, bool skipNamedkeysQuery=false)
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
                throw new ContractException("Only AccountHash or Hash keys are allowed.", (long)ERC20ClientErrors.AccountNotValid);
        }
        
        private ProcessDeployResult _processDeployResult = result =>
        {
            var executionResult = result.ExecutionResults.FirstOrDefault();
            if (executionResult is null)
                throw new ContractException("ExecutionResults null for processed deploy.",
                    (long)ERC20ClientErrors.GenericError);
                
            if (executionResult.IsSuccess)
                return;
                
            if (executionResult.ErrorMessage.Contains(((UInt16) ERC20ClientErrors.InsufficientBalance).ToString()))
                throw new ContractException("Deploy not executed. Insufficient balance.",
                    (long) ERC20ClientErrors.InsufficientBalance);
                
            if (executionResult.ErrorMessage.Contains(((UInt16) ERC20ClientErrors.InsufficientAllowance).ToString()))
                throw new ContractException("Deploy not executed. Insufficient allowance.",
                    (long) ERC20ClientErrors.InsufficientAllowance);

            throw new ContractException("Deploy not executed. " + executionResult.ErrorMessage,
                (long)ERC20ClientErrors.GenericError);
        };
        
        public DeployHelper TransferTokens(PublicKey ownerPK,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            _isValidKey(recipientKey);
            
            var deploy = DeployTemplates.ContractCall(ContractHash,
                "transfer",
                new List<NamedArg>()
                {
                    new NamedArg("recipient", CLValue.Key(recipientKey)),
                    new NamedArg("amount", CLValue.U256(amount))
                },
                ownerPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient, _processDeployResult);
        }

        public DeployHelper ApproveSpender(PublicKey ownerPK,
            GlobalStateKey spenderKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            _isValidKey(spenderKey);
            
            var deploy = DeployTemplates.ContractCall(ContractHash,
                "approve",
                new List<NamedArg>()
                {
                    new NamedArg("spender", CLValue.Key(spenderKey)),
                    new NamedArg("amount", CLValue.U256(amount))
                },
                ownerPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient, _processDeployResult);
        }

        public DeployHelper TransferTokensFromOwner(PublicKey spenderPK,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            _isValidKey(ownerKey);
            _isValidKey(recipientKey);
            
            var deploy = DeployTemplates.ContractCall(ContractHash,
                "transfer_from",
                new List<NamedArg>()
                {
                    new NamedArg("owner", CLValue.Key(ownerKey)),
                    new NamedArg("recipient", CLValue.Key(recipientKey)),
                    new NamedArg("amount", CLValue.U256(amount))
                },
                spenderPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient, _processDeployResult);
        }

        public async Task<BigInteger> GetBalance(GlobalStateKey ownerKey)
        {
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

        public async Task<BigInteger> GetAllowance(GlobalStateKey ownerKey, GlobalStateKey spenderKey)
        {
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
