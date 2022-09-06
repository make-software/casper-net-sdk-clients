using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Utils;

namespace Casper.Network.SDK.Clients.CEP78
{
    /// <summary>
    /// Specifies the behavior regarding ownership of NFTs and whether the owner of the NFT can change over the
    /// contract's lifetime. It is a required installation parameter.
    /// </summary>
    /// <seealso href="https://github.com/casper-ecosystem/cep-78-enhanced-nft#ownership">https://github.com/casper-ecosystem/cep-78-enhanced-nft#ownership</seealso>
    /// 
    public enum NFTOwnershipMode
    {
        /// <summary>
        /// Minter mode is where the ownership of the newly minted NFT is attributed to the minter of the NFT
        /// and cannot be specified by the minter. In the Minter mode the owner of the NFT will not change and
        /// thus cannot be transferred to another entity.
        /// </summary>
        Minter,
        /// <summary>
        /// Assigned mode is where the owner of the newly minted NFT must be specified by the minter of the NFT.
        /// In this mode, the assigned entity can be either minter themselves or a separate entity. However,
        /// similar to the Minter mode, the ownership in this mode cannot be changed, and NFTs minted in this mode
        /// cannot be transferred from one entity to another.
        /// </summary>
        Assigned,
        /// <summary>
        /// In the Transferable mode the owner of the newly minted NFT must be specified by the minter. However,
        /// in the Transferable mode, NFTs can be transferred from the owner to another entity.
        /// </summary>
        Transferable
    }

    /// <summary>
    /// Specifies the commodity that NFTs minted by a particular contract will represent. Currently, the NFTKind
    /// modality does not alter or govern the behavior of the contract itself and only exists to specify the
    /// correlation between on-chain data and off-chain items. It is a required installation parameter.
    /// </summary>
    /// <seealso href="https://github.com/casper-ecosystem/cep-78-enhanced-nft#nftkind">https://github.com/casper-ecosystem/cep-78-enhanced-nft#ownership</seealso>
    public enum NFTKind
    {
        /// <summary>
        /// The NFT represents a real-world physical item e.g., a house.
        /// </summary>
        Physical,
        /// <summary>
        /// The NFT represents a digital item, e.g., a unique JPEG or digital art.
        /// </summary>
        Digital,
        /// <summary>
        /// The NFT is the virtual representation of a physical notion, e.g., a patent or copyright.
        /// </summary>
        Virtual
    }

    /// <summary>
    /// Dictates which entities on a Casper network can own and mint NFTs. It is an optional installation parameter
    /// and will default to the Mixed mode if not provided.
    /// </summary>
    /// <see href="https://github.com/casper-ecosystem/cep-78-enhanced-nft#nftholdermode"/>
    public enum NFTHolderMode
    {
        /// <summary>
        /// In this mode, only Accounts can own and mint NFTs.
        /// </summary>
        Accounts,
        /// <summary>
        /// In this mode, only Contracts can own and mint NFTs.
        /// </summary>
        Contracts,
        /// <summary>
        /// In this mode both Accounts and Contracts can own and mint NFTs.
        /// </summary>
        Mixed
    }

    /// <summary>
    /// Dictates if the contract whitelist restricting access to the mint entrypoint can be updated. It is an
    /// optional installation parameter and will be set to unlocked if not passed.
    /// </summary>
    public enum WhitelistMode
    {
        /// <summary>
        /// The contract whitelist is unlocked and can be updated via the set variables endpoint.
        /// </summary>
        Unlocked,
        /// <summary>
        /// The contract whitelist is locked and cannot be updated further.
        /// </summary>
        Locked
    }

    /// <summary>
    /// Governs the behavior of contract when minting new tokens. It is an optional installation parameter
    /// and will default to the Installer mode if not provided.
    /// </summary>
    public enum MintingMode
    {
        /// <summary>
        /// This mode restricts the ability to mint new NFT tokens only to the installing account of the NFT contract.
        /// </summary>
        Installer,
        /// <summary>
        /// This mode allows any account to mint NFT tokens.
        /// </summary>
        Public
    }

