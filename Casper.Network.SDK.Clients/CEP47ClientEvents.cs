using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public enum CEP47EventType
    {
        Unknown,
        MintOne,
        BurnOne,
        Approve,
        Transfer,
        UpdateMetadata
    }

    public class CEP47Event
    {
        public CEP47EventType EventType { get; init; }
        public string ContractPackageHash { get; init; }
        public string DeployHash { get; init; }
        public string TokenId { get; init; }
        public string Owner { get; init; }
        public string Spender { get; init; }
        public string Sender { get; init; }
        public string Recipient { get; init; }
    }

    public partial class CEP47Client
    {
        private GlobalStateKey _contractPackageHash;

        public delegate void EventHandler(CEP47Event evt);

        public event EventHandler OnCEP47Event;

        private ISSEClient _sseClient;

        public async Task ListenToEvents(ISSEClient sseClient)
        {
            _sseClient = sseClient;

            var rpcResponse = await CasperClient.QueryGlobalState(ContractHash);
            var result = rpcResponse.Parse();

            _contractPackageHash = GlobalStateKey.FromString(
                result.StoredValue.Contract.ContractPackageHash);

            _sseClient.AddEventCallback(EventType.DeployProcessed, "catch-all-cb",
                this.ProcessEvent);
            _sseClient.StartListening();
        }

        private void TriggerEvent(IDictionary<string, string> map, DeployProcessed deploy)
        {
            CEP47Event evt = new CEP47Event
            {
                EventType = map.ContainsKey("event_type")
                    ? map["event_type"] switch
                    {
                        "cep47_mint_one" => CEP47EventType.MintOne,
                        "cep47_burn_one" => CEP47EventType.BurnOne,
                        "cep47_approve_token" => CEP47EventType.Approve,
                        "cep47_transfer_token" => CEP47EventType.Transfer,
                        "cep47_metadata_update" => CEP47EventType.UpdateMetadata,
                        _ => CEP47EventType.Unknown
                    }
                    : CEP47EventType.Unknown,
                TokenId = map.ContainsKey("token_id") ? map["token_id"] : null,
                Owner = map.ContainsKey("owner") ? map["owner"] : null,
                Spender = map.ContainsKey("spender") ? map["spender"] : null,
                Sender = map.ContainsKey("sender") ? map["sender"] : null,
                Recipient = map.ContainsKey("recipient") ? map["recipient"] : null,
                ContractPackageHash = _contractPackageHash.ToHexString(),
                DeployHash = deploy.DeployHash
            };

            OnCEP47Event?.Invoke(evt);
        }

        private void ProcessEvent(SSEvent evt)
        {
            if (evt.EventType != EventType.DeployProcessed)
                return;

            try
            {
                var deploy = evt.Parse<DeployProcessed>();
                if (!deploy.ExecutionResult.IsSuccess)
                    return;

                var maybeEvents = deploy.ExecutionResult.Effect.Transforms.Where(
                    tr => tr.Type == TransformType.WriteCLValue && tr.Key is URef);

                foreach (var maybeEvt in maybeEvents)
                {
                    var clValue = maybeEvt.Value as CLValue;
                    if (clValue?.TypeInfo is CLMapTypeInfo clMapTypeInfo &&
                        clMapTypeInfo.KeyType.Type is CLType.String &&
                        clMapTypeInfo.ValueType.Type is CLType.String)
                    {
                        var map = clValue.ToDictionary<string, string>();
                        if (map.ContainsKey("contract_package_hash") &&
                            map["contract_package_hash"] == _contractPackageHash.ToHexString().ToLower())
                        {
                            TriggerEvent(map, deploy);
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}
