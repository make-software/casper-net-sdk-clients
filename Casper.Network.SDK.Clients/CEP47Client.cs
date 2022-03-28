using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;
using Org.BouncyCastle.Utilities.Encoders;

namespace Casper.Network.SDK.Clients
{
    public class CEP47Client : ClientBase
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
            ulong gasPrice = 1,
            ulong ttl = 1800000)
        {
            var header = new DeployHeader()
            {
                Account = accountPK,
                Timestamp = DateUtils.ToEpochTime(DateTime.UtcNow),
                Ttl = ttl,
                ChainName = ChainName,
                GasPrice = gasPrice
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
            ulong gasPrice = 1,
            ulong ttl = 1800000)
        {
            return MintMany(senderPk, recipientPk,
                new List<BigInteger>() {tokenId},
                new List<Dictionary<string, string>>() {meta},
                paymentMotes,
                gasPrice,
                ttl);
        }

        public DeployHelper MintMany(PublicKey senderPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            List<Dictionary<string, string>> metas,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
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
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }
        
        public DeployHelper MintCopies(PublicKey ownerPK,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
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
                    new NamedArg("count", (uint)tokenIds.Count),
                },
                ownerPK,
                paymentMotes,
                ChainName,
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }
        
        public DeployHelper TransferToken(PublicKey ownerPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
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
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }
        
        public DeployHelper Approve(PublicKey ownerPk,
            PublicKey spenderPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
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
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper TransferTokenFrom(PublicKey senderPk,
            PublicKey ownerPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
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
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }
        
        private string key_and_value_to_str(PublicKey key, BigInteger value) {
            var ownerAccHash = new AccountHashKey(key);

            var bcBl2bdigest = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            bcBl2bdigest.BlockUpdate(ownerAccHash.GetBytes(), 0, ownerAccHash.GetBytes().Length);

            var bytes = value==BigInteger.Zero ? new byte[] {0x00} : CLValue.U256(value).Bytes;
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

        public async Task<Dictionary<string,string>> GetTokenMetadata(BigInteger tokenId)
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
            ulong gasPrice = 1,
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
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }

        public DeployHelper BurnOne(PublicKey senderPk,
            PublicKey ownerPk,
            BigInteger tokenId,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(new[] { CLValue.U256(tokenId)});

            return BurnMany(senderPk,
                ownerPk,
                new List<BigInteger>() {tokenId},
                paymentMotes,
                gasPrice,
                ttl);
        }
        
        public DeployHelper BurnMany(PublicKey senderPk,
            PublicKey ownerPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong gasPrice = 1,
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
                gasPrice,
                ttl);

            return new DeployHelper(deploy, CasperClient);
        }
    }
}