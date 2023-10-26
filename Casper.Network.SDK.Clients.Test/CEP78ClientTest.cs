using Casper.Network.SDK.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Casper.Network.SDK.Clients.CEP78;
using Casper.Network.SDK.SSE;

namespace Casper.Network.SDK.Clients.Test
{
    public class CEP78ClientTest
    {
        private const string CHAIN_NAME = "casper-net-1";
        private string _nodeAddress;
        private string _cep78WasmFile;
        
        private KeyPair _ownerAccount;
        private KeyPair _user1Account;
        private KeyPair _user2Account;

        private GlobalStateKey _ownerAccountKey;
        private GlobalStateKey _user1AccountKey;
        private GlobalStateKey _user2AccountKey;
        
        private HashKey _contractHash;
        private HashKey _contractPackageHash;
        private CEP78Client _cep78Client;
        
        private const string TOKEN_NAME = "DragonsNFT";
        private const string TOKEN_SYMBOL = "DGNFT";
        
        [OneTimeSetUp]
        public void Init()
        {
            _nodeAddress = Environment.GetEnvironmentVariable("CASPERNETSDK_NODE_ADDRESS");
            Assert.IsNotNull(_nodeAddress,
                "Please, set environment variable CASPERNETSDK_NODE_ADDRESS with a valid node url (with port).");

            _cep78WasmFile = Environment.GetEnvironmentVariable("CEP78TOKEN_WASM");
            Assert.IsNotNull(_nodeAddress,
                "Please, set environment variable CEP78TOKEN_WASM with a valid wasm file name located under TestData folder.");

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
            var client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            var wasmBytes = await File.ReadAllBytesAsync(TestContext.CurrentContext.TestDirectory +
                                                         "/TestData/" + _cep78WasmFile);
            
            // var whitelist = new List<HashKey>()
            // {
            //     new HashKey("hash-c1eb4805e037c6ba9c8ef80ba7f78aa6a5c0533a0c4df1dc6feac942da6969fa"),
            //     new HashKey("hash-dbb3284da4e20be62aeb332c653bfa715c7fa1ef6a73393cd36804b382f10d4e"),
            // };
            
            var installArgs = new CEP78InstallArgs()
            {
                CollectionName = TOKEN_NAME,
                CollectionSymbol = TOKEN_SYMBOL,
                TokenTotalSupply = 100,
                OwnershipMode = NFTOwnershipMode.Transferable,
                NFTKind = NFTKind.Digital,
                NFTMetadataKind = NFTMetadataKind.CEP78,
                NFTIdentifierMode = NFTIdentifierMode.Ordinal,
                MintingMode = MintingMode.Public,
                MetadataMutability = MetadataMutability.Mutable,
                // ACLWhitelist = whitelist,
            };
            
            var deployHelper = client.InstallContract(wasmBytes, installArgs, _ownerAccount.PublicKey, 180_000_000_000);

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
            _cep78Client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            await _cep78Client.SetContractHash(_ownerAccount.PublicKey, $"nft_contract");

            if (_contractHash != null)
                Assert.AreEqual(_contractHash.ToString(), _cep78Client.ContractHash.ToString());

            Assert.AreEqual(TOKEN_NAME, await _cep78Client.GetCollectionName());
            Assert.AreEqual(TOKEN_SYMBOL, await _cep78Client.GetCollectionSymbol());
            Assert.AreEqual(100L, await _cep78Client.GetTokenTotalSupply());

            Assert.AreEqual(NFTOwnershipMode.Transferable, await _cep78Client.GetOwnershipMode());
            Assert.AreEqual(NFTKind.Digital, await _cep78Client.GetNFTKind());
            Assert.AreEqual(NFTMetadataKind.CEP78, await _cep78Client.GetNFTMetadataKind());
            Assert.AreEqual(NFTIdentifierMode.Ordinal, await _cep78Client.GetNFTIdentifierMode());
            Assert.AreEqual(MintingMode.Public, await _cep78Client.GetMintingMode());

            Assert.AreEqual(true, await _cep78Client.GetAllowMinting());
            Assert.AreEqual(BurnMode.Burnable, await _cep78Client.GetBurnMode());
            Assert.AreEqual(MetadataMutability.Mutable, await _cep78Client.GetMetadataMutability());
            Assert.AreEqual(NFTHolderMode.Mixed, await _cep78Client.GetNFTHolderMode());
            Assert.AreEqual(WhitelistMode.Unlocked, await _cep78Client.GetWhitelistMode());

            // var contractWhiteList = await _cep78Client.GetContractWhiteList();
            // Assert.IsNotNull(contractWhiteList);
            // Assert.AreEqual(2, contractWhiteList.Count(h => !string.IsNullOrWhiteSpace(h.ToHexString())));

            var jsonSchema = await _cep78Client.GetJsonSchema();
            Assert.IsEmpty(jsonSchema.Properties);
            
            Assert.AreEqual(_ownerAccountKey.ToHexString(), (await _cep78Client.GetInstaller()).ToHexString());
            Assert.AreEqual(0, await _cep78Client.GetNumberOfMintedTokens());
            Assert.IsNotEmpty(await _cep78Client.GetReceiptName());
        }
        