    /// <summary>
    /// Dictates the schema for the metadata for NFTs minted by a given instance of an NFT contract.
    /// </summary>
    public enum NFTMetadataKind
    {
        /// <summary>
        /// This mode specifies that NFTs minted must have valid metadata conforming to the CEP-78 schema.
        /// </summary>
        CEP78,
        /// <summary>
        /// This mode specifies that NFTs minted must have valid metadata conforming to the NFT-721 metadata schema.
        /// </summary>
        NFT721,
        /// <summary>
        /// This mode specifies that metadata validation will not occur and raw strings can be passed to
        /// token_metadata runtime argument as part of the call to mint entry point.
        /// </summary>
        Raw,
        /// <summary>
        /// This mode specifies that metadata validation will not occur and raw strings can be passed to
        /// token_metadata runtime argument as part of the call to mint entry point.
        /// </summary>
        CustomValidated
    }

    /// <summary>
    /// Governs the primary identifier for NFTs minted for a given instance on an installed contract. It is a
    /// required installation parameter
    /// </summary>
    public enum NFTIdentifierMode
    {
        /// <summary>
        /// NFTs minted in this modality are identified by a u64 value. This value is determined by the number
        /// of NFTs minted by the contract at the time the NFT is minted.
        /// </summary>
        Ordinal,
        /// <summary>
        /// NFTs minted in this modality are identified by a base16 encoded representation of the blake2b hash
        /// of the metadata provided at the time of mint.
        /// </summary>
        Hash
    }

    /// <summary>
    /// Governs the behavior around updates to a given NFTs metadata. The Mutable option cannot be used in
    /// conjunction with the Hash modality for the NFT identifier It is a required installation parameter.
    /// </summary>
    public enum MetadataMutability
    {
        /// <summary>
        /// Metadata for NFTs minted in this mode cannot be updated once the NFT has been minted.
        /// </summary>
        Immutable,
        /// <summary>
        /// Metadata for NFTs minted in this mode can update the metadata via the set_token_metadata entry point.
        /// </summary>
        Mutable
    }

    /// <summary>
    /// Dictates whether tokens minted by a given instance of an NFT contract can be burnt.
    /// </summary>
    public enum BurnMode
    {
        /// <summary>
        /// Minted tokens can be burnt.
        /// </summary>
        Burnable,
        /// <summary>
        /// Minted tokens cannot be burnt.
        /// </summary>
        NonBurnable
    }

    /// <summary>
    /// A property definition within the custom json schema of the tokens metadata.
    /// </summary>
    public class JsonSchemaEntry
    {
        /// <summary>
        /// Name of the property in the schema.
        /// </summary>
        [JsonPropertyName("name")] public string Name { get; set; }

        /// <summary>
        /// Description of the property in the schema.
        /// </summary>
        [JsonPropertyName("description")] public string Description { get; set; }

        /// <summary>
        /// Whether the property is required (true) or optional (false).
        /// </summary>
        [JsonPropertyName("required")] public bool Required { get; set; }
    }

    /// <summary>
    /// A custom schema for tokens metadata set during contract installation. It must be used in conjunction with
    /// installation property NFTMetadataKind.CustomValidated. Once provided, the schema for a given instance of
    /// he contract cannot be changed.
    /// </summary>
    public class JsonSchema
    {
        /// <summary>
        /// Each property has a name, the description of the property itself, and whether the property is required
        /// to be present in the metadata.
        /// </summary>
        [JsonPropertyName("properties")] public Dictionary<string, JsonSchemaEntry> Properties { get; set; }

        public JsonSchema()
        {
            Properties = new Dictionary<string, JsonSchemaEntry>();
        }
    }

    /// <summary>
    /// An interface that a custom token metadata class must implement to be used with the client. See
    /// CEP78TokenMetadata and NFT721tokenMetadata for examples of implementation.
    /// </summary>
    public interface ITokenMetadata
    {
        string Serialize();

        static ITokenMetadata Deserialize<T>(string json) where T : ITokenMetadata
        {
            return JsonSerializer.Deserialize<T>(json);
        }
    }

    /// <summary>
    /// Metadata conforming to the CEP78 metadata schema.
    /// </summary>
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

    /// <summary>
    /// Metadata conforming to the NFT-721 metadata schema.
    /// </summary>
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

    /// <summary>
    /// Set of configuration parameters used during the installation of the contract
    /// </summary>
    public class CEP78InstallArgs
    {
        /// <summary>
        /// Name of the contract.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Symbols of the contract.
        /// </summary>
        public string CollectionSymbol { get; set; }

        /// <summary>
        /// Maximum number of tokens than will be ever issued by the contract.
        /// </summary>
        public ulong TokenTotalSupply { get; set; }

        /// <summary>
        /// Specifies the behavior regarding ownership of NFTs and whether the owner of the NFT can change over the
        /// contract's lifetime. It is a required installation parameter.
        /// </summary>
        public NFTOwnershipMode OwnershipMode { get; set; }

