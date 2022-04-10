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
    public enum ERC20ClientErrors
    {
        InvalidContext = UInt16.MaxValue,
        InsufficientBalance = UInt16.MaxValue - 1,
        InsufficientAllowance = UInt16.MaxValue - 2,
        Overflow = UInt16.MaxValue - 3
    }

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

        public async Task<bool> SetContractHash(PublicKey publicKey, string namedKey)
        {
            var response = await CasperClient.GetAccountInfo(publicKey);
            var result = response.Parse();
            var nk = result.Account.NamedKeys.FirstOrDefault(k => k.Name == namedKey);
            if (nk != null)
                return await SetContractHash(nk.Key);

            throw new Exception($"Named key '{namedKey}' not found.");
        }

        public async Task<bool> SetContractHash(string contractHash)
        {
            var key = GlobalStateKey.FromString(contractHash);
            return await SetContractHash(key);
        }

        public async Task<bool> SetContractHash(GlobalStateKey contractHash)
        {
            ContractHash = contractHash as HashKey;

            var result = await GetNamedKey<CLValue>("name");
            Name = result.ToString();

            result = await GetNamedKey<CLValue>("symbol");
            Symbol = result.ToString();

            result = await GetNamedKey<CLValue>("decimals");
            Decimals = result.ToByte();

            result = await GetNamedKey<CLValue>("total_supply");
            TotalSupply = result.ToBigInteger();

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

        public DeployHelper TransferTokens(PublicKey ownerPK,
            PublicKey recipientPk,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var deploy = DeployTemplates.ContractCall(ContractHash,
                "transfer",
                new List<NamedArg>()
                {
                    new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientPk))),
                    new NamedArg("amount", CLValue.U256(amount))
                },
                ownerPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper ApproveSpender(PublicKey ownerPK,
            PublicKey spenderPk,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var deploy = DeployTemplates.ContractCall(ContractHash,
                "approve",
                new List<NamedArg>()
                {
                    new NamedArg("spender", CLValue.Key(new AccountHashKey(spenderPk))),
                    new NamedArg("amount", CLValue.U256(amount))
                },
                ownerPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper TransferTokensFromOwner(PublicKey spenderPK,
            PublicKey ownerPk,
            PublicKey recipientPk,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var deploy = DeployTemplates.ContractCall(ContractHash,
                "transfer_from",
                new List<NamedArg>()
                {
                    new NamedArg("owner", CLValue.Key(new AccountHashKey(ownerPk))),
                    new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientPk))),
                    new NamedArg("amount", CLValue.U256(amount))
                },
                spenderPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public async Task<BigInteger> GetBalance(PublicKey ownerPK)
        {
            var accountHash = new AccountHashKey(ownerPK);
            var dictItem = Convert.ToBase64String(accountHash.GetBytes());

            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "balances", dictItem);
            var result = response.Parse();
            return result.StoredValue.CLValue.ToBigInteger();
        }

        public async Task<BigInteger> GetAllowance(PublicKey ownerPK, PublicKey spenderPK)
        {
            var ownerAccHash = new AccountHashKey(ownerPK);
            var spenderAccHash = new AccountHashKey(spenderPK);
            var bytes = new byte[ownerAccHash.GetBytes().Length + spenderAccHash.GetBytes().Length];
            Array.Copy(ownerAccHash.GetBytes(), 0, bytes, 0, ownerAccHash.GetBytes().Length);
            Array.Copy(spenderAccHash.GetBytes(), 0, bytes, ownerAccHash.GetBytes().Length,
                spenderAccHash.GetBytes().Length);

            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(bytes, 0, bytes.Length);
            var hash = new byte[bcBl2bdigest.GetDigestSize()];
            bcBl2bdigest.DoFinal(hash, 0);

            var dictItem = Hex.ToHexString(hash);

            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "allowances", dictItem);
            var result = response.Parse();
            return result.StoredValue.CLValue.ToBigInteger();
        }
    }
}
