using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;
using Org.BouncyCastle.Utilities.Encoders;

namespace Casper.Network.SDK.Clients
{
    public enum CEP47EventType
    {
        Unknown,
        MintOne,
        BurnOne,
        Approve,
        Transfer,
        UpdateMetadata
    }

    public class CEP47Event
    {
        public CEP47EventType EventType { get; init; }
        public string ContractPackageHash { get; init; }
        public string DeployHash { get; init; }
        public string TokenId { get; init; }
        public string Owner { get; init; }
        public string Spender { get; init; }
        public string Sender { get; init; }
        public string Recipient { get; init; }
    }

    public delegate void CEP47EventHandler(CEP47Event evt);

    public class CEP47Client : ClientBase, ICEP47Client
    {
        public string Name { get; private set; }

        public string Symbol { get; private set; }

        public Dictionary<string, string> Meta { get; private set; }

        public BigInteger TotalSupply { get; private set; }

        public CEP47Client(ICasperClient casperClient, string chainName)
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

            result = await GetNamedKey<CLValue>("meta");
            Meta = result.ToDictionary<string, string>();

            result = await GetNamedKey<CLValue>("total_supply");
            TotalSupply = result.ToBigInteger();

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

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper MintOne(PublicKey senderPk,
            PublicKey recipientPk,
            BigInteger tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            return MintMany(senderPk, recipientPk,
                new List<BigInteger>() {tokenId},
                new List<Dictionary<string, string>>() {meta},
                paymentMotes,
                ttl);
        }