        /// <summary>
        /// Specifies the commodity that NFTs minted by a particular contract will represent. Currently, the NFTKind
        /// modality does not alter or govern the behavior of the contract itself and only exists to specify the
        /// correlation between on-chain data and off-chain items. It is a required installation parameter.
        /// </summary>
        public NFTKind NFTKind { get; set; }

        /// <summary>
        /// Dictates the schema for the metadata for NFTs minted by a given instance of an NFT contract.
        /// </summary>
        public NFTMetadataKind NFTMetadataKind { get; set; }

        /// <summary>
        /// A definition of the token metadata schema. Used in conjunction with NFTMetadataKind.CustomValidated.
        /// </summary>
        public JsonSchema JsonSchema { get; set; }

        /// <summary>
        /// Governs the primary identifier for NFTs minted for a given instance on an installed contract. It is a
        /// required installation parameter
        /// </summary>
        public NFTIdentifierMode NFTIdentifierMode { get; set; }

        /// <summary>
        /// Governs the behavior around updates to a given NFTs metadata. The Mutable option cannot be used in
        /// conjunction with the Hash modality for the NFT identifier It is a required installation parameter.
        /// </summary>
        public MetadataMutability MetadataMutability { get; set; }

        /// <summary>
        /// Governs the behavior of contract when minting new tokens. It is an optional installation parameter
        /// and will default to the Installer mode if not provided.
        /// </summary>
        public MintingMode? MintingMode { get; set; }

        /// <summary>
        /// Optional parameter that dictates if minting of tokens is enabled. This value can be changed afterwards with
        /// the SetVariables() method.
        /// </summary>
        public bool? AllowMinting { get; set; }

        /// <summary>
        /// Dictates if the contract whitelist restricting access to the mint entrypoint can be updated. It is an
        /// optional installation parameter and will be set to unlocked if not passed.
        /// </summary>
        public WhitelistMode? WhitelistMode { get; set; }

        /// <summary>
        /// Dictates which entities on a Casper network can own and mint NFTs. It is an optional installation parameter
        /// and will default to the Mixed mode if not provided.
        /// </summary>
        public NFTHolderMode? NFTHolderMode { get; set; }

        /// <summary>
        /// A list of contracts that are allowed to mint tokens. This whitelist dictates which Contracts are allowed
        /// to mint NFTs in the restricted Installer minting mode. 
        /// </summary>
        public IEnumerable<HashKey> ContractWhiteList { get; set; }

        /// <summary>
        /// Dictates whether tokens minted by a given instance of an NFT contract can be burnt.
        /// </summary>
        public BurnMode? BurnMode { get; set; }
    }

    public class CEP78Client : ClientBase, ICEP78Client
    {
        private NFTMetadataKind? _metadataKind;

        /// <summary>
        /// Gets the contract name.
        /// </summary>
        public async Task<string> GetCollectionName() =>
            (await GetNamedKey<CLValue>("collection_name")).ToString();

        /// <summary>
        /// Gets the contract symbol.
        /// </summary>
        public async Task<string> GetCollectionSymbol() =>
            (await GetNamedKey<CLValue>("collection_symbol")).ToString();

        /// <summary>
        /// Gets to maximum number of tokens that may be issued in this contract.
        /// </summary>
        public async Task<ulong> GetTokenTotalSupply() =>
            (await GetNamedKey<CLValue>("total_token_supply")).ToUInt64();

        /// <summary>
        /// Gets the ownership mode.
        /// </summary>
        public async Task<NFTOwnershipMode> GetOwnershipMode() =>
            (NFTOwnershipMode) (await GetNamedKey<CLValue>("ownership_mode")).ToByte();

        /// <summary>
        /// Gets thd NFTKind variable.
        /// </summary>
        public async Task<NFTKind> GetNFTKind() =>
            (NFTKind) (await GetNamedKey<CLValue>("nft_kind")).ToByte();

        /// <summary>
        /// Gets the NFTMetadataKind variable.
        /// </summary>
        public async Task<NFTMetadataKind> GetNFTMetadataKind() =>
            (NFTMetadataKind) (await GetNamedKey<CLValue>("nft_metadata_kind")).ToByte();

        /// <summary>
        /// Gets the json schema. Valid only for CustomValidated metadata kind.
        /// </summary>
        public async Task<JsonSchema> GetJsonSchema()
        {
            var json = (await GetNamedKey<CLValue>("json_schema")).ToString();
            
            if (string.IsNullOrWhiteSpace(json))
                return new JsonSchema();
            
            return JsonSerializer.Deserialize<JsonSchema>(json);
        }

