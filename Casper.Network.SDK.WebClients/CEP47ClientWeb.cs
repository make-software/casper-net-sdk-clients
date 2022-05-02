using Casper.Network.SDK.Clients;
using Microsoft.Extensions.Configuration;

namespace Casper.Network.SDK.WebClients
{
    public class CEP47ClientWeb : CEP47Client
    {
        public CEP47ClientWeb(ICasperClient casperRpcService,
            IConfiguration config) 
            : base(casperRpcService, config["Casper.Network.SDK.Web:ChainName"])
        {
        }
    }
}
