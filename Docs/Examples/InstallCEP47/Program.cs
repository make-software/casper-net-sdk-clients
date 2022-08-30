using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.JsonRpc;
using Casper.Network.SDK.Types;

namespace InstallCEP47
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var nodeAddress = "http://127.0.0.1:11101";
            const string CHAIN_NAME = "casper-net-1";

            //
            // Set up a new Casper RPC Client with gzip compression and console output logging
            //
            var clientHandler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            };
            var loggingHandler = new RpcLoggingHandler(clientHandler)
            {
                LoggerStream = new StreamWriter(Console.OpenStandardOutput())
            };
            var httpClient = new HttpClient(loggingHandler);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip");

            var casperSdk = new NetCasperClient(nodeAddress, httpClient);
            
            //
            // Create the CEP47 client
            //
            var cep47Client = new Casper.Network.SDK.Clients.CEP47Client(casperSdk, CHAIN_NAME);

            var wasmBytes = await File.ReadAllBytesAsync("./cep47-token.wasm");
            
            var ownerKey = KeyPair.FromPem("./user-1/secret_key.pem");

            var meta = new Dictionary<string, string>()
            {
                {"Source", "C# SDK Clients"},
                {"Description", "Example of CEP47 contract installation."}
            };

            var deployHelper = cep47Client.InstallContract(wasmBytes, "example_nft", "ExampleNFT", "EXNFT", meta,
                ownerKey.PublicKey, 200_000_000_000);
            
            deployHelper.Sign(ownerKey);

            try
            {
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();

                Console.WriteLine("Contract installed!");
                Console.WriteLine("Contract package hash: " + deployHelper.ContractPackageHash);
                Console.WriteLine("Contract hash: " + deployHelper.ContractHash);    
            }
            catch (ContractException e)
            {
                Console.WriteLine($"Error in contract deploy");
                Console.WriteLine($"  Code: " + e.Code);
                Console.WriteLine($"  Message: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
