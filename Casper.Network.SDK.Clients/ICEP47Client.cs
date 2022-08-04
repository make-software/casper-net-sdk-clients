using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public interface ICEP47Client
    {
        string Name { get; }

        string Symbol { get; }

        Dictionary<string, string> Meta { get; }

        Task<bool> SetContractHash(PublicKey publicKey, string namedKey, bool skipNamedkeysQuery=false);

        Task<bool> SetContractHash(string contractHash, bool skipNamedkeysQuery=false);

        Task<bool> SetContractHash(GlobalStateKey contractHash, bool skipNamedkeysQuery=false);

        DeployHelper InstallContract(byte[] wasmBytes,
            string contractName,
            string name,
            string symbol,
            Dictionary<string, string> meta,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        Task<BigInteger> GetTotalSupply();
        
        DeployHelper MintOne(PublicKey senderPk,
            GlobalStateKey recipientKey,
            BigInteger? tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper MintMany(PublicKey senderPk,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            List<Dictionary<string, string>> metas,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper MintCopies(PublicKey ownerPK,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper TransferToken(PublicKey ownerPk,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper Approve(PublicKey ownerPk,
            GlobalStateKey spenderKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper TransferTokenFrom(PublicKey senderPk,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        Task<BigInteger?> GetTokenIdByIndex(GlobalStateKey ownerKey, uint index);

        Task<Dictionary<string, string>> GetTokenMetadata(BigInteger tokenId);

        Task<GlobalStateKey> GetOwnerOf(BigInteger tokenId);

        Task<BigInteger?> GetBalanceOf(GlobalStateKey ownerKey);

        Task<GlobalStateKey> GetApprovedSpender(GlobalStateKey ownerKey, BigInteger tokenId);

        DeployHelper UpdateTokenMetadata(PublicKey senderPk,
            BigInteger tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper BurnOne(PublicKey senderPk,
            GlobalStateKey ownerKey,
            BigInteger tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper BurnMany(PublicKey senderPk,
            GlobalStateKey ownerKey,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
    }
}
