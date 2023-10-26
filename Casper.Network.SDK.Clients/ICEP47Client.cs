using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public interface ICEP47Client
    {
        /// <summary>
        /// Gets the name of the CEP47 token
        /// </summary>
        Task<string> GetName();

        /// <summary>
        /// Gets the symbol of the CEP47 token
        /// </summary>
        Task<string> GetSymbol();

        /// <summary>
        /// Gets the metadata of the CEP47 token
        /// </summary>
        Task<Dictionary<string, string>> GetMetadata();

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
        /// Prepares a Deploy to make a new install of the CEP47 contract with the given details.
        /// </summary>
        /// <param name="wasmBytes">Contract to deploy in WASM format.</param>
        /// <param name="contractName">Name of the CEP47 contract (stored as a named key in the caller account).</param>
        /// <param name="name">Name of the CEP47 token.</param>
        /// <param name="symbol">Symbol of the CEP47 contract.</param>
        /// <param name="meta">Dictionary with the metadata of the CEP47 contract.</param>
        /// <param name="accountPK">Caller account and owner of the contract.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper InstallContract(byte[] wasmBytes,
            string contractName,
            string name,
            string symbol,
            Dictionary<string, string> meta,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Gets the number of tokens in circulation.
        /// </summary>
        Task<BigInteger> GetTotalSupply();
        
        /// <summary>
        /// Prepares a Deploy to mint a new token. 
        /// </summary>
        /// <param name="senderPk">Caller account.</param>
        /// <param name="recipientKey">Recipient and owner of the new token.</param>
        /// <param name="tokenId">Token identifier. </param>
        /// <param name="meta">Metadata of the token.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper MintOne(PublicKey senderPk,
            GlobalStateKey recipientKey,
            BigInteger? tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to mint several tokens in one call.
        /// </summary>
        /// <param name="senderPk">Caller account.</param>
        /// <param name="recipientKey">Recipient and owner of the new token.</param>
        /// <param name="tokenIds">List of token identifiers.</param>
        /// <param name="metas">List of token metadatas (one dictionary per token).</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper MintMany(PublicKey senderPk,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            List<Dictionary<string, string>> metas,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to mint several tokens in one call with the same metadata.
        /// </summary>
        /// <param name="ownerPK">Caller account.</param>
        /// <param name="recipientKey">Recipient and owner of the new tokens.</param>
        /// <param name="tokenIds">List of token identifiers.</param>
        /// <param name="meta">Metadata of the token (same for all copies).</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper MintCopies(PublicKey ownerPK,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to transfer a token to a recipient account.
        /// </summary>
        /// <param name="ownerPk">Caller account and owner of the tokens being sent.</param>
        /// <param name="recipientKey">Recipient account and new owner of the tokens after the execution of the deploy.</param>
        /// <param name="tokenIds">List of token identifiers to send to the recipient.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper TransferToken(PublicKey ownerPk,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
        
        DeployHelper TransferToken(PublicKey ownerPk,
            GlobalStateKey recipientKey,
            List<CLValue> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to approve a spender to make transfers of tokens on behalf of the owner.
        /// </summary>
        /// <param name="ownerPk">Caller account and owner of the tokens being approved for transfer.</param>
        /// <param name="spenderKey">The account that is being approved to transfer tokens from the owner.</param>
        /// <param name="tokenIds">List of token identifiers being approved for transfer.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper Approve(PublicKey ownerPk,
            GlobalStateKey spenderKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to transfer tokens to a recipient on behalf of the tokens owner. 
        /// </summary>
        /// <param name="senderPk">Caller account and approved spender.</param>
        /// <param name="ownerKey">Owner of the tokens being sent.</param>
        /// <param name="recipientKey">Recipient account and new owner of the tokens after the execution of the deploy.</param>
        /// <param name="tokenIds">List of token identifiers to send to the recipient.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper TransferTokenFrom(PublicKey senderPk,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Retrieves the token identifier given an index or position in the owner's token list.
        /// </summary>
        /// <param name="ownerKey">Owner's account</param>
        /// <param name="index">index or position in the owner's token list (from 0 to n-1).</param>
        /// <returns>A token Id or an exception with code TokenIdDoesntExist if the index is out of range.</returns>
        Task<BigInteger?> GetTokenIdByIndex(GlobalStateKey ownerKey, uint index);

        /// <summary>
        /// Retrieves the token metadata.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>A dictionary with the token metadata or an exception with code TokenIdDoesntExist if the token does not exist.</returns>
        Task<Dictionary<string, string>> GetTokenMetadata(BigInteger tokenId);

        /// <summary>
        /// Gets the account hash of the owner of a token.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>An account hash or an exception with code TokenIdDoesntExist if the index is out of range.</returns>
        Task<GlobalStateKey> GetOwnerOf(BigInteger tokenId);

        /// <summary>
        /// Gets the number of tokens owned by an account.
        /// </summary>
        /// <param name="ownerKey">Account hash of the owner being queried.</param>
        /// <returns>The number of tokens owned by the account or an exception with code UnknownAccount if the account is not known.</returns>
        Task<BigInteger?> GetBalanceOf(GlobalStateKey ownerKey);

        /// <summary>
        /// Gets the account hash of an approved spender for a given token
        /// </summary>
        /// <param name="ownerKey">Account hash of the owner.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <returns>The account hash of an approved spender, null if no spender approved or an exception with
        /// TokenIdDoesntExist if the token identifeier doesn't exist.</returns>
        Task<GlobalStateKey> GetApprovedSpender(GlobalStateKey ownerKey, BigInteger tokenId);

        /// <summary>
        /// Updates the metadata of a token.
        /// </summary>
        /// <param name="senderPk">Caller and owner of the token being updated.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="meta">New metadata of the token.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper UpdateTokenMetadata(PublicKey senderPk,
            BigInteger tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Burns one token.
        /// </summary>
        /// <param name="senderPk">Caller account (owner or spender) of the token being burned.</param>
        /// <param name="ownerKey">Owner account of the token.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper BurnOne(PublicKey senderPk,
            GlobalStateKey ownerKey,
            BigInteger tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
        
        DeployHelper BurnOne(PublicKey senderPk,
            GlobalStateKey ownerKey,
            CLValue tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Burns a list of tokens
        /// </summary>
        /// <param name="senderPk">Caller account (owner or spender) of the tokens being burned.</param>
        /// <param name="ownerKey">Owner account of the tokens.</param>
        /// <param name="tokenIds">List of token identifiers.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper BurnMany(PublicKey senderPk,
            GlobalStateKey ownerKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
        
        DeployHelper BurnMany(PublicKey senderPk,
            GlobalStateKey ownerKey,
            List<CLValue> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
    }
}