        /// <summary>
        /// Gets the NFTIdentifierMode variable.
        /// </summary>
        public async Task<NFTIdentifierMode> GetNFTIdentifierMode() =>
            (NFTIdentifierMode) (await GetNamedKey<CLValue>("identifier_mode")).ToByte();

        /// <summary>
        /// Gets the MetadataMutability variable.
        /// </summary>
        public async Task<MetadataMutability> GetMetadataMutability() =>
            (MetadataMutability) (await GetNamedKey<CLValue>("metadata_mutability")).ToByte();

        /// <summary>
        /// Gets the MintingMode variable.
        /// </summary>
        public async Task<MintingMode> GetMintingMode() =>
            (MintingMode) (await GetNamedKey<CLValue>("minting_mode")).ToByte();

        /// <summary>
        /// Gets the AllowMinting variable.
        /// </summary>
        public async Task<bool> GetAllowMinting() =>
            (await GetNamedKey<CLValue>("allow_minting")).ToBoolean();

        /// <summary>
        /// Gets the contract WhitelistMode variable.
        /// </summary>
        public async Task<WhitelistMode> GetWhitelistMode() =>
            (WhitelistMode) (await GetNamedKey<CLValue>("whitelist_mode")).ToByte();

        /// <summary>
        /// Gets the NFTHolderName variable.
        /// </summary>
        public async Task<NFTHolderMode> GetNFTHolderMode() =>
            (NFTHolderMode) (await GetNamedKey<CLValue>("holder_mode")).ToByte();

        /// <summary>
        /// Gets the ContractWhitelist variable.
        /// </summary>
        public async Task<IEnumerable<HashKey>> GetContractWhiteList()
        {
            var value = await GetNamedKey<CLValue>("contract_whitelist");
            return value.ToList<byte[]>().Select(hash => new HashKey(hash));
        }

        /// <summary>
        /// Gets the BurnMode variable.
        /// </summary>
        public async Task<BurnMode> GetBurnMode() =>
            (BurnMode) (await GetNamedKey<CLValue>("burn_mode")).ToByte();

        /// <summary>
        /// Gets the NumberOfMintedTokens variable.
        /// </summary>
        public async Task<ulong> GetNumberOfMintedTokens() =>
            (await GetNamedKey<CLValue>("number_of_minted_tokens")).ToUInt64();

        /// <summary>
        /// Gets the ReceiptName variable.
        /// </summary>
        public async Task<string> GetReceiptName() =>
            (await GetNamedKey<CLValue>("receipt_name")).ToString();

        /// <summary>
        /// Gets the installer account hash key.
        /// </summary>
        public async Task<GlobalStateKey> GetInstaller()
        {
            var response = await CasperClient.QueryGlobalState(ContractHash, null, "installer");
            var result = response.Parse();

            if (result.StoredValue.Account != null)
                return result.StoredValue.Account.AccountHash;

            throw new ContractException("Unexpected response for get 'installer' named key.",
                (long) CEP78ClientErrors.InvalidAccount);
        }

        /// <summary>
        /// Constructor of the client. Call SetContractHash or SetContractPackageHash before any other method. 
        /// </summary>
        /// <param name="casperClient">A valid ICasperClient object.</param>
        /// <param name="chainName">Name of the network being used.</param>
        public CEP78Client(ICasperClient casperClient, string chainName)
            : base(casperClient, chainName)
        {
            ProcessDeployResult = result =>
            {
                var executionResult = result.ExecutionResults.FirstOrDefault();
                
                if (executionResult is null)
                    throw new ContractException("ExecutionResults null for processed deploy.",
                        (long) CEP78ClientErrors.OtherError);

                if (executionResult.IsSuccess)
                    return;

                var match = Regex.Match(executionResult.ErrorMessage, "User error: ([0-9]+)$");
                if (match.Success && match.Groups.Count > 1)
                {
                    var errorCode  = int.Parse(match.Groups[1].Value);
                    throw new ContractException("Deploy not executed. " + ((CEP78ClientErrors) errorCode).ToString(),
                        errorCode);
                }

                throw new ContractException(executionResult.ErrorMessage, (long)CEP78ClientErrors.OtherError);
            };
        }

