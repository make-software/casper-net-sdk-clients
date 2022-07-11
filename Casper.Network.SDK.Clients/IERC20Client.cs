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

        Task<bool> SetContractHash(PublicKey publicKey, string namedKey, bool SkipNamedkeysQuery=false);

        Task<bool> SetContractHash(string contractHash, bool SkipNamedkeysQuery=false);

        Task<bool> SetContractHash(GlobalStateKey contractHash, bool SkipNamedkeysQuery=false);

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
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper ApproveSpender(PublicKey ownerPK,
            GlobalStateKey spenderKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        DeployHelper TransferTokensFromOwner(PublicKey spenderPK,
            GlobalStateKey ownerKey,
            GlobalStateKey recipientKey,
            BigInteger amount,
            BigInteger paymentMotes,
            ulong ttl = 1800000);

        Task<BigInteger> GetBalance(GlobalStateKey ownerKey);

        Task<BigInteger> GetAllowance(GlobalStateKey ownerKey, GlobalStateKey spenderKey);
    }
}
