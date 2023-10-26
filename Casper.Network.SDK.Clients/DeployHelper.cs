using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc.ResultTypes;
using Casper.Network.SDK.Types;

namespace Casper.Network.SDK.Clients
{
    public delegate void ProcessDeployResult(GetDeployResult deployResult);
    
    /// <summary>
    /// Helper class to wrap a deploy and perform the common operations for signing, sending to the network, waiting
    /// for execution and parsing results.
    /// It's usually returned by contract client classes when there's a contract call operation.
    /// </summary>
    public class DeployHelper
    {
        private readonly ICasperClient _casperClient;

        /// <summary>
        /// The wrapped Deploy object.
        /// </summary>
        public Deploy Deploy { get; private set; }

        /// <summary>
        /// The execution results. Only available after successful execution of WaitDeployProcess.
        /// </summary>
        public ExecutionResult ExecutionResult { get; private set; }

        /// <summary>
        /// True if the execution of the deploy was successful. Only available after successful execution of WaitDeployProcess.
        /// </summary>
        public bool IsSuccess => ExecutionResult?.IsSuccess ?? false;

        /// <summary>
        /// For a contract installation deploy, contains the contract hash of the new contract.
        /// Only available after successful execution of WaitDeployProcess.
        /// </summary>
        public HashKey ContractHash => ExecutionResult?.Effect.Transforms
            .FirstOrDefault(t => t.Type == TransformType.WriteContract)
            ?.Key as HashKey ?? null;

        /// <summary>
        /// For a contract installation deploy, contains the contract package hash of the new contract.
        /// Only available after successful execution of WaitDeployProcess.
        /// </summary>
        public HashKey ContractPackageHash => ExecutionResult?.Effect.Transforms
            .FirstOrDefault(t => t.Type == TransformType.WriteContractPackage)
            ?.Key as HashKey ?? null;
        
        private ProcessDeployResult _processDeployResultCallback;
        
        public  DeployHelper(Deploy deploy, ICasperClient casperClient)
        {
            Deploy = deploy;
            _casperClient = casperClient;
        }
        
        public DeployHelper(Deploy deploy, ICasperClient casperClient, 
            ProcessDeployResult processDeployResult)
        {
            Deploy = deploy;
            _casperClient = casperClient;
            _processDeployResultCallback = processDeployResult;
        }

        /// <summary>
        /// Uses the private key of a KeyPair object to sign the deploy and add an approval to it. 
        /// </summary>
        /// <param name="keyPair">A KeyPair object with a private key.</param>
        public void Sign(KeyPair keyPair)
        {
            Deploy.Sign(keyPair);
        }

        /// <summary>
        /// Sends the deploy to the network. Returns without waiting for the execution.
        /// </summary>
        public async Task PutDeploy()
        {
            await _casperClient.PutDeploy(Deploy);
        }
        
        /// <summary>
        /// Waits for the execution of a deploy in the network.
        /// </summary>
        public async Task WaitDeployProcess()
        {
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var deployResponse = await _casperClient.GetDeploy(Deploy.Hash, tokenSource.Token);

            var result = deployResponse.Parse();
            ExecutionResult = result.ExecutionResults.First();
            
            _processDeployResultCallback?.Invoke(result);
        }
    }
}
