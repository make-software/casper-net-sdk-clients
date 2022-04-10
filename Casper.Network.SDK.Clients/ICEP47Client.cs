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

        BigInteger TotalSupply { get; }

        Task<bool> SetContractHash(PublicKey publicKey, string namedKey);

        Task<bool> SetContractHash(string contractHash);

        Task<bool> SetContractHash(GlobalStateKey contractHash);

        DeployHelper InstallContract(byte[] wasmBytes,
            string contractName,
            string name,
            string symbol,
            Dictionary<string, string> meta,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper MintOne(PublicKey senderPk,
            PublicKey recipientPk,
            BigInteger tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper MintMany(PublicKey senderPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            List<Dictionary<string, string>> metas,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper MintCopies(PublicKey ownerPK,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper TransferToken(PublicKey ownerPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper Approve(PublicKey ownerPk,
            PublicKey spenderPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper TransferTokenFrom(PublicKey senderPk,
            PublicKey ownerPk,
            PublicKey recipientPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        Task<BigInteger?> GetTokenIdByIndex(PublicKey owner, uint index);

        Task<Dictionary<string, string>> GetTokenMetadata(BigInteger tokenId);

        Task<GlobalStateKey> GetOwnerOf(BigInteger tokenId);

        Task<BigInteger?> GetBalanceOf(PublicKey owner);

        Task<GlobalStateKey> GetApprovedSpender(PublicKey owner, BigInteger tokenId);

        DeployHelper UpdateTokenMetadata(PublicKey senderPk,
            BigInteger tokenId,
            Dictionary<string, string> meta,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper BurnOne(PublicKey senderPk,
            PublicKey ownerPk,
            BigInteger tokenId,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper BurnMany(PublicKey senderPk,
            PublicKey ownerPk,
            List<BigInteger> tokenIds,
            BigInteger paymentMotes,
            ulong ttl = 1800000);
    }
}
