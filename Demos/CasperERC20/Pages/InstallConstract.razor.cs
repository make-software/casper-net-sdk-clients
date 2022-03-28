using System.Numerics;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.WebClients;
using Casper.Network.SDK.Types;
using CasperERC20.Components;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace CasperERC20.Pages;

public partial class InstallConstract
{
    // erc20 contract details
    //
    private string _name;
    private string _symbol;
    private string _decimals;
    private string _totalSupply;

    private bool IsTokenDataComplete() => _name != null
                                          && _symbol != null
                                          && _decimals != null
                                          && _totalSupply != null;

    // account signer will be the owner of the contract
    //
    private string _ownerPK = null;
    
    // deploy hash for the contract installation
    //
    private string _deployHash = null;

    private string _contractHash = null;
    
    private CasperClientError _getDeployError;

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private IERC20Client ERC20ClientWeb { get; set; }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var state = await SignerInterop.GetState();
        if (state.IsUnlocked)
            _ownerPK = state.ActivePK;
        
        if (firstRender)
        {
            SignerInterop.OnStateUpdate += (connected, unlocked, key) =>
            {
                _ownerPK = unlocked ? key : null;
                StateHasChanged();
            };
        }
    }

    private RadzenSteps Steps;

    async Task OnGoToSignDeployClick()
    {
        Steps.SelectedIndex = 1;
        StateHasChanged();
    }
    
    async Task OnDeployClick()
    {
        try
        {
            var bytes = File.ReadAllBytes("erc20_token.wasm");
        
            var state = await SignerInterop.GetState();

            var deploy = ERC20ClientWeb?.InstallContract(bytes, _name, _symbol, 
                byte.Parse(_decimals), BigInteger.Parse(_totalSupply),
                PublicKey.FromHexString(state.ActivePK), 250_000_000_000);
        
            var signed = await SignerInterop.RequestSignature(deploy.Deploy, state.ActivePK, null);
            if (signed)
            {
                _deployHash = deploy.Deploy.Hash;

                Steps.SelectedIndex = 2;
                StateHasChanged();
                
                await deploy.PutDeploy();

                var task = deploy.WaitDeployProcess();
                await task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _getDeployError.ShowError("Error in the deploy", t.Exception);
                    else if (t.IsCanceled)
                        _getDeployError.ShowError("Timeout.");
                    else
                    {
                        _contractHash = deploy.ContractHash.ToString();
                        LocalStorage.SetItemAsStringAsync($"contract-{_symbol}", _contractHash);
                    }
                });
            }
            else
                throw new Exception("Deploy not signed.");
        }
        catch (Exception e)
        {
            _getDeployError?.ShowError("Error", e);
        }
        
        await InvokeAsync(StateHasChanged);
    }
}