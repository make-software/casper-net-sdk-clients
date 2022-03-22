using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public class DeployHelper
    {
        private NetCasperClient _casperClient;

        public Deploy Deploy { get; private set; }

        public ExecutionResult ExecutionResult { get; private set; }

        public bool IsSuccess => ExecutionResult?.IsSuccess ?? false;

        public HashKey ContractHash => ExecutionResult?.Effect.Transforms
            .FirstOrDefault(t => t.Type == TransformType.WriteContract)
            ?.Key as HashKey ?? null;

        public HashKey ContractPackageHash => ExecutionResult?.Effect.Transforms
            .FirstOrDefault(t => t.Type == TransformType.WriteContractPackage)
            ?.Key as HashKey ?? null;

        public DeployHelper(Deploy deploy, NetCasperClient casperClient)
        {
            Deploy = deploy;
            _casperClient = casperClient;
        }

        public void Sign(KeyPair keyPair)
        {
            Deploy.Sign(keyPair);
        }

        public async Task PutDeploy()
        {
            await _casperClient.PutDeploy(Deploy);
        }

        public async Task WaitDeployProcess()
        {
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var deployResponse = await _casperClient.GetDeploy(Deploy.Hash, tokenSource.Token);

            var result = deployResponse.Parse();
            ExecutionResult = result.ExecutionResults.First();
        }
    }
}