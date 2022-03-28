using System.Numerics;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.WebClients;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Web;
using Microsoft.AspNetCore.Components;

namespace CasperERC20.Components;

public partial class ERC20TransferFrom
{
    [Parameter] public IERC20Client ERC20Client { get; set; }
    
    [Inject] protected CasperSignerInterop SignerInterop { get; set; }

    private CasperClientError _deployAlert;

    private string OwnerPublicKey;
    private string SpenderPublicKey;
    private string TargetPublicKey;
    private string Amount;
    private string WaitDeployMsg;

    private async Task OnTransferClick()
    {
        var ownerPK = PublicKey.FromHexString(OwnerPublicKey);
        var spenderPK = PublicKey.FromHexString(SpenderPublicKey);
        var targetPK = PublicKey.FromHexString(TargetPublicKey);
        var amount = BigInteger.Parse(Amount);
        var payment = new BigInteger(900000000);

        var deployHelper = ERC20Client.TransferTokensFromOwner(spenderPK, ownerPK,
            targetPK, amount, payment);

        var signed = await SignerInterop.RequestSignature(deployHelper.Deploy, SpenderPublicKey, null);
        if (signed)
        {
            await deployHelper.PutDeploy();
            
            _deployAlert.ShowWarning("Waiting for deploy execution results (" + deployHelper.Deploy.Hash + ")");
            var task = deployHelper.WaitDeployProcess();
            await task.ContinueWith(t =>
            {
                if (t.IsCompleted && t.IsFaulted)
                {
                    _deployAlert.ShowError("Error in deploy.", t.Exception);
                }
                else
                {
                    if(deployHelper.IsSuccess)
                        _deployAlert.ShowSuccess("Deploy executed");
                    else
                        _deployAlert.ShowError("Deploy executed with error. " + deployHelper.ExecutionResult.ErrorMessage);
                }
            });

            await InvokeAsync(StateHasChanged);
        }
    }
}