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
        /// <summary>
        /// Gets the name of the CEP47 token
        /// </summary>
        public async Task<string> GetName() =>
            (await GetNamedKey<CLValue>("name")).ToString();

        /// <summary>
        /// Gets the symbol of the CEP47 token
        /// </summary>
        public async Task<string> GetSymbol() =>
            (await GetNamedKey<CLValue>("symbol")).ToString();

        /// <summary>
        /// Gets the metadata of the CEP47 token
        /// </summary>
        public async Task<Dictionary<string, string>> GetMetadata() =>
            (await GetNamedKey<CLValue>("meta")).ToDictionary<string, string>();

        /// <summary>
        /// Constructor of the client. Call SetContractHash or SetContractPackageHash before any other method. 
        /// </summary>
        /// <param name="casperClient">A valid ICasperClient object.</param>
        /// <param name="chainName">Name of the network being used.</param>
        public CEP47Client(ICasperClient casperClient, string chainName)
            : base(casperClient, chainName)
        {
            ProcessDeployResult = result =>
            {
                var executionResult = result.ExecutionResults.FirstOrDefault();
                if (executionResult is null)
                    throw new ContractException("ExecutionResults null for processed deploy.",
                        (long) CEP47ClientErrors.OtherError);

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

            return new DeployHelper(deploy, CasperClient, ProcessDeployResult);
        }

        /// <summary>
        /// Gets the number of tokens in circulation.
        /// </summary>
        public async Task<BigInteger> GetTotalSupply()
        {
            var result = await GetNamedKey<CLValue>("total_supply");
            return result.ToBigInteger();
        }

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

            namedArgs.Add(new("token_meta", CLValue.Map(dict)));

            return BuildDeployHelper("mint_copies",
                namedArgs,
                ownerPK,
                paymentMotes,
                ttl);
        }

        /// <summary>
        /// Prepares a Deploy to transfer a token to a recipient account.
        /// </summary>
        /// <param name="ownerPk">Caller account and owner of the tokens being sent.</param>
        /// <param name="recipientKey">Recipient account and new owner of the tokens after the execution of the deploy.</param>
        /// <param name="tokenIds">List of token identifiers to send to the recipient.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        public DeployHelper TransferToken(PublicKey ownerPk,
            GlobalStateKey recipientKey,
            List<CLValue> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            var clTokenIds = CLValue.List(tokenIds.ToArray());

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
        
        /// <summary>
        /// Prepares a Deploy to approve a spender to make transfers of tokens on behalf of the owner.
        /// </summary>
        /// <param name="ownerPk">Caller account and owner of the tokens being approved for transfer.</param>
        /// <param name="spenderKey">The account that is being approved to transfer tokens from the owner.</param>
        /// <param name="tokenIds">List of token identifiers being approved for transfer.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Retrieves the token identifier given an index or position in the owner's token list.
        /// </summary>
        /// <param name="ownerKey">Owner's account</param>
        /// <param name="index">index or position in the owner's token list (from 0 to n-1).</param>
        /// <returns>A token Id or an exception with code TokenIdDoesntExist if the index is out of range.</returns>
        public async Task<BigInteger?> GetTokenIdByIndex(GlobalStateKey ownerKey, uint index)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query the contract keys.",
                    (long) CEP47ClientErrors.ContractNotFound);
            
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

        /// <summary>
        /// Retrieves the token metadata.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>A dictionary with the token metadata or an exception with code TokenIdDoesntExist if the token does not exist.</returns>
        public async Task<Dictionary<string, string>> GetTokenMetadata(BigInteger tokenId)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query the contract keys.",
                    (long) CEP47ClientErrors.ContractNotFound);
            
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

        /// <summary>
        /// Gets the account hash of the owner of a token.
        /// </summary>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>An account hash or an exception with code TokenIdDoesntExist if the index is out of range.</returns>
        public async Task<GlobalStateKey> GetOwnerOf(BigInteger tokenId)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query the contract keys.",
                    (long) CEP47ClientErrors.ContractNotFound);
            
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

        /// <summary>
        /// Gets the number of tokens owned by an account.
        /// </summary>
        /// <param name="ownerKey">Account hash of the owner being queried.</param>
        /// <returns>The number of tokens owned by the account or an exception with code UnknownAccount if the account is not known.</returns>
        public async Task<BigInteger?> GetBalanceOf(GlobalStateKey ownerKey)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query the contract keys.",
                    (long) CEP47ClientErrors.ContractNotFound);
            
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

        /// <summary>
        /// Gets the account hash of an approved spender for a given token
        /// </summary>
        /// <param name="ownerKey">Account hash of the owner.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <returns>The account hash of an approved spender, null if no spender approved or an exception with
        /// TokenIdDoesntExist if the token identifeier doesn't exist.</returns>
        public async Task<GlobalStateKey> GetApprovedSpender(GlobalStateKey ownerKey, BigInteger tokenId)
        {
            if (ContractHash is null)
                throw new ContractException(
                    "Initialize the contract client with a ContractHash to query the contract keys.",
                    (long) CEP47ClientErrors.ContractNotFound);
            
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

        /// <summary>
        /// Updates the metadata of a token.
        /// </summary>
        /// <param name="senderPk">Caller and owner of the token being updated.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="meta">New metadata of the token.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        /// <summary>
        /// Burns one token.
        /// </summary>
        /// <param name="senderPk">Caller account (owner or spender) of the token being burned.</param>
        /// <param name="ownerKey">Owner account of the token.</param>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        public DeployHelper BurnOne(PublicKey senderPk,
            GlobalStateKey ownerKey,
            CLValue tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            return BurnMany(senderPk,
                ownerKey,
                new List<CLValue>() {tokenId},
                paymentMotes,
                ttl);
        }
        
        /// <summary>
        /// Burns a list of tokens
        /// </summary>
        /// <param name="senderPk">Caller account (owner or spender) of the tokens being burned.</param>
        /// <param name="ownerKey">Owner account of the tokens.</param>
        /// <param name="tokenIds">List of token identifiers.</param>
        /// <param name="paymentMotes">Payment added to the deploy.</param>
        /// <param name="ttl">Time to live of the deploy.</param>
        /// <returns>A DeployHelper object that must be signed with the caller private key before sending it to the network.</returns>
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

        public DeployHelper BurnMany(PublicKey senderPk,
            GlobalStateKey ownerKey,
            List<CLValue> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000)
        {
            // create a list of U256 for the token ids
            //
            var clTokenIds = CLValue.List(tokenIds.ToArray());

            return BuildDeployHelper("burn",
                new List<NamedArg>()
                {
                    new NamedArg("owner", CLValue.Key(ownerKey)),
                    new NamedArg("token_ids", clTokenIds)
                },
                senderPk,
                paymentMotes);
        }
    }

    /// <summary>
    /// Enumeration with common CEP47 related errors that can be returned by the CEP47 client.
    /// </summary>
    public enum CEP47ClientErrors
    {
        OtherError = 0,
        PermissionDenied = 1,
        WrongArguments = 2,
        TokenIdAlreadyExists = 3,
        TokenIdDoesntExist = 4,
        UnknownAccount = 102,
        ContractNotFound = 103
    }
}
