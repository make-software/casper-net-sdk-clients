using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;

namespace Casper.Network.SDK.Clients.CEP78
{
    public enum NFTOwnershipMode
    {
        Minter,
        Assigned,
        Transferable
    }

    public enum NFTKind
    {
        Physical,
        Digital,
        Virtual
    }

    public enum NFTHolderMode
    {
        Accounts,
        Contracts,
        Mixed
    }

    public enum WhitelistMode
    {
        Unlocked,
        Locked
    }

    public enum MintingMode
    {
        Installer,
        Public
    }

    public enum NFTMetadataKind
    {
        CEP78,
        NFT721,
        Raw,
        CustomValidated
    }

    public enum NFTIdentifierMode
    {
        Ordinal,
        Hash
    }

    public enum MetadataMutability
    {
        Immutable,
        Mutable
    }

    public enum BurnMode
    {
        Burnable,
        NonBurnable
    }

    public class JsonSchemaEntry
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("required")] public bool Required { get; set; }
    }

    public class JsonSchema
    {
        [JsonPropertyName("properties")] public Dictionary<string, JsonSchemaEntry> Properties { get; set; }
    }

    public interface ITokenMetadata
    {
        string Serialize();
        
        static ITokenMetadata Deserialize<T>(string json) where T : ITokenMetadata
        {
            return JsonSerializer.Deserialize<T>(json);
        }
    }
    
    public class CEP78TokenMetadata : ITokenMetadata
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("token_uri")] public string TokenUri { get; set; }
        [JsonPropertyName("checksum")] public string Checksum { get; set; }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class NFT721tokenMetadata : ITokenMetadata
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("symbol")] public string Symbol { get; set; }
        [JsonPropertyName("token_uri")] public string TokenUri { get; set; }
        
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    
    public class CEP78InstallArgs
    {
        public string CollectionName { get; set; }
        
        public string CollectionSymbol { get; set; }
        
        public ulong TokenTotalSupply { get; set; }
        
        public NFTOwnershipMode OwnershipMode { get; set; }
        
        public NFTKind NFTKind { get; set; }
        
        public NFTMetadataKind NFTMetadataKind { get; set; }
        
        public JsonSchema JsonSchema { get; set; }
        
        public NFTIdentifierMode NFTIdentifierMode { get; set; }
        
        public MetadataMutability MetadataMutability { get; set; }

        public MintingMode? MintingMode { get; set; }
        
        public bool? AllowMinting { get; set; }
        
        public WhitelistMode? WhitelistMode { get; set; }
        
        public NFTHolderMode? NFTHolderMode { get; set; }

        public IEnumerable<HashKey> ContractWhiteList { get; set; }
        
        public BurnMode? BurnMode { get; set; }
    }

    public class CEP78Client : ClientBase, ICEP78Client
    {
        private NFTMetadataKind? _metadataKind;
        
        public async Task<string> GetCollectionName() => 
            (await GetNamedKey<CLValue>("collection_name")).ToString();

        public async Task<string> GetCollectionSymbol() => 
            (await GetNamedKey<CLValue>("collection_symbol")).ToString();

        public async Task<ulong> GetTokenTotalSupply() => 
            (await GetNamedKey<CLValue>("total_token_supply")).ToUInt64();

        public async Task<NFTOwnershipMode> GetOwnershipMode() => 
            (NFTOwnershipMode)(await GetNamedKey<CLValue>("ownership_mode")).ToByte();

        public async Task<NFTKind> GetNFTKind() => 
            (NFTKind)(await GetNamedKey<CLValue>("nft_kind")).ToByte();

        public async Task<NFTMetadataKind> GetNFTMetadataKind() =>
            (NFTMetadataKind)(await GetNamedKey<CLValue>("nft_metadata_kind")).ToByte();

        public async Task<JsonSchema> GetJsonSchema()
        {
            var json = (await GetNamedKey<CLValue>("json_schema")).ToString();
            return JsonSerializer.Deserialize<JsonSchema>(json);
        }

        public async Task<NFTIdentifierMode> GetNFTIdentifierMode() =>
            (NFTIdentifierMode)(await GetNamedKey<CLValue>("identifier_mode")).ToByte();

        public async Task<MetadataMutability> GetMetadataMutability() =>
            (MetadataMutability)(await GetNamedKey<CLValue>("metadata_mutability")).ToByte();

        public async Task<MintingMode> GetMintingMode() => 
            (MintingMode)(await GetNamedKey<CLValue?>("minting_mode")).ToByte();

        public async Task<bool> GetAllowMinting() => 
            (await GetNamedKey<CLValue>("allow_minting")).ToBoolean();

        public async Task<WhitelistMode> GetWhitelistMode() => 
            (WhitelistMode)(await GetNamedKey<CLValue>("whitelist_mode")).ToByte();

        public async Task<NFTHolderMode> GetNFTHolderMode() =>
            (NFTHolderMode) (await GetNamedKey<CLValue>("holder_mode")).ToByte();

        public async Task<IEnumerable<HashKey>> GetContractWhiteList()
        {
            var value = await GetNamedKey<CLValue>("contract_whitelist");
            return value.ToList<byte[]>().Select(hash => new HashKey(hash));
        }

        public async Task<BurnMode> GetBurnMode() => 
            (BurnMode)(await GetNamedKey<CLValue>("burn_mode")).ToByte();
        
        public async Task<ulong> GetNumberOfMintedTokens() => 
            (await GetNamedKey<CLValue>("number_of_minted_tokens")).ToUInt64();

        public async Task<string> GetReceiptName() => 
            (await GetNamedKey<CLValue>("receipt_name")).ToString();
        
        public async Task<GlobalStateKey> GetInstaller()
        {
            var response = await CasperClient.QueryGlobalState(ContractHash, null, "installer");
            var result = response.Parse();

            if (result.StoredValue.Account != null)
                return result.StoredValue.Account.AccountHash;

            throw new ContractException("Unexpected response for get 'installer' named key.", (long)CEP78ClientErrors.InvalidAccount);
        }
        
        public CEP78Client(ICasperClient casperClient, string chainName)
            : base(casperClient, chainName)
        {
            ProcessDeployResult = result =>
            {
                var executionResult = result.ExecutionResults.FirstOrDefault();
                if (executionResult is null)
                    throw new ContractException("ExecutionResults null for processed deploy.",
                        (long) ERC20ClientErrors.OtherError);

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
                    (long) CEP47ClientErrors.OtherError);
            };
        }

        public override Task<bool> SetContractHash(GlobalStateKey contractHash, bool skipNamedkeysQuery = false)
        {
            ContractHash = contractHash as HashKey;

            return Task.FromResult(true);
        }

        public DeployHelper InstallContract(byte[] wasmBytes,
            CEP78InstallArgs installArgs,
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

            var runtimeArgs = new List<NamedArg>()
            {
                new NamedArg("collection_name", installArgs.CollectionName),
                new NamedArg("collection_symbol", installArgs.CollectionSymbol),
                new NamedArg("total_token_supply", installArgs.TokenTotalSupply),
                new NamedArg("ownership_mode", CLValue.U8((byte) installArgs.OwnershipMode)),
                new NamedArg("nft_kind", CLValue.U8((byte) installArgs.NFTKind)),
                new NamedArg("nft_metadata_kind", CLValue.U8((byte) installArgs.NFTMetadataKind)),
                new NamedArg("json_schema",
                    installArgs.JsonSchema == null ? string.Empty : JsonSerializer.Serialize(installArgs.JsonSchema)),
                new NamedArg("identifier_mode", CLValue.U8((byte) installArgs.NFTIdentifierMode)),
                new NamedArg("metadata_mutability", CLValue.U8((byte) installArgs.MetadataMutability)),
            };

            if (installArgs.MintingMode != null)
                runtimeArgs.Add(new NamedArg("minting_mode",
                    CLValue.Option(CLValue.U8((byte) installArgs.MintingMode))));

            if (installArgs.AllowMinting != null)
                runtimeArgs.Add(new NamedArg("allow_minting",
                    CLValue.Option(CLValue.Bool(installArgs.AllowMinting.Value))));

            if (installArgs.WhitelistMode != null)
                runtimeArgs.Add(new NamedArg("whitelist_mode",
                    CLValue.Option(CLValue.U8((byte) installArgs.WhitelistMode.Value))));

            if (installArgs.NFTHolderMode != null)
                runtimeArgs.Add(new NamedArg("holder_mode",
                    CLValue.Option(CLValue.U8((byte) installArgs.NFTHolderMode.Value))));

            if (installArgs.ContractWhiteList != null && installArgs.ContractWhiteList.Any())
            {
                var contracts = installArgs.ContractWhiteList.Select(c => CLValue.ByteArray(c.RawBytes)).ToArray();
                runtimeArgs.Add(new NamedArg("contract_whitelist", CLValue.List(contracts)));
            }

            if (installArgs.BurnMode != null)
                runtimeArgs.Add(new NamedArg("burn_mode",
                    CLValue.Option(CLValue.U8((byte) installArgs.BurnMode.Value))));

            var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

            var deploy = new Deploy(header, payment, session);

            return new DeployHelper(deploy, CasperClient, ProcessDeployResult);
        }

        public DeployHelper SetVariables(PublicKey ownerPk,
            bool? allowMinting,
            IEnumerable<HashKey> ContractWhiteList,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>();
            
            if(allowMinting != null)
                namedArgs.Add(new NamedArg("allow_minting", allowMinting.Value));
            
            if (ContractWhiteList != null)
            {
                if (!ContractWhiteList.Any())
                    throw new ContractException("ContractWhitelist must contain at least one entry", (long)CEP78ClientErrors.EmptyContractWhitelist);
                var contracts = ContractWhiteList.Select(c => CLValue.ByteArray(c.RawBytes)).ToArray();
                namedArgs.Add(new NamedArg("contract_whitelist", CLValue.List(contracts)));
            }

            if (!namedArgs.Any())
                throw new ContractException("No variables to set.", (long)CEP78ClientErrors.OtherError);
            
            return BuildDeployHelper("set_variables",
                namedArgs,
                ownerPk,
                paymentMotes,
                ttl);
        }

        public DeployHelper Mint(PublicKey minterPk,
            GlobalStateKey recipientKey,
            object tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var jsonMetadata = JsonSerializer.Serialize(tokenMetadata);
            
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_owner", recipientKey),
                new NamedArg("token_meta_data", jsonMetadata)
            };
            
            return BuildDeployHelper("mint",
                namedArgs,
                minterPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper Burn(PublicKey senderPk,
            ulong tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_id", tokenId),
            };
            
            return BuildDeployHelper("burn",
                namedArgs,
                senderPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper Burn(PublicKey senderPk,
            string tokenHash,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_hash", tokenHash),
            };
            
            return BuildDeployHelper("burn",
                namedArgs,
                senderPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper Approve(PublicKey senderPk,
            ulong tokenId,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_id", tokenId),
                new NamedArg("operator", operatorKey),
            };
            
            return BuildDeployHelper("approve",
                namedArgs,
                senderPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper Approve(PublicKey senderPk,
            string tokenHash,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_hash", tokenHash),
                new NamedArg("operator", operatorKey),
            };
            
            return BuildDeployHelper("approve",
                namedArgs,
                senderPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper ApproveAll(PublicKey senderPk,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("approve_all", true),
                new NamedArg("operator", operatorKey),
            };
            
            return BuildDeployHelper("set_approval_for_all",
                namedArgs,
                senderPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper RemoveApproveAll(PublicKey senderPk,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("approve_all", false),
                new NamedArg("operator", operatorKey),
            };
            
            return BuildDeployHelper("approve",
                namedArgs,
                senderPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper Transfer(PublicKey callerPk,
            ulong tokenId,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_id", tokenId),
                new NamedArg("source_key", ownerKey),
                new NamedArg("target_key", recipientKey),
            };
            
            return BuildDeployHelper("transfer",
                namedArgs,
                callerPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper Transfer(PublicKey callerPk,
            string tokenHash,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_hash", tokenHash),
                new NamedArg("source_key", ownerKey),
                new NamedArg("target_key", recipientKey),
            };
            
            return BuildDeployHelper("transfer",
                namedArgs,
                callerPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper SetTokenMetadata(PublicKey callerPk,
            ulong tokenId,
            ITokenMetadata tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var metadata = tokenMetadata.Serialize();
            
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_id", tokenId),
                new NamedArg("token_meta_data", metadata),
            };
            
            return BuildDeployHelper("set_token_metadata",
                namedArgs,
                callerPk,
                paymentMotes,
                ttl);
        }
        
        public DeployHelper SetTokenMetadata(PublicKey callerPk,
            string tokenHash,
            ITokenMetadata tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var metadata = tokenMetadata.Serialize();
            
            var namedArgs = new List<NamedArg>()
            {
                new NamedArg("token_hash", tokenHash),
                new NamedArg("token_meta_data", metadata),
            };
            
            return BuildDeployHelper("set_token_metadata",
                namedArgs,
                callerPk,
                paymentMotes,
                ttl);
        }

        public async Task<ulong> GetBalanceOf(GlobalStateKey ownerKey)
        {
            try
            {
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    "balances", ownerKey.ToHexString().ToLower());
                
                return result.Parse().StoredValue.CLValue.ToUInt64();
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> account not known
                    return 0;
                
                throw;
            }
        }

        public async Task<GlobalStateKey> GetOwnerOf(ulong tokenId)
        {
            return await GetOwnerOf(tokenId.ToString());
        }

        public async Task<GlobalStateKey> GetOwnerOf(string tokenHash)
        {
            try
            {
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    "token_owners", tokenHash);
                
                return result.Parse().StoredValue.CLValue.ToGlobalStateKey();
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> account not known
                    return null;
                
                throw;
            }
        }

        /// <summary>
        /// Gets a list of tokens owned by a key
        /// </summary>
        /// <param name="ownerKey"></param>
        /// <typeparam name="TTokenIdentifier">only `ulong` or `string` allowed</typeparam>
        /// <returns></returns>
        public async Task<IEnumerable<TTokenIdentifier>> GetOwnedTokens<TTokenIdentifier>(GlobalStateKey ownerKey)
        {
            try
            {
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    "owned_tokens", ownerKey.ToHexString().ToLower());
                
                return result.Parse().StoredValue.CLValue.ToList<TTokenIdentifier>();
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> account not known
                    return new List<TTokenIdentifier>();
                
                throw;
            }
        }

        public async Task<GlobalStateKey> GetFirstOwnerOf(ulong tokenId)
        {
            return await GetFirstOwnerOf(tokenId.ToString());
        }

        public async Task<GlobalStateKey> GetFirstOwnerOf(string tokenHash)
        {
            try
            {
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    "token_issuers", tokenHash);
                
                return result.Parse().StoredValue.CLValue.ToGlobalStateKey();
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> account not known
                    return null;
                
                throw;
            }
        }

        public async Task<string> GetRawMetadata(ulong tokenId)
        {
            return await GetRawMetadata(tokenId.ToString());
        }
        
        public async Task<string> GetRawMetadata(string tokenHash)
        {
            try
            {
                if (_metadataKind is null)
                    _metadataKind = await GetNFTMetadataKind();

                var dictionaryName = _metadataKind switch
                {
                    NFTMetadataKind.CEP78 => "metadata_cep78",
                    NFTMetadataKind.NFT721 => "metadata_nft721",
                    NFTMetadataKind.CustomValidated => "metadata_custom_validated",
                    _ => "metadata_raw"
                };
                
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    dictionaryName, tokenHash);

                var jsonString = result.Parse().StoredValue.CLValue.ToString();

                return jsonString;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> account not known
                    throw;
                
                throw;
            }
        }
        
        public async Task<T> GetMetadata<T>(ulong tokenId) where T : ITokenMetadata
        {
            return await GetMetadata<T>(tokenId.ToString());
        }
        
        public async Task<T> GetMetadata<T>(string tokenHash) where T : ITokenMetadata
        {
            var jsonString = await GetRawMetadata(tokenHash);

            return (T) ITokenMetadata.Deserialize<T>(jsonString);
        }

        public async Task<GlobalStateKey> GetApproved(ulong tokenId)
        {
            return await GetApproved(tokenId.ToString());
        }
        
        public async Task<GlobalStateKey> GetApproved(string tokenHash)
        {
            try
            {
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    "operator", tokenHash);

                var option = result.Parse().StoredValue.CLValue;
                
                return option.Some<GlobalStateKey>(out var operatorKey) ? operatorKey : null;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> token not known or no approval
                    return null;
                
                throw;
            }
        }

        public async Task<bool> IsTokenBurned(ulong tokenId)
        {
            return await IsTokenBurned(tokenId.ToString());
        }
        
        public async Task<bool> IsTokenBurned(string tokenHash)
        {
            try
            {
                var result = await CasperClient.GetDictionaryItemByContract(ContractHash.ToString(), 
                    "burnt_tokens", tokenHash);

                // an entry in the dictionary means the token is burned, regardless the value of the entry 
                var value = result.Parse().StoredValue.CLValue;

                return true;
            }
            catch (RpcClientException e)
            {
                if (e.RpcError.Code.Equals(-32003)) //Dictionary item not found -> token not burned
                    return false;
                
                throw;
            }
        }
    }
    
    /// <summary>
    /// Enumeration with common CEP78 related errors that can be returned by the CEP78 contract or the client.
    /// </summary>
    public enum CEP78ClientErrors
    {
        OtherError = 0,
        InvalidAccount = 1,
        MissingInstaller = 2,
        InvalidInstaller = 3,
        UnexpectedKeyVariant = 4,
        MissingTokenOwner = 5,
        InvalidTokenOwner = 6,
        FailedToGetArgBytes = 7,
        FailedToCreateDictionary = 8,
        MissingStorageUref = 9,
        InvalidStorageUref = 10,
        MissingOwnersUref = 11,
        InvalidOwnersUref = 12,
        FailedToAccessStorageDictionary = 13,
        FailedToAccessOwnershipDictionary = 14,
        DuplicateMinted = 15,
        FailedToConvertToCLValue = 16,
        MissingCollectionName = 17,
        InvalidCollectionName = 18,
        FailedToSerializeMetaData = 19,
        MissingAccount = 20,
        MissingMintingStatus = 21,
        InvalidMintingStatus = 22,
        MissingCollectionSymbol = 23,
        InvalidCollectionSymbol = 24,
        MissingTotalTokenSupply = 25,
        InvalidTotalTokenSupply = 26,
        MissingTokenID = 27,
        InvalidTokenIdentifier = 28,
        MissingTokenOwners = 29,
        MissingAccountHash = 30,
        InvalidAccountHash = 31,
        TokenSupplyDepleted = 32,
        MissingOwnedTokensDictionary = 33,
        TokenAlreadyBelongsToMinterFatal = 34,
        FatalTokenIdDuplication = 35,
        InvalidMinter = 36,
        MissingMintingMode = 37,
        InvalidMintingMode = 38,
        MissingInstallerKey = 39,
        FailedToConvertToAccountHash = 40,
        InvalidBurner = 41,
        PreviouslyBurntToken = 42,
        MissingAllowMinting = 43,
        InvalidAllowMinting = 44,
        MissingNumberOfMintedTokens = 45,
        InvalidNumberOfMintedTokens = 46,
        MissingTokenMetaData = 47,
        InvalidTokenMetaData = 48,
        MissingApprovedAccountHash = 49,
        InvalidApprovedAccountHash = 50,
        MissingApprovedTokensDictionary = 51,
        TokenAlreadyApproved = 52,
        MissingApproveAll = 53,
        InvalidApproveAll = 54,
        MissingOperator = 55,
        InvalidOperator = 56,
        Phantom = 57,
        ContractAlreadyInitialized = 58,
        MintingIsPaused = 59,
        FailureToParseAccountHash = 60,
        VacantValueInDictionary = 61,
        MissingOwnershipMode = 62,
        InvalidOwnershipMode = 63,
        InvalidTokenMinter = 64,
        MissingOwnedTokens = 65,
        InvalidAccountKeyInDictionary = 66,
        MissingJsonSchema = 67,
        InvalidJsonSchema = 68,
        InvalidKey = 69,
        InvalidOwnedTokens = 70,
        MissingTokenURI = 71,
        InvalidTokenURI = 72,
        MissingNftKind = 73,
        InvalidNftKind = 74,
        MissingHolderMode = 75,
        InvalidHolderMode = 76,
        MissingWhitelistMode = 77,
        InvalidWhitelistMode = 78,
        MissingContractWhiteList = 79,
        InvalidContractWhitelist = 80,
        UnlistedContractHash = 81,
        InvalidContract = 82,
        EmptyContractWhitelist = 83,
        MissingReceiptName = 84,
        InvalidReceiptName = 85,
        InvalidJsonMetadata = 86,
        InvalidJsonFormat = 87,
        FailedToParseCep99Metadata = 88,
        FailedToParse721Metadata = 89,
        FailedToParseCustomMetadata = 90,
        InvalidCEP99Metadata = 91,
        FailedToJsonifyCEP99Metadata = 92,
        InvalidNFT721Metadata = 93,
        FailedToJsonifyNFT721Metadata = 94,
        InvalidCustomMetadata = 95,
        MissingNFTMetadataKind = 96,
        InvalidNFTMetadataKind = 97,
        MissingIdentifierMode = 98,
        InvalidIdentifierMode = 99,
        FailedToParseTokenId = 100,
        MissingMetadataMutability = 101,
        InvalidMetadataMutability = 102,
        FailedToJsonifyCustomMetadata = 103,
        ForbiddenMetadataUpdate = 104,
        MissingBurnMode = 105,
        InvalidBurnMode = 106,
    }
}