        public DeployHelper MintMany(PublicKey senderPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            List<Dictionary<string, string>> metas,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            // create a list of U256 for the token ids
            //
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            // create a list of Map<string,string> for the metadata of each token
            //
            var clMetas = CLValue.List(metas.Select(meta =>
            {
                var dict = new Dictionary<CLValue, CLValue>();
                foreach (var kvp in meta)
                    dict.Add(kvp.Key, kvp.Value);
                return CLValue.Map(dict);
            }).ToArray());

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "mint",
                new List<NamedArg>()
                {
                    new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientPk))),
                    new NamedArg("token_ids", clTokenIds),
                    new NamedArg("token_metas", clMetas)
                },
                senderPk,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper MintCopies(PublicKey ownerPK,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            var dict = new Dictionary<CLValue, CLValue>();
            foreach (var kvp in meta)
                dict.Add(kvp.Key, kvp.Value);

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "mint_copies",
                new List<NamedArg>()
                {
                    new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientPk))),
                    new NamedArg("token_ids", clTokenIds),
                    new NamedArg("token_meta", CLValue.Map(dict)),
                    new NamedArg("count", (uint) tokenIds.Count),
                },
                ownerPK,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper TransferToken(PublicKey ownerPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "transfer",
                new List<NamedArg>()
                {
                    new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientPk))),
                    new NamedArg("token_ids", clTokenIds),
                },
                ownerPk,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper Approve(PublicKey ownerPk,
            PublicKey spenderPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "approve",
                new List<NamedArg>()
                {
                    new NamedArg("spender", CLValue.Key(new AccountHashKey(spenderPk))),
                    new NamedArg("token_ids", clTokenIds),
                },
                ownerPk,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper TransferTokenFrom(PublicKey senderPk,
            PublicKey ownerPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "transfer_from",
                new List<NamedArg>()
                {
                    new NamedArg("sender", CLValue.Key(new AccountHashKey(ownerPk))),
                    new NamedArg("recipient", CLValue.Key(new AccountHashKey(recipientPk))),
                    new NamedArg("token_ids", clTokenIds),
                },
                senderPk,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        private string key_and_value_to_str(PublicKey key, BigInteger value)
        {
            var ownerAccHash = new AccountHashKey(key);

            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(ownerAccHash.GetBytes(), 0, ownerAccHash.GetBytes().Length);

            var bytes = value == BigInteger.Zero ? new byte[] {0x00} : CLValue.U256(value).Bytes;
            bcBl2bdigest.BlockUpdate(bytes, 0, bytes.Length);

            var hash = new byte[bcBl2bdigest.GetDigestSize()];
            bcBl2bdigest.DoFinal(hash, 0);

            return Hex.ToHexString(hash);
        }

        public async Task<BigInteger?> GetTokenIdByIndex(PublicKey owner, uint index)
        {
            var dictItem = key_and_value_to_str(owner, new BigInteger(index));

            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "owned_tokens_by_index", dictItem);
            var result = response.Parse();
            var option = result.StoredValue.CLValue;
            if (option != null && option.Some(out BigInteger tokenId))
                return tokenId;

            return null;
        }

        public async Task<Dictionary<string, string>> GetTokenMetadata(BigInteger tokenId)
        {
            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "metadata", tokenId.ToString());
            var result = response.Parse();

            var option = result.StoredValue.CLValue;
            if (option != null && option.Some(out Dictionary<string, string> metadata))
                return metadata;

            return null;
        }

        public async Task<GlobalStateKey> GetOwnerOf(BigInteger tokenId)
        {
            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "owners", tokenId.ToString());
            var result = response.Parse();

            var option = result.StoredValue.CLValue;
            if (option != null && option.Some(out GlobalStateKey ownerAccount))
                return ownerAccount;

            return null;
        }

        public async Task<BigInteger?> GetBalanceOf(PublicKey owner)
        {
            var ownerAccHash = new AccountHashKey(owner);

            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "balances", ownerAccHash.ToHexString().ToLower());
            var result = response.Parse();

            var option = result.StoredValue.CLValue;

            if (option != null && option.Some(out BigInteger tokenId))
                return tokenId;

            return null;
        }

        public async Task<GlobalStateKey> GetApprovedSpender(PublicKey owner, BigInteger tokenId)
        {
            var ownerAccHash = new AccountHashKey(owner);

            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(ownerAccHash.GetBytes(), 0, ownerAccHash.GetBytes().Length);

            var bytes = CLValue.String(tokenId.ToString()).Bytes;
            bcBl2bdigest.BlockUpdate(bytes, 0, bytes.Length);

            var hash = new byte[bcBl2bdigest.GetDigestSize()];
            bcBl2bdigest.DoFinal(hash, 0);

            var dictItem = Hex.ToHexString(hash);

            var response = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(),
                "allowances", dictItem);
            var result = response.Parse();
            var option = result.StoredValue.CLValue;
            if (option != null && option.Some(out GlobalStateKey spender))
                return spender;

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

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "update_token_meta",
                new List<NamedArg>()
                {
                    new NamedArg("token_id", tokenId),
                    new NamedArg("token_meta", CLValue.Map(dict))
                },
                senderPk,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper BurnOne(PublicKey senderPk,
            PublicKey ownerPk,
            BigInteger tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(new[] {CLValue.U256(tokenId)});

            return BurnMany(senderPk,
                ownerPk,
                new List<BigInteger>() {tokenId},
                paymentMotes,
                ttl);
        }

        public DeployHelper BurnMany(PublicKey senderPk,
            PublicKey ownerPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            // create a list of U256 for the token ids
            //
            var clTokenIds = CLValue.List(tokenIds.Select(t => CLValue.U256(t)).ToArray());

            var deploy = DeployTemplates.ContractCall(ContractHash,
                "burn",
                new List<NamedArg>()
                {
                    new NamedArg("owner", CLValue.Key(new AccountHashKey(ownerPk))),
                    new NamedArg("token_ids", clTokenIds)
                },
                senderPk,
                paymentMotes,
                ChainName,
                DEFAULT_GAS_PRICE,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        private GlobalStateKey _contractPackageHash;

        public event CEP47EventHandler OnCEP47Event;

        private ServerEventsClient _sse;

        public async Task ListenToEvents()
        {
            var localNetHost = "127.0.0.1";
            var localNetPort = 18101;
            _sse = new ServerEventsClient(localNetHost, localNetPort);

            var rpcResponse = await CasperClient.QueryGlobalState(ContractHash);
            var result = rpcResponse.Parse();

            _contractPackageHash = GlobalStateKey.FromString(
                result.StoredValue.Contract.ContractPackageHash);

            _sse.AddEventCallback(EventType.DeployProcessed, "catch-all-cb",
                this.ProcessEvent);
            _sse.StartListening();
        }

        private void TriggerEvent(IDictionary<string, string> map, DeployProcessed deploy)
        {
            CEP47Event evt = new CEP47Event
            {
                EventType = map["event_type"] switch
                {
                    "cep47_mint_one" => CEP47EventType.MintOne,
                    "cep47_burn_one" => CEP47EventType.BurnOne,
                    "cep47_approve_token" => CEP47EventType.Approve,
                    "cep47_transfer_token" => CEP47EventType.Transfer,
                    "cep47_metadata_update" => CEP47EventType.UpdateMetadata,
                    _ => CEP47EventType.Unknown
                },
                TokenId = map.ContainsKey("token_id") ? map["token_id"] : null,
                Owner = map.ContainsKey("owner") ? map["owner"] : null,
                Spender = map.ContainsKey("spender") ? map["spender"] : null,
                Sender = map.ContainsKey("sender") ? map["sender"] : null,
                Recipient = map.ContainsKey("recipient") ? map["recipient"] : null,
                ContractPackageHash = _contractPackageHash.ToHexString(),
                DeployHash = deploy.DeployHash
            };

            OnCEP47Event?.Invoke(evt);
        }

        private void ProcessEvent(SSEvent evt)
        {
            try
            {
                if (evt.EventType == EventType.DeployProcessed)
                {
                    var deploy = evt.Parse<DeployProcessed>();
                    if (!deploy.ExecutionResult.IsSuccess)
                        return;

                    var maybeEvents = deploy.ExecutionResult.Effect.Transforms.Where(
                        tr => tr.Type == TransformType.WriteCLValue && tr.Key is URef);

                    foreach (var maybeEvt in maybeEvents)
                    {
                        var clValue = maybeEvt.Value as CLValue;
                        if (clValue?.TypeInfo is CLMapTypeInfo clMapTypeInfo &&
                            clMapTypeInfo.KeyType.Type is CLType.String &&
                            clMapTypeInfo.ValueType.Type is CLType.String)
                        {
                            var map = clValue.ToDictionary<string, string>();
                            if (map.ContainsKey("contract_package_hash") &&
                                map["contract_package_hash"] == _contractPackageHash.ToHexString().ToLower())
                            {
                                TriggerEvent(map, deploy);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
    }
}
