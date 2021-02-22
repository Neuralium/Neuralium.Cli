using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Neuralia.NClap.Metadata;
using Neuralia.Blockchains.Core.Logging;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime.Commands;

namespace Neuralium.Cli.Classes.Runtime
{
	
    public enum ApiCommands
    {
		// -----------------------------------------------------------------------------
	    [HelpCommand(ShortName = "h", LongName = "help", Description = @"Help, note that you get signle-command help. For example:
```
> help exit
Usage:
    exit
```")]
	    Help,
	    // -----------------------------------------------------------------------------
	    [Command(typeof(BackupWallet), LongName = nameof(BackupWallet), Description = "Launch the wallet backup process (object)")]
	    BackupWallet, 
	    // -----------------------------------------------------------------------------
		[Command(typeof(CanPublishAccount), LongName = nameof(CanPublishAccount), Description = "Can we publish our account? (bool)")]
		CanPublishAccount,
		// -----------------------------------------------------------------------------
		[Command(typeof(ClearAppointment), LongName = nameof(ClearAppointment), Description = "Clears the current appointment (bool)")]
		ClearAppointment,
		// -----------------------------------------------------------------------------
		[Command(typeof(ClearCachedCredentials), LongName = nameof(ClearCachedCredentials), Description = "Clears our cached credentials (bool)")]
		ClearCachedCredentials,
		// -----------------------------------------------------------------------------		
		[Command(typeof(ConfigurePortMappingMode), LongName = nameof(ConfigurePortMappingMode), Description = "Configure port mapping mode (bool)")]
		ConfigurePortMappingMode,
		// -----------------------------------------------------------------------------		
		[Command(typeof(CreateNewWallet), LongName = nameof(CreateNewWallet), Description = "Allows you to create a new wallet, and potentially to publish it [long-running] (uint)")]
		CreateNewWallet,
		// -----------------------------------------------------------------------------		
		[Command(typeof(CreateNextXmssKey), LongName = nameof(CreateNextXmssKey), Description = "Allows you to create a new Xmss key (Expert Users) (bool)")]
		CreateNextXmssKey,
		// -----------------------------------------------------------------------------		
		[Command(typeof(CreateStandardAccount), LongName = nameof(CreateStandardAccount), Description = "Allows you to create a new account inside your wallet (Advanced Users) (bool)")]
		CreateStandardAccount,
		// -----------------------------------------------------------------------------		
		[Command(typeof(GenerateXmssKeyIndexNodeCache), LongName = nameof(GenerateXmssKeyIndexNodeCache), Description = "Generate Xmss Key cache (Expert Users) (void)")]
		GenerateXmssKeyIndexNodeCache, 
		// -----------------------------------------------------------------------------		
		[Command(typeof(GetBlockSizeAndHash), LongName = nameof(GetBlockSizeAndHash), Description = "Single block's size and hash (string)")]
		GetBlockSizeAndHash,
		// -----------------------------------------------------------------------------		
		[Command(typeof(GetCurrentOperatingMode), LongName = nameof(GetCurrentOperatingMode), Description = "Returns the current operation mode (int)")]
		GetCurrentOperatingMode,
		// -----------------------------------------------------------------------------		
		[Command(typeof(GetMiningRegistrationIpMode), LongName = nameof(GetMiningRegistrationIpMode), Description = "Mining registration IP mode (byte)")]
		GetMiningRegistrationIpMode,
		// -----------------------------------------------------------------------------	
		[Command(typeof(GetPortMappingStatus), LongName = nameof(GetPortMappingStatus), Description = "Port mapping status (object)")]
		GetPortMappingStatus,
		// -----------------------------------------------------------------------------		
		[Command(typeof(GetPublicIPMode), LongName = nameof(GetPublicIPMode), Description = "Public ip mode (byte)")]
		GetPublicIPMode,
		// -----------------------------------------------------------------------------		
		[Command(typeof(IsMiningAllowed), LongName = nameof(IsMiningAllowed), Description = "Are we allowed to mine? (bool)")]
		IsMiningAllowed,
		// -----------------------------------------------------------------------------		
		[Command(typeof(IsMiningEnabled), LongName = nameof(IsMiningEnabled), Description = "Are we currently mining? (bool)")]
		IsMiningEnabled,
		// -----------------------------------------------------------------------------		
		[Command(typeof(IsWalletLoaded), LongName = nameof(IsWalletLoaded), Description = "Is the wallet loaded? (bool)")]
		IsWalletLoaded,
		// -----------------------------------------------------------------------------		
		[Command(typeof(IsWalletSynced), LongName = nameof(IsWalletSynced), Description = "Is the wallet fully synced? (bool)")]
		IsWalletSynced, 
		// -----------------------------------------------------------------------------		
		[Command(typeof(SyncBlockchain), LongName = nameof(SyncBlockchain), Description = "Commands to sync the blockchain (bool)")]
		SyncBlockchain,
		// -----------------------------------------------------------------------------		
		[Command(typeof(LoadWallet), LongName = nameof(LoadWallet), Description = "Loads the wallet [long running] (uint or object)")]
		LoadWallet,
		// -----------------------------------------------------------------------------		
		[Command(typeof(Ping), LongName = nameof(Ping), Description = "Pings the server (bool)")]
		Ping,
		// -----------------------------------------------------------------------------	
		[Command(typeof(PublishAccount), LongName = nameof(PublishAccount), Description = "Sends the account publication transaction [long-running] (uint)")]
		PublishAccount,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryAppointmentConfirmationResult), LongName = nameof(QueryAppointmentConfirmationResult), Description = "Query an appointment confirmation result (bool)")]
		QueryAppointmentConfirmationResult,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryAccountTotalNeuraliums), LongName = nameof(QueryAccountTotalNeuraliums), Description = "Query an account's total amount of Neuraliums (object)")]
		QueryAccountTotalNeuraliums,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryBlock), LongName = nameof(QueryBlock), Description = "Query a single block (string)")]
		QueryBlock,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryBlockBytes), LongName = nameof(QueryBlockBytes), Description = "Query a single block bytes (byte[])")]
		QueryBlockBytes,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryBlockChainInfo), LongName = nameof(QueryBlockChainInfo), Description = "Query the blockchain's info (object)")]
		QueryBlockChainInfo,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryBlockchainSynced), LongName = nameof(QueryBlockchainSynced), Description = "Query if the blockchain is fully synced (bool)")]
		QueryBlockchainSynced,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryBlockBinaryTransactions), LongName = nameof(QueryBlockBinaryTransactions), Description = "Query block's transaction in binary format (List[object])")]
		QueryBlockBinaryTransactions,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryBlockHeight), LongName = nameof(QueryBlockHeight), Description = "Query what is the current block height (long)")]
		QueryBlockHeight,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryChainStatus), LongName = nameof(QueryChainStatus), Description = @"Query the chain's status (object)