        [Test, Order(2)]
        public void CatchSetContractHashWrongKeyTest()
        {
            var client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            // catch error for wrong named key
            //
            var ex = Assert.CatchAsync<ContractException>(async () =>
                await client.SetContractHash(_ownerAccount.PublicKey, "wrong_named_key"));
            Assert.IsNotNull(ex);
            Assert.AreEqual("Named key 'wrong_named_key' not found.", ex.Message);
        }

        [Test, Order(3)]
        public async Task SetContractHashTest()
        {
            Assert.IsNotNull(_contractHash, "This test must run after InstallContractTest");
            var client = new CEP47Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            client.SetContractHash(_contractHash.ToString());

            Assert.AreEqual("DragonsNFT", await _cep78Client.GetCollectionName());
        }

        [Test, Order(3)]
        public async Task MintOneTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var metadata = new CEP78TokenMetadata()
            {
                Name = "First token",
                TokenUri = "https://first.token",
                Checksum = "012345678901234567890123456789012345678901234567"
            };
            
            var deployHelper = _cep78Client.Mint(_user2Account.PublicKey,
                _user2AccountKey,
                metadata,
                1_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            var key = await _cep78Client.GetOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());

            key = await _cep78Client.GetFirstOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());
            
            Assert.AreEqual(1, await _cep78Client.GetBalanceOf(_user2AccountKey));
            Assert.AreEqual(1, await _cep78Client.GetNumberOfMintedTokens());
            Assert.IsNull(await _cep78Client.GetApproved(0));

