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

        Task<ulong> GetNumberOfMintedTokens();
        
        Task<string> GetReceiptName();
        
        Task<GlobalStateKey> GetInstaller();
        
        /// <summary>
        /// Looks up the contract hash to use for all calls to the contract in a named key of an account.
        /// </summary>
        /// <param name="publicKey">Account that contains a named key with the contract hash.</param>
        /// <param name="namedKey">Named key that contains the contract hash.</param>
        Task SetContractHash(PublicKey publicKey, string namedKey);

        /// <summary>
        /// Sets the contract hash to use for all calls to the contract
        /// </summary>
        /// <param name="contractHash">Contract hash of the contract</param>
        void SetContractHash(string contractHash);

        /// <summary>
        /// Sets the contract hash to use for all calls to the contract
        /// </summary>
        /// <param name="contractHash">A valid Contract hash.</param>
        void SetContractHash(GlobalStateKey contractHash);

        /// <summary>
        /// Initialization of the client with a contract package hash allows to make contract method calls, but it does not allow
        /// to retrieve values from named keys (e.g. balances, token information, etc.). Use a contract hash when this information is needed.
        /// </summary>
        /// <param name="contractPackageHash">The contract package hash to use for all calls to the contract.</param>
        /// <param name="contractVersion">The version number of the contract to call. Use null to call latest version.</param>
        void SetContractPackageHash(GlobalStateKey contractPackageHash, uint? contractVersion);
        
        /// <summary>
        /// Prepares a Deploy to make a new install of the CEP78 contract with the given configuration.
        /// </summary>
        /// <param name="wasmBytes">Contract to deploy in WASM format.</param>
        /// <param name="installArgs">Settings for the new contract.</param>
        /// <param name="accountPK">Caller account and owner of the contract.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper InstallContract(byte[] wasmBytes,
            CEP78InstallArgs installArgs,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerPk"></param>
        /// <param name="allowMinting"></param>
        /// <param name="ContractWhiteList"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper SetVariables(PublicKey ownerPk,
            bool? allowMinting,
            IEnumerable<HashKey> ContractWhiteList,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="minterPk"></param>
        /// <param name="recipientKey"></param>
        /// <param name="tokenMetadata"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Mint(PublicKey minterPk,
            GlobalStateKey recipientKey,
            ITokenMetadata tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderPk"></param>
        /// <param name="tokenId"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Burn(PublicKey senderPk,
            ulong tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderPk"></param>
        /// <param name="tokenHash"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Burn(PublicKey senderPk,
            string tokenHash,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderPk"></param>
        /// <param name="tokenId"></param>
        /// <param name="operatorKey"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Approve(PublicKey senderPk,
            ulong tokenId,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderPk"></param>
        /// <param name="tokenHash"></param>
        /// <param name="operatorKey"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Approve(PublicKey senderPk,
            string tokenHash,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderPk"></param>
        /// <param name="operatorKey"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper ApproveAll(PublicKey senderPk,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="senderPk"></param>
        /// <param name="operatorKey"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper RemoveApproveAll(PublicKey senderPk,
            GlobalStateKey operatorKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callerPk"></param>
        /// <param name="tokenId"></param>
        /// <param name="ownerKey"></param>
        /// <param name="recipientKey"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Transfer(PublicKey callerPk,
            ulong tokenId,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callerPk"></param>
        /// <param name="tokenHash"></param>
        /// <param name="ownerKey"></param>
        /// <param name="recipientKey"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Transfer(PublicKey callerPk,
            string tokenHash,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callerPk"></param>
        /// <param name="tokenId"></param>
        /// <param name="tokenMetadata"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper SetTokenMetadata(PublicKey callerPk,
            ulong tokenId,
            ITokenMetadata tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callerPk"></param>
        /// <param name="tokenHash"></param>
        /// <param name="tokenMetadata"></param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper SetTokenMetadata(PublicKey callerPk,
            string tokenHash,
            ITokenMetadata tokenMetadata,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        Task<ulong> GetBalanceOf(GlobalStateKey ownerKey);

        Task<GlobalStateKey> GetOwnerOf(ulong tokenId);

        Task<GlobalStateKey> GetOwnerOf(string tokenHash);

        Task<IEnumerable<TTokenIdentifier>> GetOwnedTokenIdentifiers<TTokenIdentifier>(GlobalStateKey ownerKey);

        Task<GlobalStateKey> GetFirstOwnerOf(ulong tokenId);

        Task<GlobalStateKey> GetFirstOwnerOf(string tokenHash);

        Task<string> GetRawMetadata(ulong tokenId);

        Task<string> GetRawMetadata(string tokenHash);

        /// <summary>
        /// Gets the metadata of a token.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <typeparam name="T">A class that represents the metadata schema of the contract</typeparam>
        /// <returns></returns>
        Task<T> GetMetadata<T>(ulong tokenId) where T : ITokenMetadata;

        /// <summary>
        /// Gets the metadata of a token.
        /// </summary>
        /// <param name="tokenHash">The hash token identifier.</param>
        /// <typeparam name="T">A class that represents the metadata schema of the contract</typeparam>
        /// <returns></returns>
        Task<T> GetMetadata<T>(string tokenHash) where T : ITokenMetadata;

        /// <summary>
        /// Retrieves the key of an operator approved for transferring the token on behalf of the owner.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>An operator key or null if none has been approved.</returns>
        Task<GlobalStateKey> GetApproved(ulong tokenId);

        /// <summary>
        /// Retrieves the key of an operator approved for transferring the token on behalf of the owner.
        /// </summary>
        /// <param name="tokenHash">The hash token identifier.</param>
        /// <returns>An operator key or null if none has been approved.</returns>
        Task<GlobalStateKey> GetApproved(string tokenHash);

        /// <summary>
        /// Checks if a token is burned.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>true if the token is burned. False if the token is active or the token was not found.</returns>
        Task<bool> IsTokenBurned(ulong tokenId);

        /// <summary>
        /// Checks if a token is burned.
        /// </summary>
        /// <param name="tokenHash">The hash token identifier.</param>
        /// <returns>true if the token is burned. False if the token is active or the token was not found.</returns>
        Task<bool> IsTokenBurned(string tokenHash);
    }
}