### Return Value
A `json` dictionary with various information. For example:
```
{
  'walletInfo': {
		'walletExists': true,
		'walletFullyCreated': true,
		'isWalletLoaded': true,
		'walletEncrypted': false,
		'walletPath': '/path/to/your/.neuralium/neuralium'
    },
    'minRequiredPeerCount': 1,
    'miningTier': 255
}
```
")]
		QueryChainStatus,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryCompressedBlock), LongName = nameof(QueryCompressedBlock), Description = "Query a block's compressed details (byte[])")]
		QueryCompressedBlock,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryCurrentDifficulty), LongName = nameof(QueryCurrentDifficulty), Description = "Query current mining difficulty level (long)")]
		QueryCurrentDifficulty,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryDecomposedBlock), LongName = nameof(QueryDecomposedBlock), Description = "Query a block's decompressed details (object)")]
		QueryDecomposedBlock,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryDecomposedBlockJson), LongName = nameof(QueryDecomposedBlockJson), Description = "Query a block's decompressed details (string)")]
		QueryDecomposedBlockJson,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryDefaultWalletAccountCode), LongName = nameof(QueryDefaultWalletAccountCode), Description = "Query which among wallet's accounts is set as the default account (account code version) (string)")]
		QueryDefaultWalletAccountCode,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryDefaultWalletAccountId), LongName = nameof(QueryDefaultWalletAccountId), Description = "Query which among wallet's accounts is set as the default account (account id version) (string)")]
		QueryDefaultWalletAccountId,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryElectionContext), LongName = nameof(QueryElectionContext), Description = "Query a block's election context (object)")]
		QueryElectionContext,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryLowestAccountBlockSyncHeight), LongName = nameof(QueryLowestAccountBlockSyncHeight), Description = "Query a lowest sync height among accounts (long)")]
		QueryLowestAccountBlockSyncHeight,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryMiningHistory), LongName = nameof(QueryMiningHistory), Description = "Query our mining history (List[object])")]
		QueryMiningHistory,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryMiningStatistics), LongName = nameof(QueryMiningStatistics), Description = "Query our mining statistics (object)")]
		QueryMiningStatistics,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryNeuraliumTimelineHeader), LongName = nameof(QueryNeuraliumTimelineHeader), Description = "Query transactions timeline columns headers (object)")]
		QueryNeuraliumTimelineHeader,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryNeuraliumTimelineSection), LongName = nameof(QueryNeuraliumTimelineSection), Description = "Query transactions timeline rows for a given day (object)")]
		QueryNeuraliumTimelineSection,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryNeuraliumTransactionPool), LongName = nameof(QueryNeuraliumTransactionPool), Description = "Query the current pool of transactions (List[object])")]
		QueryNeuraliumTransactionPool,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryPeerConnectionDetails), LongName = nameof(QueryPeerConnectionDetails), Description = "Query details about peers connection (object)")]
		QueryPeerConnectionDetails,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QuerySupportedChains), LongName = nameof(QuerySupportedChains), Description = "Query what chain we support (object)")]
		QuerySupportedChains,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QuerySystemInfo), LongName = nameof(QuerySystemInfo), Description = "Query node's system information")]
		QuerySystemInfo,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletAccountAppointmentDetails), LongName = nameof(QueryWalletAccountAppointmentDetails), Description = "Query an account's appointment details (Note: you will need a GUI to perform the appointment) (object)")]
		QueryWalletAccountAppointmentDetails,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletAccountDetails), LongName = nameof(QueryWalletAccountDetails), Description = "Query an account's details (object)")]
		QueryWalletAccountDetails,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletAccountPresentationTransactionId), LongName = nameof(QueryWalletAccountPresentationTransactionId), Description = "Query an account's presentation transaction's id (string)")]
		QueryWalletAccountPresentationTransactionId,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletAccounts), LongName = nameof(QueryWalletAccounts), Description = @"Query the wallet's accounts (List[object])
