using Casper.Network.SDK.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Casper.Network.SDK.Clients.Test
{
    public class CEP47ClientTest
    {
        private string _nodeAddress;
        private string _chainName = "casper-net-1";
        private string _cep47WasmFile;

        private KeyPair _ownerAccount;
        private KeyPair _user1Account;
        private KeyPair _user2Account;

        private HashKey _contractHash;
        private CEP47Client _cep47Client;

        private const string TOKEN_NAME = "DragonsNFT";
        private const string TOKEN_SYMBOL = "DGNFT";

        private List<CEP47Event> events;

        [OneTimeSetUp]
        public void Init()
        {
            _nodeAddress = Environment.GetEnvironmentVariable("CASPERNETSDK_NODE_ADDRESS");
            Assert.IsNotNull(_nodeAddress,
                "Please, set environment variable CASPERNETSDK_NODE_ADDRESS with a valid node url (with port).");

            _cep47WasmFile = Environment.GetEnvironmentVariable("CEP47TOKEN_WASM");
            Assert.IsNotNull(_nodeAddress,
                "Please, set environment variable CEP47TOKEN_WASM with a valid wasm file name located under TestData folder.");

            var fkFilename = TestContext.CurrentContext.TestDirectory +
                             "/TestData/owneract.pem";
            _ownerAccount = KeyPair.FromPem(fkFilename);
            Assert.IsNotNull(_ownerAccount, $"Cannot read owner key from '{fkFilename}");

            var uk1Filename = TestContext.CurrentContext.TestDirectory +
                              "/TestData/user1act.pem";
            _user1Account = KeyPair.FromPem(uk1Filename);
            Assert.IsNotNull(_user1Account, $"Cannot read owner key from '{uk1Filename}");

            var uk2Filename = TestContext.CurrentContext.TestDirectory +
                              "/TestData/user2act.pem";
            _user2Account = KeyPair.FromPem(uk2Filename);
            Assert.IsNotNull(_user2Account, $"Cannot read owner key from '{uk2Filename}");
        }

        [Test, Order(1)]
        public async Task InstallContractTest()
        {
            var client = new CEP47Client(new NetCasperClient(_nodeAddress), _chainName);

            var wasmBytes = File.ReadAllBytes(TestContext.CurrentContext.TestDirectory +
                                              "/TestData/" + _cep47WasmFile);

            var meta = new Dictionary<string, string>
            {
                {"origin", "fire"}
            };

            var deployHelper = client.InstallContract(wasmBytes,
                TOKEN_NAME, TOKEN_NAME, TOKEN_SYMBOL, meta,
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
        }

        [Test, Order(2)]
        public async Task SetContractHashFromPKTest()
        {
            _cep47Client = new CEP47Client(new NetCasperClient(_nodeAddress), _chainName);

            var b = await _cep47Client.SetContractHash(_ownerAccount.PublicKey, $"{TOKEN_NAME}_contract_hash");
            Assert.IsTrue(b);

            if (_contractHash != null)
                Assert.AreEqual(_contractHash.ToString(), _cep47Client.ContractHash.ToString());

            events = new List<CEP47Event>();
            _cep47Client.OnCEP47Event += evt => events.Add(evt);
            await _cep47Client.ListenToEvents();
        }
        
        [Test, Order(2)]
        public void CatchSetContractHashWrongKeyTest()
        {
            var client = new CEP47Client(new NetCasperClient(_nodeAddress), _chainName);

            // catch error for wrong named key
            //
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await client.SetContractHash(_ownerAccount.PublicKey, "wrong_named_key"));
            Assert.IsNotNull(ex);
            Assert.AreEqual("Named key 'wrong_named_key' not found.", ex.Message);
        }

        [Test, Order(3)]
        public async Task SetContractHashTest()
        {
            Assert.IsNotNull(_contractHash, "This test must run after InstallContractTest");
            var client = new CEP47Client(new NetCasperClient(_nodeAddress), _chainName);

            var b = await client.SetContractHash(_contractHash.ToString());
            Assert.IsTrue(b);

            Assert.AreEqual(TOKEN_NAME, client.Name);
            Assert.AreEqual(TOKEN_SYMBOL, client.Symbol);
            Assert.AreEqual(BigInteger.Zero, client.TotalSupply);

            Assert.IsNotNull(client.Meta);

            Assert.IsNotNull(client.ContractHash);
            Assert.AreEqual(_contractHash.ToString(), client.ContractHash.ToString());
        }

        [Test, Order(3)]
        public async Task MintOneTest()
        {
            Assert.IsNotNull(_cep47Client, "This test must run after SetContractHashTest");

            var tokenMeta = new Dictionary<string, string>
            {
                {"color", "green"}
            };

            var deployHelper = _cep47Client.MintOne(_ownerAccount.PublicKey,
                _user2Account.PublicKey,
                new BigInteger(1),
                tokenMeta,
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
        public async Task GetTokenIdByIndexTest()
        {
            var tokenId = await _cep47Client.GetTokenIdByIndex(_user2Account.PublicKey, 0);
            Assert.AreEqual(BigInteger.One, tokenId);
        }

        [Test, Order(4)]
        public async Task GetTokeMetadataTest()
        {
            var tokenMeta = await _cep47Client.GetTokenMetadata(BigInteger.One);

            Assert.IsNotNull(tokenMeta);
            Assert.AreEqual(1, tokenMeta.Keys.Count);
            Assert.AreEqual("green", tokenMeta["color"]);
        }

        [Test, Order(4)]
        public async Task GetBalanceTest()
        {
            var balanceOf = await _cep47Client.GetBalanceOf(_user2Account.PublicKey);
            Assert.AreEqual(BigInteger.One, balanceOf);
        }
        
        [Test, Order(5)]
        public async Task UpdateTokenMetadataTest()
        {
            var tokenMeta = new Dictionary<string, string>
            {
                {"color", "lightgreen"},
                {"name", "drako"}
            };
            var deployHelper = _cep47Client.UpdateTokenMetadata(_user2Account.PublicKey,
                new BigInteger(1),
                tokenMeta,
                1_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            
            var tm = await _cep47Client.GetTokenMetadata(new BigInteger(1));
            Assert.AreEqual(2, tm.Keys.Count);
            Assert.AreEqual("lightgreen", tm["color"]);
            Assert.AreEqual("drako", tm["name"]);
        }

        [Test, Order(5)]
        public async Task MintCopiesTest()
        {
            Assert.IsNotNull(_cep47Client, "This test must run after SetContractHashTest");

            var tokenIds = new List<BigInteger> {new(10), new(11), new(12)};

            var tokenMeta = new Dictionary<string, string>
            {
                {"color", "red"}
            };

            var deployHelper = _cep47Client.MintCopies(_ownerAccount.PublicKey,
                _user2Account.PublicKey,
                tokenIds,
                tokenMeta,
                3_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_ownerAccount);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            foreach (var tId in tokenIds)
            {
                var tm = await _cep47Client.GetTokenMetadata(tId);
                Assert.AreEqual(tokenMeta.Keys.Count, tm.Keys.Count);
                Assert.AreEqual(tokenMeta["color"], tm["color"]);
            }
        }

        [Test, Order(5)]
        public async Task MintManyTest()
        {
            Assert.IsNotNull(_cep47Client, "This test must run after SetContractHashTest");

            var tokenIds = new List<BigInteger> {new(20), new(21), new(22)};

            var tokenMetas = new List<Dictionary<string, string>>
            {
                new() {{"color", "orange"}},
                new() {{"color", "yellow"}},
                new() {{"color", "purple"}}
            };

            var deployHelper = _cep47Client.MintMany(_ownerAccount.PublicKey,
                _user1Account.PublicKey,
                tokenIds,
                tokenMetas,
                30_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_ownerAccount);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            for (int i = 0; i < tokenIds.Count; i++)
            {
                var tm = await _cep47Client.GetTokenMetadata(tokenIds[i]);
                Assert.AreEqual(tokenMetas[i].Keys.Count, tm.Keys.Count);
                Assert.AreEqual(tokenMetas[i]["color"], tm["color"]);
            }
        }

        [Test, Order(6)]
        public async Task GetOwnerOfTest()
        {
            var user2Acct = new AccountHashKey(_user2Account.PublicKey).ToString();
            var tokenIds = new List<BigInteger> {new(10), new(11), new(12)};
            foreach (var tokenId in tokenIds)
            {
                var ownerAcct = await _cep47Client.GetOwnerOf(tokenId);
                Assert.AreEqual(user2Acct, ownerAcct.ToString());
            }

            var user1Acct = new AccountHashKey(_user1Account.PublicKey).ToString();
            tokenIds = new List<BigInteger> {new(20), new(21), new(22)};
            foreach (var tokenId in tokenIds)
            {
                var ownerAcct = await _cep47Client.GetOwnerOf(tokenId);
                Assert.AreEqual(user1Acct, ownerAcct.ToString());
            }
        }

        [Test, Order(7)]
        public async Task TransferTest()
        {
            var deployHelper = _cep47Client.TransferToken(_user2Account.PublicKey,
                _user1Account.PublicKey,
                new List<BigInteger>() {new(10), new(11)},
                2_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            var owner10 = await _cep47Client.GetOwnerOf(new BigInteger(10));
            var user1Acct = new AccountHashKey(_user1Account.PublicKey).ToString();
            Assert.AreEqual(user1Acct, owner10.ToString());
        }

        [Test, Order(7)]
        public async Task ApproveTest()
        {
            var deployHelper = _cep47Client.Approve(_user2Account.PublicKey,
                _user1Account.PublicKey,
                new List<BigInteger>() {new(12)},
                2_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
        }

        [Test, Order(8)]
        public async Task GetApprovedSpenderTest()
        {
            var spender = await _cep47Client.GetApprovedSpender(_user2Account.PublicKey, new BigInteger(12));
            var user1Acct = new AccountHashKey(_user1Account.PublicKey).ToString();
            Assert.AreEqual(user1Acct, spender.ToString());
        }

        [Test, Order(9)]
        public async Task TransferFromTest()
        {
            var deployHelper = _cep47Client.TransferTokenFrom(_user1Account.PublicKey,
                _user2Account.PublicKey,
                _ownerAccount.PublicKey,
                new List<BigInteger>() {new(12)},
                2_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            var owner12 = await _cep47Client.GetOwnerOf(new BigInteger(12));
            var ownerAcct = new AccountHashKey(_ownerAccount.PublicKey).ToString();
            Assert.AreEqual(ownerAcct, owner12.ToString());
        }

        [Test, Order(10)]
        public async Task BurnTest()
        {
            var deployHelper = _cep47Client.BurnOne(_user1Account.PublicKey,
                _user1Account.PublicKey,
                new BigInteger(20),
                1_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            var task1 = deployHelper.WaitDeployProcess().ContinueWith(async _ =>
            {
                Assert.IsTrue(deployHelper.IsSuccess);
                Assert.IsNotNull(deployHelper.ExecutionResult);
                Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
                
                var meta = await _cep47Client.GetTokenMetadata(new BigInteger(20));
                Assert.IsNull(meta); // dictionary returns None, converted to null
            });

            var deployHelper2 = _cep47Client.BurnMany(_user1Account.PublicKey,
                _user1Account.PublicKey,
                new List<BigInteger>() {new(21), new(22)},
                2_000_000_000);

            Assert.IsNotNull(deployHelper2);
            Assert.IsNotNull(deployHelper2.Deploy);

            deployHelper2.Sign(_user1Account);

            await deployHelper2.PutDeploy();

            var task2 = deployHelper2.WaitDeployProcess().ContinueWith(async _ =>
            {
                Assert.IsTrue(deployHelper2.IsSuccess);
                Assert.IsNotNull(deployHelper2.ExecutionResult);
                Assert.IsTrue(deployHelper2.ExecutionResult.Cost > 0);

                var meta2 = await _cep47Client.GetTokenMetadata(new BigInteger(21));
                Assert.IsNull(meta2); // dictionary returns None, converted to null

                var meta3 = await _cep47Client.GetTokenMetadata(new BigInteger(22));
                Assert.IsNull(meta3); // dictionary returns None, converted to null
            });

            Task.WaitAll(task1, task2);
        }

        [Test, Order(11)]
        public void EventsTriggeredTest()
        {
            var ev = events.FirstOrDefault(evt => evt.EventType == CEP47EventType.MintOne &&
                                                   evt.TokenId == "1");
            Assert.IsNotNull(ev);
            Assert.IsNotNull(ev.ContractPackageHash);
            Assert.IsNotNull(ev.DeployHash);
            Assert.IsNotNull(ev.Recipient);
            
            Assert.AreEqual(7, events.Count(evt => evt.EventType == CEP47EventType.MintOne));

            Assert.IsTrue(events.Any(evt => evt.EventType == CEP47EventType.Transfer &&
                                            evt.TokenId == "10"));
            Assert.IsTrue(events.Any(evt => evt.EventType == CEP47EventType.Transfer &&
                                            evt.TokenId == "11"));
            Assert.IsTrue(events.Any(evt => evt.EventType == CEP47EventType.Transfer &&
                                            evt.TokenId == "12"));
            
            ev = events.FirstOrDefault(evt => evt.EventType == CEP47EventType.Approve &&
                                            evt.TokenId == "12");
            Assert.IsNotNull(ev);
            Assert.IsNotNull(ev.Owner);
            Assert.IsNotNull(ev.Spender);
            
            Assert.AreEqual(3, events.Count(evt => evt.EventType == CEP47EventType.BurnOne));
            
            Assert.IsTrue(events.Any(evt => evt.EventType == CEP47EventType.UpdateMetadata &&
                                            evt.TokenId == "1"));
        }
    }
}
