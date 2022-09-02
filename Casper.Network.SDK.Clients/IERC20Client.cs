using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public interface IERC20Client
    {
        /// <summary>
        /// Gets the name of the ERC20 token
        /// </summary>
        Task<string> GetName();

        /// <summary>
        /// Gets the symbol of the ERC20 token
        /// </summary>
        Task<string> GetSymbol();

        /// <summary>
        /// Gets the decimals number of the ERC20 token.
        /// </summary>
        Task<byte> GetDecimals();

        /// <summary>
        /// Gets the total supply of the ERC20 token.
        /// </summary>
        Task<BigInteger> GetTotalSupply();

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
        /// Prepares a Deploy to make a new install of the ERC20 contract with the given details.
        /// </summary>
        /// <param name="wasmBytes">Contract to deploy in WASM format.</param>
        /// <param name="name">Name of the ERC20 token.</param>
        /// <param name="symbol">Symbol of the ERC20 token.</param>
        /// <param name="decimals">Number of decimals of the ERC20 token.</param>
        /// <param name="totalSupply">Total supply for the new ERC20 contract.</param>
        /// <param name="accountPK">Caller account and owner of the contract.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper InstallContract(byte[] wasmBytes,
            string name,
            string symbol,
            byte decimals,
            BigInteger totalSupply,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000
        );

        /// <summary>
        /// Prepares a Deploy to make a transfer of tokens to a recipient account.
        /// </summary>
        /// <param name="ownerPK">Caller account and owner of the tokens to send.</param>
        /// <param name="recipientKey">Recipient account of the tokens.</param>
        /// <param name="amount">Amount of tokens to send (with decimals).</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper TransferTokens(PublicKey ownerPK,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to approve a spender to make transfers of tokens on behalf of the owner.
        /// </summary>
        /// <param name="ownerPK">Caller account and owner of the tokens that the spender will be able to transfer after the approval.</param>
        /// <param name="spenderKey">The account that is being approved to transfer tokens from the owner.</param>
        /// <param name="amount">Amount of tokens to approve for transfers.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper ApproveSpender(PublicKey ownerPK,
            GlobalStateKey spenderKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Prepares a Deploy to transfer tokens to a recipient on behalf of the tokens owner. 
        /// </summary>
        /// <param name="spenderPK">The approved account to transfer tokens on behal of the owner.</param>
        /// <param name="ownerKey">The tokens owner account.</param>
        /// <param name="recipientKey">The recipient account of the tokens. </param>
        /// <param name="amount">Amount of tokens to transfer (with decimals).</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
        DeployHelper TransferTokensFromOwner(PublicKey spenderPK,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        /// <summary>
        /// Retrieves the balance of tokens for the given account.
        /// </summary>
        /// <param name="ownerKey">Account to check the balance of.</param>
        /// <returns>The balance of tokens with decimals.</returns>
        Task<BigInteger> GetBalance(GlobalStateKey ownerKey);

        /// <summary>
        /// Retrieves the remaining amount of tokens that the owner approved to the spender to transfer on his behalf.
        /// </summary>
        /// <param name="ownerKey">The owner's account.</param>
        /// <param name="spenderKey">The spender's account.</param>
        /// <returns>The amount of tokens with decimals that the spender may transfer on behalf of the owner.</returns>
        Task<BigInteger> GetAllowance(GlobalStateKey ownerKey, GlobalStateKey spenderKey);
    }
}
