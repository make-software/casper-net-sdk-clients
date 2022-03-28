using Blazored.LocalStorage;
using Casper.Network.SDK;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Web;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorDelegate.Pages;

public class ExplorerComponent : ComponentBase
{
    [Inject] protected ICasperClient CasperRpcService { get; set; }

    [Inject] protected CasperSignerInterop SignerInterop { get; set; }

    [Inject] protected NotificationService NotificationService { get; set; }
    
    [Inject] protected ILocalStorageService LocalStorage { get; set; }

    protected string SuccessMessage;
    protected string ErrorMessage;

    protected async Task<Deploy> SignDeployWithSigner(Deploy deploy, string srcPk, string? tgtPk)
    {
        var json = deploy.SerializeToJson();

        var signerResult = await SignerInterop.Sign(json, srcPk, tgtPk);
        var approval = new DeployApproval()
        {
            Signer = PublicKey.FromHexString(signerResult.EnumerateArray().First().GetProperty("signer").ToString()),
            Signature = Signature.FromHexString(
                signerResult.EnumerateArray().First().GetProperty("signature").ToString())
        };
        deploy.Approvals.Add(approval);

        return deploy;
    }
}