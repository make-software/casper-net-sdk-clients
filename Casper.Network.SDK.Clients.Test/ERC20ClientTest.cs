using Casper.Network.SDK.Types;
using NUnit.Framework;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Casper.Network.SDK.JsonRpc;

namespace Casper.Network.SDK.Clients.Test
{
    public class ERC20ClientTest
    {
        private const string CHAIN_NAME = "casper-net-1";
        private string _nodeAddress;
        private string _erc20WasmFile;
        
        private KeyPair _ownerAccount;
        private KeyPair _user1Account;
        private KeyPair _user2Account;

        private GlobalStateKey _ownerAccountKey;
        private GlobalStateKey _user1AccountKey;
        private GlobalStateKey _user2AccountKey;

        private HashKey _contractHash;
        private HashKey _contractPackageHash;
        private ERC20Client _erc20Client;

        private const string TOKEN_NAME = ".NET SDK";
        private const string TOKEN_SYMBOL = "NETSDK";
        private const byte TOKEN_DECIMALS = 5;
        private const string TOKEN_SUPPLY = "1000000";

            
        [OneTimeSetUp]
        public void Init()
        {
            _nodeAddress = Environment.GetEnvironmentVariable("CASPERNETSDK_NODE_ADDRESS");
            Assert.IsNotNull(_nodeAddress,
                "Please, set environment variable CASPERNETSDK_NODE_ADDRESS with a valid node url (with port).");

            _erc20WasmFile = Environment.GetEnvironmentVariable("ERC20TOKEN_WASM");
            Assert.IsNotNull(_nodeAddress,
                "Please, set environment variable ERC20TOKEN_WASM with a valid wasm file name located under TestData folder.");

            var fkFilename = TestContext.CurrentContext.TestDirectory +
                             "/TestData/owneract.pem";
            _ownerAccount = KeyPair.FromPem(fkFilename);
            Assert.IsNotNull(_ownerAccount, $"Cannot read owner key from '{fkFilename}");
            _ownerAccountKey = new AccountHashKey(_ownerAccount.PublicKey);
            
            var uk1Filename = TestContext.CurrentContext.TestDirectory +
                             "/TestData/user1act.pem";
            _user1Account = KeyPair.FromPem(uk1Filename);
            Assert.IsNotNull(_user1Account, $"Cannot read owner key from '{uk1Filename}");
            _user1AccountKey = new AccountHashKey(_user1Account.PublicKey);

            var uk2Filename = TestContext.CurrentContext.TestDirectory +
                              "/TestData/user2act.pem";
            _user2Account = KeyPair.FromPem(uk2Filename);
            Assert.IsNotNull(_user2Account, $"Cannot read owner key from '{uk2Filename}");
            _user2AccountKey = new AccountHashKey(_user2Account.PublicKey);
        }

        [Test, Order(1)]
        public async Task InstallContractTest()
        {
            var client = new ERC20Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            var wasmBytes = File.ReadAllBytes(TestContext.CurrentContext.TestDirectory +
                                              "/TestData/" + _erc20WasmFile);

            var deployHelper = client.InstallContract(wasmBytes, 
                TOKEN_NAME, TOKEN_SYMBOL, TOKEN_DECIMALS, BigInteger.Parse(TOKEN_SUPPLY),
                _ownerAccount.PublicKey, 250_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_ownerAccount);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();
            
            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            Assert.IsNotNull(deployHelper.ContractHash);
            Assert.AreEqual(69, deployHelper.ContractHash.ToString().Length);
            Assert.IsNotNull(deployHelper.ContractPackageHash);
            Assert.AreEqual(69, deployHelper.ContractPackageHash.ToString().Length);

            _contractHash = deployHelper.ContractHash;
            _contractPackageHash = deployHelper.ContractPackageHash;
        }

        [Test, Order(2)]
        public async Task SetContractHashFromPKTest()
        {
            _erc20Client = new ERC20Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            await _erc20Client.SetContractHash(_ownerAccount.PublicKey, "erc20_token_contract");

            if (_contractHash != null)
                Assert.AreEqual(_contractHash.ToString(), _erc20Client.ContractHash.ToString());
            else
                _contractHash = _erc20Client.ContractHash;
        }
        
        [Test, Order(2)]
        public void CatchSetContractHashWrongKeyTest()
        {
            // catch error for wrong named key
            //
            var ex = Assert.CatchAsync<Exception>(async () =>
                await _erc20Client.SetContractHash(_ownerAccount.PublicKey, "wrong_named_key"));
            Assert.IsNotNull(ex);
            Assert.AreEqual("Named key 'wrong_named_key' not found.", ex.Message);
        }

        [Test, Order(3)]
        public async Task SetContractHashTest()
        {
            Assert.IsNotNull(_contractHash, "This test must run after InstallContractTest");
            var client = new ERC20Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            client.SetContractHash(_contractHash.ToString());
            
            Assert.AreEqual(TOKEN_NAME, await client.GetName());
            Assert.AreEqual(TOKEN_SYMBOL, await client.GetSymbol());
            Assert.AreEqual(TOKEN_DECIMALS, await client.GetDecimals());
            Assert.AreEqual(BigInteger.Parse(TOKEN_SUPPLY), await client.GetTotalSupply());
            
            Assert.IsNotNull(client.ContractHash);
            Assert.AreEqual(_contractHash.ToString(), client.ContractHash.ToString());
        }

