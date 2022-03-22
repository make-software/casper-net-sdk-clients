using Casper.Network.SDK.Types;
using NUnit.Framework;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;

namespace Casper.Network.SDK.Clients.Test
{
    public class ERC20ClientTest
    {
        private string _nodeAddress;
        private string _chainName = "casper-net-1";
        private string _erc20WasmFile;
        
        private KeyPair _ownerAccount;
        private KeyPair _user1Account;
        private KeyPair _user2Account;

        private HashKey _contrachHash;
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
            
            var ukFilename = TestContext.CurrentContext.TestDirectory +
                             "/TestData/user1act.pem";
            _user1Account = KeyPair.FromPem(ukFilename);
            Assert.IsNotNull(_user1Account, $"Cannot read owner key from '{ukFilename}");

            _user2Account = KeyPair.CreateNew(KeyAlgo.SECP256K1);
        }

        [Test, Order(1)]
        public async Task InstallContractTest()
        {
            var client = new ERC20Client(_nodeAddress, _chainName);

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

            _contrachHash = deployHelper.ContractHash;
        }

        [Test, Order(2)]
        public async Task SetContractHashTest()
        {
            Assert.IsNotNull(_contrachHash, "This test must run after InstallContractTest");
            _erc20Client = new ERC20Client(_nodeAddress, _chainName);
            var b = await _erc20Client.SetContractHash(_contrachHash.ToString());
            Assert.IsTrue(b);
            
            Assert.AreEqual(TOKEN_NAME, _erc20Client.Name);
            Assert.AreEqual(TOKEN_SYMBOL, _erc20Client.Symbol);
            Assert.AreEqual(TOKEN_DECIMALS, _erc20Client.Decimals);
            Assert.AreEqual(BigInteger.Parse(TOKEN_SUPPLY), _erc20Client.TotalSupply);
            
            Assert.IsNotNull(_erc20Client.ContractHash);
            Assert.AreEqual(_contrachHash.ToString(), _erc20Client.ContractHash.ToString());
        }

        [Test, Order(3)]
        public async Task TransferTokensTest()
        {
            Assert.IsNotNull(_erc20Client, "This test must run after SetContractHashTest");

            var deployHelper = _erc20Client.TransferTokens(_ownerAccount.PublicKey,
                _user2Account.PublicKey,
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

        [Test, Order(4)]
        public async Task GetBalanceTest()
        {
            var balanceOfUser2 = await _erc20Client.GetBalance(_user2Account.PublicKey);
            Assert.AreEqual((BigInteger)10000, balanceOfUser2);
            
            var balanceOfOwner = await _erc20Client.GetBalance(_ownerAccount.PublicKey);
            Assert.AreEqual(BigInteger.Parse(TOKEN_SUPPLY), balanceOfOwner+balanceOfUser2);
        }

        [Test, Order(5)]
        public async Task ApproveSpenderTest()
        {
            Assert.IsNotNull(_erc20Client, "This test must run after SetContractHashTest");

            var deployHelper = _erc20Client.ApproveSpender(_ownerAccount.PublicKey,
                _user1Account.PublicKey,
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

        [Test, Order(6)]
        public async Task TransferTokensFromTest()
        {
            var deployHelper = _erc20Client.TransferTokensFromOwner(_user1Account.PublicKey,
                _ownerAccount.PublicKey,
                _user2Account.PublicKey,
                100_000, 
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

        [Test, Order(7)]
        public async Task AllowanceTest()
        {
            var allowanceOfUser1 = await _erc20Client.GetAllowance(_ownerAccount.PublicKey,
                _user1Account.PublicKey);
            Assert.AreEqual((BigInteger)400_000, allowanceOfUser1);
            
            var balanceOfUser2 = await _erc20Client.GetBalance(_user2Account.PublicKey);
            Assert.AreEqual((BigInteger)110000, balanceOfUser2);
            
            var balanceOfOwner = await _erc20Client.GetBalance(_ownerAccount.PublicKey);
            Assert.AreEqual(BigInteger.Parse(TOKEN_SUPPLY), balanceOfOwner+balanceOfUser2);
        }

        [Test]
        public async Task SetContractHashFromPKTest()
        {
            var client1 = new ERC20Client(_nodeAddress, _chainName);
            var b = await client1.SetContractHash(_ownerAccount.PublicKey, "erc20_token_contract");
            Assert.IsTrue(b);
            Assert.AreEqual(69, client1.ContractHash.ToString().Length);
            
            // catch error for wrong named key
            //
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await client1.SetContractHash(_ownerAccount.PublicKey, "wrong_named_key"));
            Assert.IsNotNull(ex);
            Assert.AreEqual("Named key 'wrong_named_key' not found.", ex.Message);
        }
    }
}

