using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients.CEP78
{
    public interface ICEP78Client
    {
        Task<string> GetCollectionName();

        Task<string> GetCollectionSymbol();

        Task<ulong> GetTokenTotalSupply();

        Task<NFTOwnershipMode> GetOwnershipMode();

        Task<NFTKind> GetNFTKind();

        Task<NFTMetadataKind> GetNFTMetadataKind();

        Task<JsonSchema> GetJsonSchema();

        Task<NFTIdentifierMode> GetNFTIdentifierMode();

        Task<MetadataMutability> GetMetadataMutability();

        Task<MintingMode> GetMintingMode();

        Task<bool> GetAllowMinting();

        Task<WhitelistMode> GetWhitelistMode();

        Task<NFTHolderMode> GetNFTHolderMode();

        Task<IEnumerable<HashKey>> GetContractWhiteList();

        Task<BurnMode> GetBurnMode();
        
        DeployHelper InstallContract(byte[] wasmBytes,
            CEP78InstallArgs installArgs,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
        
        DeployHelper Mint(PublicKey minterPk,
            GlobalStateKey recipientKey,
            object tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
    }
}