### Return Value
The wallet's accounts (list fo json dictionnaries). For example:

```
[
  {
    'accountCode': 'XYZ',
	'accountId': '{*ABCD-ABCD-ABCD}',
	'friendlyName': 'MyUserAccount',
	'isActive': true,
	'status': 1
  }
]
```
Of particular in terest is the meaning of the `'status'` field:
```
Unknown = 0,
New = 1,
Dispatched = 2,
Published = 3,
Dispatching = 4,
Rejected = 255
```
Note that the `'accountId'` field, when prefixed with a '*' like above, is a provisional account hash which will be replaced by a definitive account id once the account reaches the `'Published'' status.
")]
		QueryWalletAccounts,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletTransactionHistory), LongName = nameof(QueryWalletTransactionHistory), Description = "Query the wallet's transactions history (List[object])")]
		QueryWalletTransactionHistory,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletTransactionHistoryDetails), LongName = nameof(QueryWalletTransactionHistoryDetails), Description = "Query details abount a particular transaction among wallet's transactions history (object)")]
		QueryWalletTransactionHistoryDetails,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletInfo), LongName = nameof(QueryWalletInfo), Description = "Query information on the wallet (object)")]
		QueryWalletInfo,
		// -----------------------------------------------------------------------------	
		[Command(typeof(QueryWalletSynced), LongName = nameof(QueryWalletSynced), Description = "Query if wallet is fully synced (bool)")]
		QueryWalletSynced,
		// -----------------------------------------------------------------------------	
		[Command(typeof(ReadAppSetting), LongName = "ReadAppSetting", Description = "Read application setting 'name' (object)")]
		ReadAppSetting,
		// -----------------------------------------------------------------------------	
        [Command(typeof(GenericOperation), ShortName = "r", LongName = "run", Description = @"Run an operation by name, allows you to use the --jparams option. For example:
run SendNeuraliums jparams=[{'Name':'targetAccountId','Value':'{SF3}'},{'Name':'amount','Value':'1.1'}]")]
        Run,
        // -----------------------------------------------------------------------------	
		[Command(typeof(RequestAppointment), LongName = nameof(RequestAppointment), Description = "Request an appointment (Note: you will need a GUI to perform the appointment) (bool)")]
		RequestAppointment,
		// -----------------------------------------------------------------------------	
		[Command(typeof(ResetWalletIndex), LongName = nameof(ResetWalletIndex), Description = "Resets the wallet indexing (bool)")]
		ResetWalletIndex,
		// -----------------------------------------------------------------------------	
		[Command(typeof(RestoreWalletFromBackup), LongName = nameof(RestoreWalletFromBackup), Description = "Restores a wallet from a backup (bool)")]
		RestoreWalletFromBackup,
		// -----------------------------------------------------------------------------	
		[Command(typeof(RestoreWalletNarballBackup), LongName = nameof(RestoreWalletNarballBackup), Description = "Restores a wallet 'narball' backup (bool)")]	
		RestoreWalletNarballBackup,
		// -----------------------------------------------------------------------------	
		[Command(typeof(SetActiveAccount), LongName = nameof(SetActiveAccount), Description = "Set the wallet's active accounts (bool)")]
		SetActiveAccount,
		
		[Command(typeof(SetPuzzleAnswers), LongName = nameof(SetPuzzleAnswers), Description = "Sets the answers for each puzzles (Note: you will need a GUI to perform the appointment) (void)")]
		SetPuzzleAnswers,
		// -----------------------------------------------------------------------------	
		[Command(typeof(SetSMSConfirmationCodeString), LongName = nameof(SetSMSConfirmationCodeString), Description = "Sets the SMS confirmation string (void)")]
		SetSMSConfirmationCodeString,
		// -----------------------------------------------------------------------------	
		[Command(typeof(Shutdown), LongName = nameof(Shutdown), Description = "Send a shutdown request to the server (bool)")]
		Shutdown,
		// -----------------------------------------------------------------------------	
		[Command(typeof(StartMining), LongName = nameof(StartMining), Description = "Starts the mining")]
		StartMining,
		// -----------------------------------------------------------------------------	
		[Command(typeof(StopMining), LongName = nameof(StopMining), Description = "Stops the mining")]
		StopMining,
		// -----------------------------------------------------------------------------	
		[Command(typeof(SendNeuraliums), LongName = nameof(SendNeuraliums), Description = "Send neuralium to another account [long-running] (uint)")]
		SendNeuraliums,
		// -----------------------------------------------------------------------------	
		[Command(typeof(SignXmssMessage), LongName = nameof(SignXmssMessage), Description = "Signs an an Xmss message (Expert Users) (byte[])")]
		SignXmssMessage,
		// -----------------------------------------------------------------------------	
		[Command(typeof(Test), LongName = nameof(Test), Description = "Tests the server")]
		Test,
		// -----------------------------------------------------------------------------	
		[Command(typeof(TestP2pPort), LongName = nameof(TestP2pPort), Description = @"Tests p2p port
### Return Value

The return value is a bit field, so it can be sum of many of the following values
```
	Failed = 0 (bit 0),
	Success = 1 (bit 1),
	RequestCallback = 2 (bit 2),

	CallbackAttempted = 4 (bit 3),
	CallbackSucceeded = 8 (bit 4),

	Ipv6 = 64 (bit 6),
	IsValidator = 128 (bit 7)
