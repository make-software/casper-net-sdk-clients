using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.Types;

namespace CallCEP47
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var nodeAddress = "http://127.0.0.1:11101";
            const string CHAIN_NAME = "casper-net-1";
            var TOKEN_ID  = BigInteger.One;
            
            //
            // Set up a new Casper RPC Client
            //
            var casperSdk = new NetCasperClient(nodeAddress);
            
            //
            // Create the CEP47 client and initialize it with a previously installed CEP47 contract
            //
            var cep47Client = new Casper.Network.SDK.Clients.CEP47Client(casperSdk, CHAIN_NAME);

            var user1Key = KeyPair.FromPem("./user-1/secret_key.pem");

            try
            {
                await cep47Client.SetContractHash(user1Key.PublicKey, $"example_nft_contract_hash");

                Console.WriteLine("CEP47 contract name: " + cep47Client.Name);
                Console.WriteLine("CEP47 contract symbol: " + cep47Client.Symbol);
                
                var k = await cep47Client.GetTotalSupply();
                Console.WriteLine("CEP47 total supply. " + k);
            }
            catch (ContractException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            
            //
            // Mint first token
            //
            Console.WriteLine();
            Console.WriteLine("Minting first token...");

            var meta = new Dictionary<string, string>()
            {
                {"number", "one"}
            };
            
            try
            {
                var deployHelper = cep47Client.MintOne(user1Key.PublicKey, new AccountHashKey(user1Key.PublicKey),
                    TOKEN_ID, meta, 2_000_000_000);
            
                deployHelper.Sign(user1Key);
                
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();
                
                Console.WriteLine("Mint cost: " + deployHelper.ExecutionResult.Cost);
                Console.WriteLine();

                var count = await cep47Client.GetBalanceOf(new AccountHashKey(user1Key.PublicKey));
                Console.WriteLine("User1 balance: " + count);

                var tokenOwner = await cep47Client.GetOwnerOf(TOKEN_ID);
                Console.WriteLine("Token owner (account hash): " + tokenOwner);

                var tId = await cep47Client.GetTokenIdByIndex(new AccountHashKey(user1Key.PublicKey), 0);
                Console.WriteLine("First token identifier: " + tId);
            }
            catch (ContractException e)
            {
                Console.WriteLine("Error in mint operation: " + e.Message);
                return;
            }
            
            //
            // transfer the token to another account
            //
            Console.WriteLine();
            Console.WriteLine("Sending token to another account...");
            
            var user2Key =  KeyPair.FromPem("./user-2/secret_key.pem");

            try
            {
                var deployHelper = cep47Client.TransferToken(user1Key.PublicKey, new AccountHashKey(user2Key.PublicKey),
                    new List<BigInteger>() { TOKEN_ID }, 2_000_000_000);

                deployHelper.Sign(user1Key);
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();
                
                Console.WriteLine("Transfer cost: " + deployHelper.ExecutionResult.Cost);

                var count = await cep47Client.GetBalanceOf(new AccountHashKey(user1Key.PublicKey));
                Console.WriteLine("User1 balance: " + count);

                count = await cep47Client.GetBalanceOf(new AccountHashKey(user2Key.PublicKey));
                Console.WriteLine("User2 balance: " + count);
                
                var metadata = await cep47Client.GetTokenMetadata(TOKEN_ID);
                Console.WriteLine("Token metadata");
                foreach (var kvp in metadata)
                    Console.WriteLine(kvp.Key + " - " + kvp.Value);
            }
            catch (ContractException e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            //
            // Burn a token
            //
            Console.WriteLine();
            Console.WriteLine("Burning token...");

            try
            {
                var deployHelper = cep47Client.BurnOne(user2Key.PublicKey, new AccountHashKey(user2Key.PublicKey),
                    TOKEN_ID, 2_000_000_000);

                deployHelper.Sign(user2Key);
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();
                
                Console.WriteLine("Burn cost: " + deployHelper.ExecutionResult.Cost);

                var count = await cep47Client.GetBalanceOf(new AccountHashKey(user2Key.PublicKey));
                Console.WriteLine("User2 balance: " + count);
                
                var metadata = await cep47Client.GetTokenMetadata(TOKEN_ID); 
                Debug.Assert(metadata == null); // network returns None, converted to null
            }
            catch (ContractException e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
