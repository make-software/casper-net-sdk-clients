using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK;
using Casper.Network.SDK.Clients;
using Casper.Network.SDK.SSE;
using Casper.Network.SDK.Types;

namespace CallERC20
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var nodeAddress = "http://127.0.0.1:11101";
            const string CHAIN_NAME = "casper-net-1";
            
            //
            // Set up a new Casper RPC Client
            //
            var casperSdk = new NetCasperClient(nodeAddress);
            
            //
            // Create the ERC20 client and initialize it with a previously installed ERC20 contract
            //
            var erc20Client = new ERC20Client(casperSdk, CHAIN_NAME);

            var user1Key = KeyPair.FromPem("./user-1/secret_key.pem");
            var user2Key =  KeyPair.FromPem("./user-2/secret_key.pem");

            try
            {
                await erc20Client.SetContractHash(user1Key.PublicKey, $"erc20_token_contract");

                Console.WriteLine("ERC20 contract name: " + erc20Client.Name);
                Console.WriteLine("ERC20 contract symbol: " + erc20Client.Symbol);
                Console.WriteLine("ERC20 decimals: " + erc20Client.Decimals);
                Console.WriteLine("ERC20 total supply: " + FormatAmount(erc20Client.TotalSupply, erc20Client.Decimals));
            }
            catch (ContractException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            
            //
            // Transfer tokens to another user
            //
            Console.WriteLine();
            Console.WriteLine("Transfer tokens...");

            try
            {
                var deployHelper = erc20Client.TransferTokens(user1Key.PublicKey,
                    new AccountHashKey(user2Key.PublicKey),
                    10_00000, 1_000_000_000);
                    
                deployHelper.Sign(user1Key);
                
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();
                
                Console.WriteLine("Transfer cost: " + deployHelper.ExecutionResult.Cost);
                Console.WriteLine();

                var count = await erc20Client.GetBalance(new AccountHashKey(user1Key.PublicKey));
                Console.WriteLine("User1 balance: " + FormatAmount(count, erc20Client.Decimals));

                count = await erc20Client.GetBalance(new AccountHashKey(user2Key.PublicKey));
                Console.WriteLine("User2 balance: " + FormatAmount(count, erc20Client.Decimals));
            }
            catch (ContractException e)
            {
                Console.WriteLine("Error in mint operation: " + e.Message);
                return;
            }
            
            //
            // Approve an spender
            //
            Console.WriteLine();
            Console.WriteLine("Approve an spender...");
            
            
            try
            {
                var deployHelper = erc20Client.ApproveSpender(user1Key.PublicKey,
                    new AccountHashKey(user2Key.PublicKey),
                    100_00000, 2_000_000_000);
            
                deployHelper.Sign(user1Key);
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();
                
                Console.WriteLine("Approval cost: " + deployHelper.ExecutionResult.Cost);
            
                var count = await erc20Client.GetAllowance(new AccountHashKey(user1Key.PublicKey),
                    new AccountHashKey(user2Key.PublicKey));
                
                Console.WriteLine("User2 approval from User1: " + FormatAmount(count, erc20Client.Decimals));
            }
            catch (ContractException e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            //
            // Transfer tokens on behalf of user1 to a contract address
            //
            Console.WriteLine();
            Console.WriteLine("Transfer tokens on behalf of user1 to a contract address...");
            
            try
            {
                var rpcResponse = await casperSdk.QueryGlobalState(erc20Client.ContractHash);
                var result = rpcResponse.Parse();

                var contractPackageHash = GlobalStateKey.FromString(
                    result.StoredValue.Contract.ContractPackageHash) as HashKey;
                
                var deployHelper = erc20Client.TransferTokensFromOwner(user2Key.PublicKey,
                    new AccountHashKey(user1Key.PublicKey),
                    contractPackageHash, 50_00000, 2_000_000_000);
            
                deployHelper.Sign(user2Key);
                await deployHelper.PutDeploy();
                await deployHelper.WaitDeployProcess();
                
                Console.WriteLine("Transfer-from cost: " + deployHelper.ExecutionResult.Cost);
            
                var count = await erc20Client.GetBalance(erc20Client.ContractHash);
                Console.WriteLine("Contract balance: " + FormatAmount(count, erc20Client.Decimals));
            }
            catch (ContractException e)
            {
                Console.WriteLine(e);
            }
        }
        
        static string FormatAmount(BigInteger amount, int decimals)
        {
            var s = amount.ToString();
            var l = s.Length;
            return s[..(l - decimals)] + "." + s[(l - decimals)..];
        }
    }
}