```

")]
		TestP2pPort,
		// -----------------------------------------------------------------------------	
		[Command(typeof(ToggleServerMessages), LongName = nameof(ToggleServerMessages), Description = "Enable/Disable server messages")]
		ToggleServerMessages,
		// -----------------------------------------------------------------------------	
        [Command(typeof(WalletExists), LongName = nameof(WalletExists), Description = "Can the wallet be found? (bool)")]
		WalletExists,
		// -----------------------------------------------------------------------------	
		[Command(typeof(WriteAppSetting), LongName = nameof(WriteAppSetting), Description = "Write/modify an a setting in config.json's AppSettings section")]	
		WriteAppSetting,
		// -----------------------------------------------------------------------------	
		[Command(typeof(ExitCommand), LongName = "exit", Description = "Exits the shell")]
        Exit,
        // -----------------------------------------------------------------------------	
        [Command(typeof(PrintMarkdown), LongName = "PrintMarkdown", Description = "Prints a this very Markdown page!")]
        PrintMarkdown
    }

    public class PrintMarkdown : Command
    {
	    public override Task<CommandResult> ExecuteAsync(CancellationToken cancel)
	    {
		    var type = typeof(ApiCommands);
		    Console.WriteLine($"# Api Commands");
		    Console.WriteLine($"(This documentation was **auto generated** using neuraliumcli's {nameof(PrintMarkdown)} command, please edit 'Neuralium.Cli/src/Classes/Runtime/ApiCommands.cs' directly if you have any changes to make)");

		    foreach (var field in type.GetMembers())
		    {
			    try
			    {
				    var attribute = (field.GetCustomAttribute<CommandAttribute>(false));
				    
				    Console.WriteLine($"## {attribute.LongName}");
				    Console.WriteLine($"{attribute.Description}");

				    var commandType = attribute.GetImplementingType(attribute.GetType());

				    var positionalArguments = commandType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
					    .Where(info =>
					    {
						    try
						    {
							    return info.GetCustomAttribute<PositionalArgumentAttribute>() != null;
						    }
						    catch (Exception e)
						    {
							    return false;
						    }
					    }).OrderBy(info => info.GetCustomAttribute<PositionalArgumentAttribute>().Position).ToImmutableArray();

				    if (positionalArguments.Length > 0)
				    {
					    Console.WriteLine("### Positional arguments");
					    foreach (var property in positionalArguments)
					    {
						    try
						    {
							    var attrib = property.GetCustomAttribute<PositionalArgumentAttribute>();
							    string defaultValue = attrib.DefaultValue != null ? (", default = " + attrib.DefaultValue) : "";
							    string description = attrib.Description != null ? $": {attrib.Description}" : "";
								    
							    Console.WriteLine($"{attrib.Position+1}. {property.Name} ({attrib.Flags.ToString()}, {property.PropertyType.Name}{defaultValue}){description}");

						    }
						    catch (Exception e)
						    {
							    Console.WriteLine(e);
							    throw;
						    }
					    }
				    }
				    Console.WriteLine("");

			    }
			    catch (Exception e)
			    {
			    }
			    
		    }
		    
		    return Task.FromResult(CommandResult.Success);
	    }
    }
    public class NamedOperation : OperationBase
    {
        public NamedOperation()
        {
            this.operationName = this.GetType().Name;
        }

        public override Task<CommandResult> ExecuteAsync(CancellationToken cancel)
        {
	        //lets now fill parameters from the command type's properties, ordered using PositionalArgument's Position property 
	        var properties = this.GetType().GetProperties()
		        .Where(property => Attribute.IsDefined(property, typeof(PositionalArgumentAttribute)))
		        .OrderBy(property => ((PositionalArgumentAttribute)property
			        .GetCustomAttributes(typeof(PositionalArgumentAttribute), false)
			        .Single()).Position);

	        string toJson(Dictionary<int, string> dict)
	        {
		        var entries = dict.Select(d =>
			        $"{d.Key}:{d.Value}");
		        return "{" + string.Join(",", entries) + "}";
	        }
	        
	        foreach (var property in properties)
	        {
		        var value = property.GetValue(this);

		        if (value != null)
		        {
			        if(value is Dictionary<int, string> dict)
				        this.parameters.Add(toJson(dict)); //TODO use NClap custom parsing facilities
			        else
						this.parameters.Add(value.ToString());
		        }
	        }

	        return base.ExecuteAsync(cancel);
        }
    }

    public class NamedLongRunningOperation : NamedOperation
    {
	    [NamedArgument(ArgumentFlags.Optional, ShortName = "t", LongName = "timeout", DefaultValue = 0.0, Description  = "Wait 'timeout' seconds for completion in the case of long running operations. If timeout is reached, correlationId is returned.")]
	    public double TimeoutForLongOperation
	    {
		    get => this.timeoutForLongOperation;
		    set => this.timeoutForLongOperation = value;
	    }
    }

    public class ToggleServerMessages : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "Whether to enable server message or not")]
	    public bool Enable { get; set; }
    }

    public class TestP2pPort : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = @"Which port 
   * P2p = 1,
   * Validator = 2,  
   * ValidatorHttp = 3")]
	    public int TestPort { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "Use callback?")]
	    public bool Callback { get; set; }
    }
    
    public class QuerySystemInfo : NamedOperation { }

    public class QuerySupportedChains : NamedOperation{}
    public class QueryBlockHeight : NamedOperation{}
    public class QueryChainStatus : NamedOperation{}
    public class IsWalletLoaded : NamedOperation{}
    public class GetPortMappingStatus : NamedOperation{}

    public class ConfigurePortMappingMode : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "Use UPnP protocol?")]
	    public bool UseUPnP { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "Use PmP protocol (Apple routers)")]
	    public bool UsePmP { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 2, Description = "The index of the nat device you want to use, among those returned by " + nameof(ApiCommands.GetPortMappingStatus))]
	    public int NatDeviceIndex { get; set; }
    }
    public class GetPublicIPMode : NamedOperation{}
    public class BackupWallet : NamedOperation{}
    public class QueryWalletInfo : NamedOperation{}
    public class QueryLowestAccountBlockSyncHeight : NamedOperation{}

    public class QueryCurrentDifficulty : NamedOperation { }

    public class QueryMiningHistory : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int Page { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public int PageSize { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    public byte MaxLevel { get; set; } 
    }
    public class QueryMiningStatistics : NamedOperation{}

    public class QueryNeuraliumTimelineHeader : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
    }

    public class QueryNeuraliumTransactionPool : NamedOperation{}
    public class QueryNeuraliumTimelineSection : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public DateTime day { get; set; }
    }
    public class QueryDecomposedBlock : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId { get; set; }
    }
    public class QueryDecomposedBlockJson : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId { get; set; }
    }

    public class GetBlockSizeAndHash : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId { get; set; }
    }
    
    public class QueryBlockBytes : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId { get; set; }
    }

    public class QueryAppointmentConfirmationResult : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
    }

    public class SetPuzzleAnswers : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public List<int> Answers { get; set; }
    }
    public class SetActiveAccount : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
    }

    public class GenerateXmssKeyIndexNodeCache : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public byte Ordinal { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    public long Index { get; set; }
    }

    public class SignXmssMessage : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public byte[] Message { get; set; }
    }
    public class SetSMSConfirmationCodeString : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public string ConfirmationCode { get; set; }
    }
    
    public class ClearCachedCredentials : NamedOperation{}

    public class ClearAppointment : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
    }
    public class RestoreWalletFromBackup : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public string BackupsPath { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public string Passphrase { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    public string Salt { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 3)]
	    public string Nonce { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 4)]
	    public int Iterations { get; set; }
    }

    public class RestoreWalletNarballBackup : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public string sourcePath { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public string destinationPath { get; set; }
    }
    public class GetMiningRegistrationIpMode : NamedOperation{}

    public class IsBlockchainSynced : NamedOperation{}
    
    public class IsWalletSynced : NamedOperation{}
    public class GetCurrentOperatingMode : NamedOperation{}


    public class QueryTotalConnectedPeersCount : NamedOperation{}
    
    public class QueryPeerConnectionDetails : NamedOperation{}
    public class SyncBlockchain : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public bool Force { get; set; }
    }
    public class LoadWallet : NamedLongRunningOperation
    {
	    [PositionalArgument(ArgumentFlags.Optional, Position = 0, Description = "The passphrase. If omitted, it will be asked from standard input.")]
	    public string Passphrase { get; set; }
    }
    public class WalletExists : NamedOperation{}

    public class WriteAppSetting : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Optional, Position = 0, Description = "The setting name (e.g. 'Port')")]
	    public string Name { get; set; }
	    [PositionalArgument(ArgumentFlags.Optional, Position = 1, Description = "The setting value (e.g. 33888)")]
	    public string Value { get; set; }
    }
    public class Test : NamedOperation{}
    public class Ping : NamedOperation{}
    public class Shutdown : NamedOperation{}
    public class QueryBlockChainInfo : NamedOperation{}
    public class QueryBlockchainSynced : NamedOperation{}
    public class CreateStandardAccount : NamedLongRunningOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "A name for your account (e.g. 'MyNeuraliumAccount')")]
	    public string AccountName { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = @"The type of account
     * User = 1,
     * Server = 2"
	    )]
	    public int AccountType{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 2, DefaultValue = false, Description = "Begin the account publication process right after account is created (untested)")]
	    public bool PublishAccount { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 3, Description = @"Whether to encrypt the wallet or not
Here are a few words on passphrases which will be asked to you in a prompt. Passphrases are sent as a `Dictionary[int,string]`. 
Depending on your encryption options, you will use a different subset of indices. Here's a description of the various indices for the passphrases dictionary
   * index **0**: the *wallet* password (only used if *EncryptWallet=true*)
   * index **1**: the *transactions key* password, or the password for all keys if *EncryptKeysIndividually=false*
   * index **2**: the *messages key* password (only used if *EncryptKeysIndividually=true*)
   * index **3**: the *key change key* password (only used if *EncryptKeysIndividually=true*)
   * index **4**: the *super key* password (only used if *EncryptKeysIndividually=true*)
   * index **5**: the *validator signature key* password (only used if *EncryptKeysIndividually=true*)
   * index **6**: the *validator secret key* password (only used if *EncryptKeysIndividually=true*)
")]
	    public bool EncryptWallet{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 4, Description = "Whether to encrypt the keys")]
	    public bool EncryptKey{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 5, Description = "Whether to encrypt each kind of key with different passwords")]
	    public bool EncryptKeysIndividually{ get; set; }

    }

    public class CreateNextXmssKey : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "Key's index (0-6)")]
	    public byte ordinal { get; set; }
    }
    public class CreateNewWallet : NamedLongRunningOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "A name for your account (e.g. 'MyNeuraliumAccount')")]
	    public string AccountName { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = @"The type of account
   * User = 1,
   * Server = 2
")]
	    public int AccountType{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 2, Description = @"Whether to encrypt the wallet or not
Here are a few words on passphrases which will be asked to you in a prompt. Passphrases are sent as a `Dictionary[int,string]`. 
Depending on your encryption options, you will use a different subset of indices. Here's a description of the various indices for the passphrases dictionary
   * index **0**: the *wallet* password (only used if *EncryptWallet=true*)
   * index **1**: the *transactions key* password, or the password for all keys if *EncryptKeysIndividually=false*
   * index **2**: the *messages key* password (only used if *EncryptKeysIndividually=true*)
   * index **3**: the *key change key* password (only used if *EncryptKeysIndividually=true*)
   * index **4**: the *super key* password (only used if *EncryptKeysIndividually=true*)
   * index **5**: the *validator signature key* password (only used if *EncryptKeysIndividually=true*)
   * index **6**: the *validator secret key* password (only used if *EncryptKeysIndividually=true*)
")]
	    public bool EncryptWallet{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 3, Description = "Whether to encrypt the keys")]
	    public bool EncryptKey{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 4, Description = "Whether to encrypt each kind of key with different passwords")]
	    public bool EncryptKeysIndividually{ get; set; }

	    [PositionalArgument(ArgumentFlags.Optional, Position = 5, DefaultValue = false, Description = "Begin the account publication process right after account is created (untested)")]
	    public bool PublishAccount { get; set; }
    }
    public class QueryBlockBinaryTransactions : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId{ get; set; }
    }
    public class CanPublishAccount : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }
    public class PublishAccount : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }
    public class QueryElectionContext : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId{ get; set; }
    }
    public class StartMining : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Optional, Position = 0, DefaultValue = null, Description = "(Optional, leave default value to use your own account as the delegate) The account id to to send your rewards to.")]
	    public string DelegateAccountId{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Optional, Position = 1, DefaultValue = 0, Description = "(Optional, leave default to use the best available tier for you) The desired mining Tier (1,2,3 or 4)")]
	    public int Tier{ get; set; }
    }
    public class StopMining : NamedOperation{}
    public class IsMiningEnabled : NamedOperation{}
    public class IsMiningAllowed : NamedOperation{}
    public class QueryWalletSynced : NamedOperation{} 
    public class QueryBlock : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId{ get; set; }
    }
    public class QueryCompressedBlock : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int BlockId{ get; set; }
    }
    public class QueryWalletTransactionHistory : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }
    
    public class QueryWalletTransactionHistoryDetails : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public string TransactionId{ get; set; }
    }
    public class QueryWalletAccounts : NamedOperation{}
    public class QueryDefaultWalletAccountId : NamedOperation{}
    
    public class QueryDefaultWalletAccountCode : NamedOperation{}

    public class QueryWalletAccountDetails : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }

    public class QueryWalletAccountPresentationTransactionId : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }
    public class QueryWalletAccountAppointmentDetails : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }

    public class RequestAppointment : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = @"The appointment region (timezone) can be one of:
   * Occident = 1,
   * Central = 2,
   * Orient = 4 
")]
	    public int PreferredRegion{ get; set; }
    }

    public class ReadAppSetting : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The setting name (e.g. 'Port'). You can use '*' to read all settings.")]
	    public string Name{ get; set; }
    }

    public class ResetWalletIndex : NamedOperation{ }
    public class QueryAccountTotalNeuraliums : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account code, can be queried with and API command such as '" + nameof(ApiCommands.QueryDefaultWalletAccountCode) + "'")]
	    public string AccountCode{ get; set; }
    }
    public class SendNeuraliums : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0, Description = "The account id to send neuraliums to")]
	    public string TargetAccountId{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1, Description = "The amount of iums to send (e.g. 10.42 *iums*)")]
	    public decimal Amount{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 2, DefaultValue = 0.0, Description = "The tip (in iums) to give to the miners for your trasaction to be prioritized")]
	    public decimal Tip{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 3, DefaultValue = "A note that will be associated with the transaction.")]
	    public string Note{ get; set; }
    }
}