using Casper.Network.SDK.Clients;
using Casper.Network.SDK.WebClients;
using Casper.Network.SDK.Types;
using CasperERC20.Components;
using Microsoft.AspNetCore.Components;

namespace CasperERC20.Pages;

public partial class ViewContract
{
    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private IERC20Client ERC20Client { get; set; }

    [Parameter] public string ContractHash { get; set; }
    
    private CasperClientError _detailsError;
    private CasperClientError _getBalanceError;
    private CasperClientError _getAllowanceError;

    private string SubjectPK;
    private string BalanceOf;

    private string OwnerPK;
    private string SpenderPK;
    private string ApprovedBalance;

    // public override Task SetParametersAsync(ParameterView parameters)
    // {
    //     foreach (var parameter in parameters)
    //     {
    //         switch (parameter.Name)
    //         {
    //             case nameof(ContractHash):
    //                 ContractHash = (string) parameter.Value;
    //                 var task = ERC20Client.SetContractHash(ContractHash);
    //                 task.ContinueWith(t =>
    //                 {
    //                     if(t.IsCompleted && t.Exception != null)
    //                         _detailsError.Show("Cannot retrieve contract named keys", t.Exception);
    //                     InvokeAsync(StateHasChanged);
    //                 });
    //                 break;
    //             default:
    //                 throw new ArgumentException($"Unknown parameter: {parameter.Name}");
    //         }
    //     }
    //
    //     return base.SetParametersAsync(ParameterView.Empty);
    // }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var task = ERC20Client.SetContractHash(ContractHash);
            await task.ContinueWith(t =>
            {
                if(t.IsCompleted && t.Exception != null)
                    _detailsError.ShowError("Cannot retrieve contract named keys", t.Exception);
                InvokeAsync(StateHasChanged);
            });
        }
    }
    
    public async Task OnGetBalanceClick()
    {
        _getBalanceError.Hide();
        BalanceOf = null;
        try
        {
            var task = ERC20Client.GetBalance(PublicKey.FromHexString(SubjectPK));

            await task.ContinueWith(t =>
            {
                if (t.IsCompleted && t.Exception == null)
                    BalanceOf = t.Result.ToString();
                else
                    _getBalanceError?.ShowError("Account not found", t.Exception);
            });
        }
        catch (Exception e)
        {
            _getBalanceError?.ShowError("Account not found", e);
        }

        await InvokeAsync(StateHasChanged);
    }

    public async Task OnGetAllowanceClick()
    {
        _getAllowanceError.Hide();
        ApprovedBalance = null;
        try
        {
            var task = ERC20Client.GetAllowance(PublicKey.FromHexString(OwnerPK),
                PublicKey.FromHexString(SpenderPK));

            await task.ContinueWith(t =>
            {
                if (t.IsCompleted && t.Exception == null)
                    ApprovedBalance = t.Result.ToString();
                else
                    _getAllowanceError?.ShowError("Account not found", t.Exception);
            });
        }
        catch (Exception e)
        {
            _getAllowanceError?.ShowError("Account not found", e);
        }

        await InvokeAsync(StateHasChanged);
    }
}