        /// <summary>
        /// Prepares a Deploy to make a new install of the CEP47 contract with the given details.
        /// </summary>
        /// <param name="wasmBytes">Contract to deploy in WASM format.</param>
        /// <param name="installArgs">Installation arguments.</param>
        /// <param name="accountPK">Caller account and owner of the contract.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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
                    CLValue.U8((byte) installArgs.MintingMode)));

            if (installArgs.AllowMinting != null)
                runtimeArgs.Add(new NamedArg("allow_minting",
                    CLValue.Bool(installArgs.AllowMinting.Value)));

            if (installArgs.WhitelistMode != null)
                runtimeArgs.Add(new NamedArg("whitelist_mode",
                    CLValue.U8((byte) installArgs.WhitelistMode.Value)));

            if (installArgs.NFTHolderMode != null)
                runtimeArgs.Add(new NamedArg("holder_mode",
                    CLValue.U8((byte) installArgs.NFTHolderMode.Value)));

            if (installArgs.ContractWhiteList != null && installArgs.ContractWhiteList.Any())
            {
                var contracts = installArgs.ContractWhiteList.Select(c => CLValue.ByteArray(c.RawBytes)).ToArray();
                runtimeArgs.Add(new NamedArg("contract_whitelist", CLValue.List(contracts)));
            }

            if (installArgs.BurnMode != null)
                runtimeArgs.Add(new NamedArg("burn_mode",
                    CLValue.U8((byte) installArgs.BurnMode.Value)));

            var session = new ModuleBytesDeployItem(wasmBytes, runtimeArgs);

            var deploy = new Deploy(header, payment, session);

            return new DeployHelper(deploy, CasperClient, ProcessDeployResult);
        }

        /// <summary>
        /// Prepares a Deploy to change the allow_minting and contract_whitelist variables in the contract.
        /// </summary>
        /// <param name="ownerPk"></param>
        /// <param name="allowMinting"></param>
        /// <param name="ContractWhiteList"></param>
        /// <param name="paymentMotes"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        public DeployHelper SetVariables(PublicKey ownerPk,
            bool? allowMinting,
            IEnumerable<HashKey> ContractWhiteList,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var namedArgs = new List<NamedArg>();

            if (allowMinting != null)
                namedArgs.Add(new NamedArg("allow_minting", allowMinting.Value));

            if (ContractWhiteList != null)
            {
                if (!ContractWhiteList.Any())
                    throw new ContractException("ContractWhitelist must contain at least one entry",
                        (long) CEP78ClientErrors.EmptyContractWhitelist);
                var contracts = ContractWhiteList.Select(c => CLValue.ByteArray(c.RawBytes)).ToArray();
                namedArgs.Add(new NamedArg("contract_whitelist", CLValue.List(contracts)));
            }

            if (!namedArgs.Any())
                throw new ContractException("No variables to set.", (long) CEP78ClientErrors.OtherError);

            return BuildDeployHelper("set_variables",
                namedArgs,
                ownerPk,
                paymentMotes,
                ttl);
        }

        /// <summary>
        /// Prepares a Deploy to mint a new token. 
        /// </summary>
        /// <param name="minterPk">Caller account.</param>
        /// <param name="recipientKey">Recipient and owner of the new token.</param>
        /// <param name="tokenMetadata">Metadata of the token.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        public DeployHelper Mint(PublicKey minterPk,
            GlobalStateKey recipientKey,
            ITokenMetadata tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var jsonMetadata = tokenMetadata.Serialize();

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

        /// <summary>
        /// Prepares a Deploy to burn a token. 
        /// </summary>
        /// <param name="senderPk">Owner account.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to burn a token. 
        /// </summary>
        /// <param name="senderPk">Owner account.</param>
        /// <param name="tokenHash">Token identifier.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to Approve an operator for transferring a token on behalf of the owner. 
        /// </summary>
        /// <param name="senderPk">Owner account.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="operatorKey">Operator account being approved.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to Approve an operator for transferring a token on behalf of the owner
        /// </summary>
        /// <param name="senderPk">Owner account.</param>
        /// <param name="tokenHash">Token identifier.</param>
        /// <param name="operatorKey">Operator account being approved.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to Approve an operator for transferring any token owned by the caller on his behalf.
        /// </summary>
        /// <param name="senderPk">Owner account.</param>
        /// <param name="operatorKey">Operator account being approved.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to Approve an operator for transferring any token owned by the caller on his behalf.
        /// </summary>
        /// <param name="senderPk">Owner account.</param>
        /// <param name="operatorKey">Approved operator account.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to transfer a token to a new recipient.
        /// </summary>
        /// <param name="callerPk">Caller account.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="ownerKey">Token owner account</param>
        /// <param name="recipientKey">Recipients account.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to transfer a token to a new recipient.
        /// </summary>
        /// <param name="callerPk">Caller account.</param>
        /// <param name="tokenHash">Token identifier.</param>
        /// <param name="ownerKey">Token owner account</param>
        /// <param name="recipientKey">Recipients account.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to update the metadata of a token.
        /// </summary>
        /// <param name="callerPk">Owner account.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="tokenMetadata">New token metadata.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Prepares a Deploy to update the metadata of a token.
        /// </summary>
        /// <param name="callerPk">Owner account.</param>
        /// <param name="tokenHash">Token identifier.</param>
        /// <param name="tokenMetadata">New token metadata.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Gets the number of tokens owned by an account.
        /// </summary>
        /// <param name="ownerKey">Account hash of the owner being queried.</param>
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

        /// <summary>
        /// Gets the owner of a token.
        /// </summary>
        /// <param name="tokenId">Token identifier.</param>
        public async Task<GlobalStateKey> GetOwnerOf(ulong tokenId)
        {
            return await GetOwnerOf(tokenId.ToString());
        }

        /// <summary>
        /// Gets the owner of a token.
        /// </summary>
        /// <param name="tokenHash">Token identifier.</param>
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
        /// <param name="ownerKey">Account hash of the owner being queried.</param>
        /// <typeparam name="TTokenIdentifier">only `ulong` or `string` allowed</typeparam>
        public async Task<IEnumerable<TTokenIdentifier>> GetOwnedTokenIdentifiers<TTokenIdentifier>(GlobalStateKey ownerKey)
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

        /// <summary>
        /// Gets the first ever owner of a token.
        /// </summary>
        /// <param name="tokenId">Token identifier.</param>
        public async Task<GlobalStateKey> GetFirstOwnerOf(ulong tokenId)
        {
            return await GetFirstOwnerOf(tokenId.ToString());
        }

        /// <summary>
        /// Gets the first ever owner of a token.
        /// </summary>
        /// <param name="tokenHash">Token identifier.</param>
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

        /// <summary>
        /// Gets the raw metadata of a token. Valid only when NFTMetadataKind is CustomValidated.
        /// </summary>
        /// <param name="tokenId">Token identifier.</param>
        public async Task<string> GetRawMetadata(ulong tokenId)
        {
            return await GetRawMetadata(tokenId.ToString());
        }

        /// <summary>
        /// Gets the raw metadata of a token. Valid only when NFTMetadataKind is CustomValidated.
        /// </summary>
        /// <param name="tokenHash">Token identifier.</param>
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

        /// <summary>
        /// Gets token metadata as a parsed object of an specified class.
        /// </summary>
        /// <param name="tokenId">Token identifier.</param>
        /// <typeparam name="T">Type that implements ITokenMetadata.</typeparam>
        public async Task<T> GetMetadata<T>(ulong tokenId) where T : ITokenMetadata
        {
            return await GetMetadata<T>(tokenId.ToString());
        }

        /// <summary>
        /// Gets token metadata as a parsed object of an specified class.
        /// </summary>
        /// <param name="tokenHash">Token identifier.</param>
        /// <typeparam name="T">Type that implements ITokenMetadata.</typeparam>
        public async Task<T> GetMetadata<T>(string tokenHash) where T : ITokenMetadata
        {
            var jsonString = await GetRawMetadata(tokenHash);

            return (T) ITokenMetadata.Deserialize<T>(jsonString);
        }

        /// <summary>
        /// Gets the operator account key that has been approved for transferring the token on behalf of the owner.
        /// </summary>
        /// <param name="tokenId">Token identifier.</param>
        public async Task<GlobalStateKey> GetApproved(ulong tokenId)
        {
            return await GetApproved(tokenId.ToString());
        }

        /// <summary>
        /// Gets the operator account key that has been approved for transferring the token on behalf of the owner.
        /// </summary>
        /// <param name="tokenHash">Token identifier.</param>
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

        /// <summary>
        /// Checks if the token is burned.
        /// </summary>
        /// <param name="tokenId">Token identifier.</param>
        public async Task<bool> IsTokenBurned(ulong tokenId)
        {
            return await IsTokenBurned(tokenId.ToString());
        }

        /// <summary>
        /// Checks if the token is burned.
        /// </summary>
        /// <param name="tokenHash">Token identifier.</param>
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
