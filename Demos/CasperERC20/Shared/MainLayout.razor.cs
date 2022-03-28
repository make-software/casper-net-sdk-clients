using CasperERC20.Services;
using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Web;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace CasperERC20.Shared;

enum SignerStatus
{
    Unknown,
    NotPresent,
    Disconnected,
    Connected,
    Unlocked
}

public partial class MainLayout
{
    [Inject] protected CasperSignerInterop SignerInterop { get; set; }

    [Inject] protected ILogger<MainLayout> Logger { get; set; }

    [Inject] protected EventStore EventStore { get; set; }
    
    private SignerStatus SignerStatus = SignerStatus.Unknown;
    private string ActivePk = string.Empty;

    [Inject] protected ICasperSSEService SSEService { get; set; }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            SignerInterop.OnStateUpdate += (connected, unlocked, key) =>
            {
                ActivePk = key;
                if (!connected)
                    SignerStatus = SignerStatus.Disconnected;
                else if (!unlocked)
                    SignerStatus = SignerStatus.Connected;
                else
                    SignerStatus = SignerStatus.Unlocked;
                StateHasChanged();
            };
            var signerPresent = await SignerInterop.IsSignerPresent();

            if (!signerPresent)
            {
                Logger.LogDebug("Signer extension not present.");
                SignerStatus = SignerStatus.NotPresent;
                StateHasChanged();
                return;
            }

            await SignerInterop.AddEventListeners();

            var isConnected = await SignerInterop.IsConnected();
            if (!isConnected)
            {
                Logger.LogDebug("Signer extension not connected to this site.");
                SignerStatus = SignerStatus.Disconnected;
                StateHasChanged();
                return;
            }

            var state = await SignerInterop.GetState();
            Console.WriteLine(state);

            SignerStatus = state.IsUnlocked ? SignerStatus.Unlocked : SignerStatus.Connected;
            if (state.IsUnlocked)
            {
                ActivePk = state.ActivePK;
                Logger.LogDebug("Signer extension unlock. Active key: " + GetActivePKLabel());
            }
            else
            {
                Logger.LogDebug("Signer extension locked.");
            }
            
            StateHasChanged();
        }
    }

    // void IDisposable.Dispose()
    // {
    //     SSEService.RemoveEventCallback(EventType.BlockAdded, cbDeploysName);
    //     SSEService.RemoveEventCallback(EventType.DeployProcessed, cbDeploysName);
    // }
    
    protected async Task OnConnectClick()
    {
        if (SignerStatus >= SignerStatus.Disconnected)
            await SignerInterop.RequestConnection();
        // StateHasChanged();
    }

    protected string GetActivePKLabel()
    {
        return $"{ActivePk[..5]}..{ActivePk[28..32]}";
    }
}