            var mintedMetadata = await _cep78Client.GetMetadata<CEP78TokenMetadata>(0);
            Assert.IsNotNull(mintedMetadata);
            Assert.AreEqual(metadata.Name, mintedMetadata.Name);
            Assert.AreEqual(metadata.TokenUri, mintedMetadata.TokenUri);
            Assert.AreEqual(metadata.Checksum, mintedMetadata.Checksum);
        }

        [Test, Order(4)]
        public async Task TransferTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var deployHelper = _cep78Client.Transfer(_user2Account.PublicKey,
                0, _user2AccountKey, _user1AccountKey, 2_000_000_000);
            
            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            
            var key = await _cep78Client.GetOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user1AccountKey.ToString(), key.ToString());

            key = await _cep78Client.GetFirstOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());
            
            Assert.AreEqual(0, await _cep78Client.GetBalanceOf(_user2AccountKey));
            Assert.AreEqual(1, await _cep78Client.GetBalanceOf(_user1AccountKey));
        }

        [Test, Order(5)]
        public async Task ApproveTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var deployHelper = _cep78Client.Approve(_user1Account.PublicKey,
                0, _user2AccountKey, 2_000_000_000);
            
            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            
            var key = await _cep78Client.GetOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user1AccountKey.ToString(), key.ToString());

            key = await _cep78Client.GetApproved(0);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString() );
        }

        [Test, Order(6)]
        public async Task SetTokenMetadataTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var newTokenMetadata = new CEP78TokenMetadata()
            {
                Name = "Token name",
                TokenUri = "https://token.uri",
                Checksum = "987654321098765432109876543210987654321098765432"
            };
            var deployHelper = _cep78Client.SetTokenMetadata(_user1Account.PublicKey,
                0, newTokenMetadata, 2_000_000_000);
            
            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            
            var mintedMetadata = await _cep78Client.GetMetadata<CEP78TokenMetadata>(0);
            Assert.IsNotNull(mintedMetadata);
            Assert.AreEqual(newTokenMetadata.Name, mintedMetadata.Name);
            Assert.AreEqual(newTokenMetadata.TokenUri, mintedMetadata.TokenUri);
            Assert.AreEqual(newTokenMetadata.Checksum, mintedMetadata.Checksum);
        }

        [Test, Order(7)]
        public async Task TransferFromTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");
            
            var deployHelper = _cep78Client.Transfer(_user2Account.PublicKey,
                0, _user1AccountKey, _ownerAccountKey, 2_000_000_000);
            
            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            
            var key = await _cep78Client.GetOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_ownerAccountKey.ToString(), key.ToString());

            key = await _cep78Client.GetApproved(0);
            Assert.IsNull(key);
        }

        [Test, Order(8)]
        public async Task BurnTokenTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");
            
            var deployHelper = _cep78Client.Burn(_ownerAccount.PublicKey,
                0,  2_000_000_000);
            
            deployHelper.Sign(_ownerAccount);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);
            
            Assert.IsTrue(await _cep78Client.IsTokenBurned(0));

            var tokenIds = await _cep78Client.GetOwnedTokenIdentifiers<ulong>(_ownerAccountKey);
            Assert.AreEqual(1, tokenIds.Count());
        }
        
        [Test, Order(9)]
        public async Task InstallContractWithHashIdentifiersTest()
        {
            var client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            var wasmBytes = await File.ReadAllBytesAsync(TestContext.CurrentContext.TestDirectory +
                                                         "/TestData/" + _cep78WasmFile);

            var installArgs = new CEP78InstallArgs()
            {
                CollectionName = "HashesContract",
                CollectionSymbol = "HASHC",
                TokenTotalSupply = 2,
                OwnershipMode = NFTOwnershipMode.Transferable,
                NFTKind = NFTKind.Digital,
                NFTMetadataKind = NFTMetadataKind.NFT721,
                NFTIdentifierMode = NFTIdentifierMode.Hash,
                MintingMode = MintingMode.Public,
                MetadataMutability = MetadataMutability.Immutable,
                BurnMode = BurnMode.NonBurnable,
                NFTHolderMode = NFTHolderMode.Accounts,
            };
            
            var deployHelper = client.InstallContract(wasmBytes, installArgs, _user1Account.PublicKey, 180_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user1Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);

            _contractHash = deployHelper.ContractHash;
            _contractPackageHash = deployHelper.ContractPackageHash;

            _cep78Client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);
            _cep78Client.SetContractHash(_contractHash);
            
            Assert.AreEqual(NFTIdentifierMode.Hash, await _cep78Client.GetNFTIdentifierMode());
            Assert.AreEqual(NFTMetadataKind.NFT721, await _cep78Client.GetNFTMetadataKind());
            Assert.AreEqual(NFTHolderMode.Accounts, await _cep78Client.GetNFTHolderMode());
        }

        [Test, Order(10)]
        public async Task GetTokenIdentifierTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var metadata = new NFT721tokenMetadata()
            {
                Name = "Second token",
                TokenUri = "https://second.token",
                Symbol = "symbol"
            };
            
            var deployHelper = _cep78Client.Mint(_user2Account.PublicKey,
                _user2AccountKey,
                metadata,
                1_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            var hashes = await _cep78Client.GetOwnedTokenIdentifiers<string>(_user2AccountKey);
            Assert.IsNotNull(hashes);
            
            var tokenHash =  hashes.FirstOrDefault();
            Assert.IsNotNull(tokenHash);            

            var key = await _cep78Client.GetOwnerOf(tokenHash);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());

            key = await _cep78Client.GetFirstOwnerOf(tokenHash);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());
            
            Assert.AreEqual(1, await _cep78Client.GetBalanceOf(_user2AccountKey));
            Assert.AreEqual(1, await _cep78Client.GetNumberOfMintedTokens());
            Assert.IsNull(await _cep78Client.GetApproved(0));

            var mintedMetadata = await _cep78Client.GetMetadata<NFT721tokenMetadata>(tokenHash);
            Assert.IsNotNull(mintedMetadata);
            Assert.AreEqual(metadata.Name, mintedMetadata.Name);
            Assert.AreEqual(metadata.TokenUri, mintedMetadata.TokenUri);
            Assert.AreEqual(metadata.Symbol, mintedMetadata.Symbol);
        }

        [Test, Order(11)]
        public async Task ApproveAllTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var hashes = await _cep78Client.GetOwnedTokenIdentifiers<string>(_user2AccountKey);
            Assert.IsNotNull(hashes);
            
            var tokenHash =  hashes.FirstOrDefault();
            Assert.IsNotEmpty(tokenHash);

            var deployHelper = _cep78Client.ApproveAll(_user2Account.PublicKey, _user1AccountKey,
                2_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();
            await deployHelper.WaitDeployProcess();
            
            Assert.IsTrue(deployHelper.IsSuccess);

            var operatorKey = await _cep78Client.GetApproved(tokenHash);
            Assert.AreEqual(_user1AccountKey.ToString(), operatorKey.ToString());
        }
        
        [Test, Order(12)]
        public async Task CatchBurnErrorTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var hashes = await _cep78Client.GetOwnedTokenIdentifiers<string>(_user2AccountKey);
            Assert.IsNotNull(hashes);
            
            var tokenHash =  hashes.FirstOrDefault();
            Assert.IsNotEmpty(tokenHash);

            var deployHelper = _cep78Client.Burn(_user2Account.PublicKey, tokenHash, 2_000_000_000);
            
            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            var ex = Assert.CatchAsync<ContractException>(deployHelper.WaitDeployProcess);
            
            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("InvalidBurnMode", StringComparison.InvariantCultureIgnoreCase));
            Assert.AreEqual((long)CEP78ClientErrors.InvalidBurnMode, ex.Code);
            
            Assert.IsFalse(deployHelper.IsSuccess);
        }
        
        [Test, Order(13)]
        public async Task InstallContractWithCustomMetadataTest()
        {
            var client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);

            var wasmBytes = await File.ReadAllBytesAsync(TestContext.CurrentContext.TestDirectory +
                                                         "/TestData/" + _cep78WasmFile);

            var jsonSchema = new JsonSchema()
            {
                Properties = new Dictionary<string, JsonSchemaEntry>()
                {
                    {"field1", new JsonSchemaEntry() { Name = "field1", Description = "Description1", Required = true}},
                    {"field2", new JsonSchemaEntry() { Name = "field2", Description = "Description2", Required = false}},
                }
            };
            var installArgs = new CEP78InstallArgs()
            {
                CollectionName = "CustomMetadataContract",
                CollectionSymbol = "CMC",
                TokenTotalSupply = 2,
                NFTMetadataKind = NFTMetadataKind.CustomValidated,
                JsonSchema = jsonSchema,
            };
            
            var deployHelper = client.InstallContract(wasmBytes, installArgs, _user2Account.PublicKey, 180_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);

            _contractHash = deployHelper.ContractHash;
            _contractPackageHash = deployHelper.ContractPackageHash;

            _cep78Client = new CEP78Client(new NetCasperClient(_nodeAddress), CHAIN_NAME);
            _cep78Client.SetContractHash(_contractHash);
            
            Assert.AreEqual(NFTOwnershipMode.Minter, await _cep78Client.GetOwnershipMode());
            Assert.AreEqual(NFTKind.Physical, await _cep78Client.GetNFTKind());
            Assert.AreEqual(NFTMetadataKind.CustomValidated, await _cep78Client.GetNFTMetadataKind());
            Assert.AreEqual(NFTIdentifierMode.Ordinal, await _cep78Client.GetNFTIdentifierMode());
            Assert.AreEqual(MintingMode.Installer, await _cep78Client.GetMintingMode());

            Assert.AreEqual(true, await _cep78Client.GetAllowMinting());
            Assert.AreEqual(BurnMode.Burnable, await _cep78Client.GetBurnMode());
            Assert.AreEqual(MetadataMutability.Immutable, await _cep78Client.GetMetadataMutability());
            Assert.AreEqual(NFTHolderMode.Mixed, await _cep78Client.GetNFTHolderMode());
            Assert.AreEqual(WhitelistMode.Unlocked, await _cep78Client.GetWhitelistMode());

            var gotJsonSchema = await _cep78Client.GetJsonSchema();
            Assert.IsNotNull(gotJsonSchema);
            Assert.IsNotNull(gotJsonSchema.Properties);
            Assert.AreEqual(2, gotJsonSchema.Properties.Count());
            Assert.AreEqual("field1", gotJsonSchema.Properties["field1"].Name);
            Assert.AreEqual("Description1", gotJsonSchema.Properties["field1"].Description);
            Assert.AreEqual(true, gotJsonSchema.Properties["field1"].Required);
            Assert.AreEqual("field2", gotJsonSchema.Properties["field2"].Name);
            Assert.AreEqual("Description2", gotJsonSchema.Properties["field2"].Description);
            Assert.AreEqual(false, gotJsonSchema.Properties["field2"].Required);
        }
        
        class MyCustomMetadata : ITokenMetadata
        {
                [JsonPropertyName("field1")] public string Field1 { get; set; }
                [JsonPropertyName("field2")] public string Field2 { get; set; }

                public string Serialize() => JsonSerializer.Serialize(this, new JsonSerializerOptions()
                    {
                        IgnoreNullValues = true,
                    });
        }
        
        [Test, Order(14)]
        public async Task MintWithCustomMetadataTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");

            var metadata = new MyCustomMetadata()
            {
                Field1 = "Value1",
            };
            
            var deployHelper = _cep78Client.Mint(_user2Account.PublicKey,
                _user2AccountKey,
                metadata,
                1_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            var key = await _cep78Client.GetOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());

            key = await _cep78Client.GetFirstOwnerOf(0);
            Assert.IsNotNull(key);
            Assert.AreEqual(_user2AccountKey.ToString(), key.ToString());
            
            Assert.AreEqual(1, await _cep78Client.GetBalanceOf(_user2AccountKey));
            Assert.AreEqual(1, await _cep78Client.GetNumberOfMintedTokens());
            Assert.IsNull(await _cep78Client.GetApproved(0));

            var mintedMetadata = await _cep78Client.GetMetadata<MyCustomMetadata>(0);
            Assert.IsNotNull(mintedMetadata);
            Assert.AreEqual(metadata.Field1, mintedMetadata.Field1);
            Assert.IsNull(mintedMetadata.Field2);
        }
        
        [Test, Order(14)]
        public async Task SetVariablesTest()
        {
            Assert.IsNotNull(_cep78Client, "This test must run after SetContractHashFromPKTest");
            
            var whitelist = new List<HashKey>()
            {
                new HashKey("hash-c1eb4805e037c6ba9c8ef80ba7f78aa6a5c0533a0c4df1dc6feac942da6969fa"),
                new HashKey("hash-dbb3284da4e20be62aeb332c653bfa715c7fa1ef6a73393cd36804b382f10d4e"),
            };
            
            var deployHelper = _cep78Client.SetVariables(_user2Account.PublicKey,
                allowMinting: false,
                whitelist,
                2_000_000_000);

            Assert.IsNotNull(deployHelper);
            Assert.IsNotNull(deployHelper.Deploy);

            deployHelper.Sign(_user2Account);

            await deployHelper.PutDeploy();

            await deployHelper.WaitDeployProcess();

            Assert.IsTrue(deployHelper.IsSuccess);
            Assert.IsNotNull(deployHelper.ExecutionResult);
            Assert.IsTrue(deployHelper.ExecutionResult.Cost > 0);

            var allowMinting = await _cep78Client.GetAllowMinting();
            Assert.IsFalse(allowMinting);

            // var gotWhitelist = await _cep78Client.GetContractWhiteList();
            // Assert.IsNotNull(gotWhitelist);
            // Assert.AreEqual(2, whitelist.Count());
            // Assert.AreEqual(whitelist[0].ToString(), gotWhitelist.First().ToString());
            // Assert.AreEqual(whitelist[1].ToString(), gotWhitelist.Last().ToString());
        }
    }
}