        [Test, Order(3)]
        public async Task TransferTokensTest()
        {
            Assert.IsNotNull(_contractPackageHash, "This test must run after InstallContractTest");
            var client = new ERC20Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);
            
            client.SetContractPackageHash(_contractPackageHash, null);
            
            var deployHelper = client.TransferTokens(_ownerAccount.PublicKey,
                _user2AccountKey,
                10_000,
                1_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_ownerAccount);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();
            
            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
        }

        [Test, Order(3)]
        public async Task ApproveSpenderTest()
        {
            Assert.IsNotNull(_erc20Client, "This test must run after SetContractHashTest");

            var deployHelper = _erc20Client.ApproveSpender(_ownerAccount.PublicKey,
                _user1AccountKey,
                500_000, 
                1_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_ownerAccount);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();
            
            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
        }

        [Test, Order(4)]
        public async Task GetBalanceTest()
        {
            var balanceOfUser2 = await _erc20Client.GetBalance(_user2AccountKey);
            Assert.AreEqual((BigInteger)10000, balanceOfUser2);
            
            var balanceOfOwner = await _erc20Client.GetBalance(_ownerAccountKey);
            Assert.AreEqual(BigInteger.Parse(TOKEN_SUPPLY), balanceOfOwner+balanceOfUser2);
        }

        [Test, Order(5)]
        public async Task TransferTokensFromTest()
        {
            var deployHelper = _erc20Client.TransferTokensFromOwner(_user1Account.PublicKey,
               _ownerAccountKey,
                _contractHash,
                300_000, 
                1_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();
            
            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
        }

        [Test, Order(6)]
        public async Task AllowanceTest()
        {
            var allowanceOfUser1 = await _erc20Client.GetAllowance(_ownerAccountKey,
                _user1AccountKey);
            Assert.AreEqual((BigInteger)200_000, allowanceOfUser1);
            
            var balanceOfUser2 = await _erc20Client.GetBalance(_user2AccountKey);
            Assert.AreEqual((BigInteger)10_000, balanceOfUser2);
            
            var balanceOfContract = await _erc20Client.GetBalance(_contractHash);
            Assert.AreEqual((BigInteger)300_000, balanceOfContract);
            
            var balanceOfOwner = await _erc20Client.GetBalance(_ownerAccountKey);
            Assert.AreEqual(BigInteger.Parse(TOKEN_SUPPLY), balanceOfOwner+balanceOfUser2+balanceOfContract);
        }

        [Test, Order(7)]
        public void CatchAccountBalanceNotFoundTest()
        {
            var ex = Assert.CatchAsync<ContractException>(async () =>
                await _erc20Client.GetBalance(_user1AccountKey));
            Assert.IsNotNull(ex);
            Assert.AreEqual((int)ERC20ClientErrors.UnknownAccount, ex.Code);
        }
        
        [Test, Order(7)]
        public void CatchAllowanceNotFoundTest()
        {
            var ex = Assert.CatchAsync<ContractException>(async () =>
                await _erc20Client.GetAllowance(_ownerAccountKey, _user2AccountKey));
            Assert.IsNotNull(ex);
            Assert.AreEqual((int)ERC20ClientErrors.UnknownAccount, ex.Code);
        }
        
        [Test, Order(7)]
        public async Task CatchInsufficientBalanceTest()
        {
            Assert.IsNotNull(_erc20Client, "This test must run after SetContractHashTest");

            var deployHelper = _erc20Client.TransferTokens(_user2Account.PublicKey,
               _ownerAccountKey,
                300_000,
                1_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            var ex = Assert.CatchAsync<ContractException>(async () =>
                await deployHelper.WaitDeployProcess());
            Assert.IsNotNull(ex);
            Assert.AreEqual((long)ERC20ClientErrors.InsufficientBalance, ex.Code);
        }

        [Test, Order(7)]
        public async Task CatchUnknownAccountTest()
        {
            Assert.IsNotNull(_erc20Client, "This test must run after SetContractHashTest");

            var deployHelper = _erc20Client.TransferTokens(_user1Account.PublicKey,
                _user2AccountKey,
                300_000,
                1_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            var ex = Assert.CatchAsync<ContractException>(async () => await deployHelper.WaitDeployProcess());

            Assert.IsNotNull(ex);
            Assert.AreEqual((long)ERC20ClientErrors.InsufficientBalance, ex.Code);
        }
        
        [Test, Order(7)]
        public async Task CatchTransferFromInsufficientAllowanceTest()
        {
            var deployHelper = _erc20Client.TransferTokensFromOwner(_user1Account.PublicKey,
                _ownerAccountKey,
                _contractHash,
                300_000, 
                1_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);
            
            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            var ex = Assert.CatchAsync<ContractException>(async () => await deployHelper.WaitDeployProcess());

            Assert.IsNotNull(ex);
            Assert.AreEqual((long)ERC20ClientErrors.InsufficientAllowance, ex.Code);
        }

        [Test, Order(7)]
        public void CatchContractHashNotSetTest()
        {
            Assert.IsNotNull(_contractPackageHash, "This test must run after InstallContractTest");
            var client = new ERC20Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);
            
            client.SetContractPackageHash(_contractPackageHash, null);

            var ex = Assert.CatchAsync(() => client.GetBalance(_ownerAccountKey));
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("Initialize the contract client"));
            
            ex = Assert.CatchAsync(() => client.GetAllowance(_ownerAccountKey, _user1AccountKey));
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("Initialize the contract client"));
        }
    }
}
