﻿using Casper.Network.SDK.Clients;
using Microsoft.Extensions.Configuration;

namespace Casper.Network.SDK.WebClients
{
    public class ERC20ClientWeb : ERC20Client
    {
        public ERC20ClientWeb(ICasperClient casperRpcService,
            IConfiguration config) 
            : base(casperRpcService, config["Casper.Network.SDK.Web:ChainName"])
        {
        }
    }
}
