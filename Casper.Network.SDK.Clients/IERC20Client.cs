using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public interface IERC20Client
    {
        string Name { get; }

        string Symbol { get; }

        byte Decimals { get; }

        BigInteger TotalSupply { get; }

        Task<bool> SetContractHash(PublicKey publicKey, string namedKey);

        Task<bool> SetContractHash(string contractHash);

        Task<bool> SetContractHash(GlobalStateKey contractHash);

        DeployHelper InstallContract(byte[] wasmBytes,
            string name,
            string symbol,
            byte decimals,
            BigInteger totalSupply,
            PublicKey accountPK,
            BigInteger paymentMotes,
            ulong ttl = 1800000
        );

        DeployHelper TransferTokens(PublicKey ownerPK,
            PublicKey recipientPk,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper ApproveSpender(PublicKey ownerPK,
            PublicKey spenderPk,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper TransferTokensFromOwner(PublicKey spenderPK,
            PublicKey ownerPk,
            PublicKey recipientPk,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        Task<BigInteger> GetBalance(PublicKey ownerPK);

        Task<BigInteger> GetAllowance(PublicKey ownerPK, PublicKey spenderPK);
    }
}
