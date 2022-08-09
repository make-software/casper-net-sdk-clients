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
    public partial class CEP47Client : ClientBase, ICEP47Client
    {
        public string Name { get; private set; }

        public string Symbol { get; private set; }

        public Dictionary<string, string> Meta { get; private set; }

        public CEP47Client(ICasperClient casperClient, string chainName)
            : base(casperClient, chainName)
        {
        }

        public override async Task<bool> SetContractHash(GlobalStateKey contractHash, bool skipNamedkeysQuery = false)
        {
            ContractHash = contractHash as HashKey;

            if (!skipNamedkeysQuery)
            {
                var result = await GetNamedKey<CLValue>("name");
                Name = result.ToString();

                result = await GetNamedKey<CLValue>("symbol");
                Symbol = result.ToString();

                result = await GetNamedKey<CLValue>("meta");
                Meta = result.ToDictionary<string, string>();
            }

            return true;
        }

        public DeployHelper InstallContract(byte[] wasmBytes,
            string contractName,
            string name,
            string symbol,
            Dictionary<string, string> meta,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
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

            var dict = new Dictionary<CLValue, CLValue>();
            foreach (var kvp in meta)
                dict.Add(kvp.Key, kvp.Value);

            var runtimeArgs = new List<NamedArg>()
            {
                new NamedArg("name", name),
                new NamedArg("symbol", symbol),
                new NamedArg("meta", CLValue.Map(dict)),
                new NamedArg("contract_name", contractName)
            };

            var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

            var deploy = new Deploy(header, payment, session);

            return new DeployHelper(deploy, CasperClient, _processDeployResult);
        }

        private ProcessDeployResult _processDeployResult = result =>
        {
            var executionResult = result.ExecutionResults.FirstOrDefault();
            if (executionResult is null)
                throw new ContractException("ExecutionResults null for processed deploy.",
                    (long) ERC20ClientErrors.GenericError);

            if (executionResult.IsSuccess)
                return;

            if (executionResult.ErrorMessage.Contains("User error: 1"))
                throw new ContractException("Deploy not executed. Permission denied",
                    (long) CEP47ClientErrors.PermissionDenied);

            if (executionResult.ErrorMessage.Contains("User error: 2"))
                throw new ContractException("Deploy not executed. Wrong arguments",
                    (long) CEP47ClientErrors.WrongArguments);

            if (executionResult.ErrorMessage.Contains("User error: 3"))
                throw new ContractException("Deploy not executed. Token Id already exists",
                    (long) CEP47ClientErrors.TokenIdAlreadyExists);

            if (executionResult.ErrorMessage.Contains("User error: 4"))
                throw new ContractException("Deploy not executed. Token Id doesn't exist",
                    (long) CEP47ClientErrors.TokenIdDoesntExist);

            throw new ContractException("Deploy not executed. " + executionResult.ErrorMessage,
                (long) CEP47ClientErrors.GenericError);
        };

        public async Task<BigInteger> GetTotalSupply()
        {
            var result = await GetNamedKey<CLValue>("total_supply");
            return result.ToBigInteger();
        }

        private DeployHelper BuildDeployHelper(string entryPoint,
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

                return new DeployHelper(deploy, CasperClient, _processDeployResult);
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

                return new DeployHelper(deploy, CasperClient, _processDeployResult);
            }

            throw new Exception(
                "Neither Contract nor ContractPackage hashes are available. Check object initialization.");
        }

        public DeployHelper MintOne(PublicKey senderPk,
            GlobalStateKey recipientKey,
            BigInteger? tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            return MintMany(senderPk, recipientKey,
                tokenId != null ? new List<BigInteger>() {tokenId.Value} : null,
                new List<Dictionary<string, string>>() {meta},
                paymentMotes,
                ttl);
        }

        public DeployHelper MintMany(PublicKey senderPk,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            List<Dictionary<string, string>> metas,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("recipient", CLValue.Key(recipientKey))
            };

            // create a list of U256 for the token ids
            //
            if (tokenIds != null)
            {
                namedArgs.Add(new("token_ids", CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray())));
            }

            // create a list of Map<string,string> for the metadata of each token
            //
            var clMetas = CLValue.List(metas.Select(meta =>
            {
                var dict = new Dictionary<CLValue, CLValue>();
                foreach (var kvp in meta)
                    dict.Add(kvp.Key, kvp.Value);
                return CLValue.Map(dict);
            }).ToArray());

            namedArgs.Add(new("token_metas", clMetas));

            return BuildDeployHelper("mint",
                    namedArgs,
                    senderPk,
                    paymentMotes,
                    ttl);
        }

        public DeployHelper MintCopies(PublicKey ownerPK,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("recipient", CLValue.Key(recipientKey)),
                new NamedArg("count", (uint) tokenIds.Count),
            };

            // create a list of U256 for the token ids
            //
            if (tokenIds != null)
            {
                namedArgs.Add(new("token_ids", CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray())));
            }

            var dict = new Dictionary<CLValue, CLValue>();
            foreach (var kvp in meta)
                dict.Add(kvp.Key, kvp.Value);

            namedArgs.Add(new("token_metas", CLValue.Map(dict)));

            return BuildDeployHelper("mint_copies",
                namedArgs,
                ownerPK,
                paymentMotes,
                ttl);
        }

        public DeployHelper TransferToken(PublicKey ownerPk,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            return BuildDeployHelper("transfer",
                new List<NamedArg>()
                {
                    new NamedArg("recipient", CLValue.Key(recipientKey)),
                    new NamedArg("token_ids", clTokenIds),
                },
                ownerPk,
                paymentMotes,
                ttl);
        }

        public DeployHelper Approve(PublicKey ownerPk,
            GlobalStateKey spenderKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            return BuildDeployHelper("approve",
                new List<NamedArg>()
                {
                    new NamedArg("spender", CLValue.Key(spenderKey)),
                    new NamedArg("token_ids", clTokenIds),
                },
                ownerPk,
                paymentMotes,
                ttl);
        }

        public DeployHelper TransferTokenFrom(PublicKey spenderPk,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            return BuildDeployHelper("transfer_from",
                new List<NamedArg>()
                {
                    new NamedArg("sender", CLValue.Key(ownerKey)),
                    new NamedArg("recipient", CLValue.Key(recipientKey)),
                    new NamedArg("token_ids", clTokenIds),
                },
                spenderPk,
                paymentMotes,
                ttl);
        }

        private string key_and_value_to_str(GlobalStateKey key, BigInteger value)
        {
            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(key.GetBytes(), 0, key.GetBytes().Length);

            var bytes = value == BigInteger.Zero ? new byte[] {0x00} : CLValue.U256(value).Bytes;
            bcBl2bdigest.BlockUpdate(bytes, 0, bytes.Length);

            var hash = new byte[bcBl2bdigest.GetDigestSize()];
            bcBl2bdigest.DoFinal(hash, 0);

            return Hex.ToHexString(hash);
        }

        public async Task<BigInteger?> GetTokenIdByIndex(GlobalStateKey ownerKey, uint index)
        {
            try
            {
                var dictItem = key_and_value_to_str(ownerKey, new BigInteger(index));

                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "owned_tokens_by_index", dictItem);
                var result = response.Parse();
                var option = result.StoredValue.CLValue;
                if (option != null && option.Some(out BigInteger tokenId))
                    return tokenId;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Token index not found in the owned_tokens_by_index entries.",
                        (long) CEP47ClientErrors.TokenIdDoesntExist, e);

                throw;
            }

            return null;
        }

        public async Task<Dictionary<string, string>> GetTokenMetadata(BigInteger tokenId)
        {
            try
            {
                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "metadata", tokenId.ToString());
                var result = response.Parse();

                var option = result.StoredValue.CLValue;
                if (option != null && option.Some(out Dictionary<string, string> metadata))
                    return metadata;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Token not found in the metadata entries.",
                        (long) CEP47ClientErrors.TokenIdDoesntExist, e);

                throw;
            }

            return null;
        }

        public async Task<GlobalStateKey> GetOwnerOf(BigInteger tokenId)
        {
            try
            {
                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "owners", tokenId.ToString());
                var result = response.Parse();

                var option = result.StoredValue.CLValue;
                if (option != null && option.Some(out GlobalStateKey ownerAccount))
                    return ownerAccount;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Token not found in the owners entries.",
                        (long) CEP47ClientErrors.TokenIdDoesntExist, e);

                throw;
            }

            return null;
        }

        public async Task<BigInteger?> GetBalanceOf(GlobalStateKey ownerKey)
        {
            try
            {
                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "balances", ownerKey.ToHexString().ToLower());
                var result = response.Parse();

                var option = result.StoredValue.CLValue;

                if (option != null && option.Some(out BigInteger tokenId))
                    return tokenId;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Account not found in the balances entries.",
                        (long) CEP47ClientErrors.UnknownAccount, e);

                throw;
            }

            return null;
        }

        public async Task<GlobalStateKey> GetApprovedSpender(GlobalStateKey ownerKey, BigInteger tokenId)
        {
            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(ownerKey.GetBytes(), 0, ownerKey.GetBytes().Length);

            var bytes = CLValue.String(tokenId.ToString()).Bytes;
            bcBl2bdigest.BlockUpdate(bytes, 0, bytes.Length);

            var hash = new byte[bcBl2bdigest.GetDigestSize()];
            bcBl2bdigest.DoFinal(hash, 0);

            try
            {
                var dictItem = Hex.ToHexString(hash);

                var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                    "allowances", dictItem);
                var result = response.Parse();
                var option = result.StoredValue.CLValue;
                if (option != null && option.Some(out GlobalStateKey spender))
                    return spender;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code == -32003)
                    throw new ContractException("Token not found in the allowances entries.",
                        (long) CEP47ClientErrors.TokenIdDoesntExist, e);

                throw;
            }

            return null;
        }

        public DeployHelper UpdateTokenMetadata(PublicKey senderPk,
            BigInteger tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var dict = new Dictionary<CLValue, CLValue>();
            foreach (var kvp in meta)
                dict.Add(kvp.Key, kvp.Value);

            return BuildDeployHelper("update_token_meta",
                new List<NamedArg>()
                {
                    new NamedArg("token_id", tokenId),
                    new NamedArg("token_meta", CLValue.Map(dict))
                },
                senderPk,
                paymentMotes,
                ttl);
        }

        public DeployHelper BurnOne(PublicKey senderPk,
            GlobalStateKey ownerKey,
            BigInteger tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            return BurnMany(senderPk,
                ownerKey,
                new List<BigInteger>() {tokenId},
                paymentMotes,
                ttl);
        }

        public DeployHelper BurnMany(PublicKey senderPk,
            GlobalStateKey ownerKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            // create a list of U256 for the token ids
            //
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            return BuildDeployHelper("burn",
                new List<NamedArg>()
                {
                    new NamedArg("owner", CLValue.Key(ownerKey)),
                    new NamedArg("token_ids", clTokenIds)
                },
                senderPk,
                paymentMotes,
                ttl);
        }
    }

    public enum CEP47ClientErrors
    {
        GenericError = 0,
        PermissionDenied = 1,
        WrongArguments = 2,
        TokenIdAlreadyExists = 3,
        TokenIdDoesntExist = 4,
        UnknownAccount = 100
    }
}
