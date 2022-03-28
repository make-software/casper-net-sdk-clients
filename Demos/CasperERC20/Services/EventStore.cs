using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Types;
using Casper.Network.SDK.Web;
using Org.BouncyCastle.Crypto.Paddings;

namespace CasperERC20.Services;

public class EventStore
{
    private readonly ICasperSSEService _sseService;
    private readonly ILogger<EventStore> _logger;
    
    private readonly List<Block> _blocks = new List<Block>();
    private readonly List<DeployProcessed> _deploys = new List<DeployProcessed>();
    
    private readonly string cbBlocksName = Guid.NewGuid().ToString();
    private readonly string cbDeploysName = Guid.NewGuid().ToString();
    
    public delegate void NewBlockHandler(Block newBlock);
    public event NewBlockHandler BlockAdded;

    public delegate void NewDeploy(DeployProcessed newDeploy);
    public event NewDeploy OnNewDeploy;
    
    public EventStore(ICasperSSEService sseService, ILogger<EventStore> logger)
    {
        _sseService = sseService;
        _logger = logger;
        
        try
        {
            _sseService.AddEventCallback(EventType.BlockAdded, cbBlocksName,  NewEventCallback);
            _sseService.AddEventCallback(EventType.DeployProcessed, cbDeploysName,  NewEventCallback);

            _sseService.StartListening();
        }
        catch (Exception e)
        {
            // ignored
        }
    }
    
    protected virtual void OnBlockAdded(Block block)    // the Trigger method, called to raise the event
    {
        // make a copy to be more thread-safe
        NewBlockHandler handler = BlockAdded;   

        if (handler != null)
        {
            // invoke the subscribed event-handler(s)
            handler(block);  
        }
    }
    private async void NewEventCallback(SSEvent evt)
    {
        _logger.LogTrace($"New event {evt.Id} - {evt.EventType}");
        if (evt.EventType == EventType.BlockAdded)
        {
            var blockAdded = evt.Parse<BlockAdded>();
            AddBlock(blockAdded.Block);
            OnBlockAdded(blockAdded.Block);
        }
        else if (evt.EventType == EventType.DeployProcessed)
        {
            var deployProcessed = evt.Parse<DeployProcessed>();
            AddDeploy(deployProcessed);
            //OnNewDeploy.Invoke(deployProcessed);
        }
    }

    public IEnumerable<Block> Blocks => _blocks;

    public IEnumerable<DeployProcessed> Deploys => _deploys;

    public void AddBlock(Block b) => _blocks.Add(b);

    public void AddDeploy(DeployProcessed d) => _deploys.Add(d);
}