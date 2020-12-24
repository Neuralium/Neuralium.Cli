using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [Command(typeof(GenericOperation), ShortName = "r", LongName = "run", Description = "Operation")]
        Run,
        [HelpCommand(ShortName = "h", LongName = "help", Description = "Help")]
        Help,
        [Command(typeof(EnterWalletPassphrase), LongName = nameof(EnterWalletPassphrase), Description = "Wallet's passphrase")]
	    EnterWalletPassphrase,
		[Command(typeof(EnterKeyPassphrase), LongName = nameof(EnterKeyPassphrase), Description = "Key's passphrase")]
		EnterKeyPassphrase,
		[Command(typeof(WalletKeyFileCopied), LongName = nameof(WalletKeyFileCopied), Description = "Wallet's Key file is copied")]
		WalletKeyFileCopied,
		[Command(typeof(QuerySupportedChains), LongName = nameof(QuerySupportedChains), Description = "Query what chain we support (object)")]
		QuerySupportedChains,
		[Command(typeof(QueryBlockHeight), LongName = nameof(QueryBlockHeight), Description = "Query what is the current block height (long)")]
		QueryBlockHeight,
		[Command(typeof(QueryChainStatus), LongName = nameof(QueryChainStatus), Description = "Query the chain's status (object)")]
		QueryChainStatus,
		[Command(typeof(IsWalletLoaded), LongName = nameof(IsWalletLoaded), Description = "Is the wallet loaded? (bool)")]
		IsWalletLoaded,
		[Command(typeof(LoadWallet), LongName = nameof(LoadWallet), Description = "Loads the wallet [long running] (int or object)")]
		LoadWallet,
		[Command(typeof(WalletExists), LongName = nameof(WalletExists), Description = "Can the wallet be found? (bool)")]
		WalletExists,
		[Command(typeof(CompleteLongRunningEvent), LongName = nameof(CompleteLongRunningEvent), Description = "Wait for a long running event's completion (bool)")]
		CompleteLongRunningEvent,
		[Command(typeof(RenewLongRunningEvent), LongName = nameof(RenewLongRunningEvent), Description = "Renew a long running event (bool)")]
		RenewLongRunningEvent,
        [Command(typeof(Test), LongName = nameof(Test), Description = "Tests the server")]
		Test,
        [Command(typeof(Ping), LongName = nameof(Ping), Description = "Pings the server (bool)")]
        Ping,
		[Command(typeof(Shutdown), LongName = nameof(Shutdown), Description = "Send a shutdown request to the server (bool)")]
		Shutdown,
		[Command(typeof(QueryBlockChainInfo), LongName = nameof(QueryBlockChainInfo), Description = "Query the blockchain's info (object)")]
		QueryBlockChainInfo,
		[Command(typeof(CreateNewWallet), LongName = nameof(CreateNewWallet), Description = "Creates a new wallet [long-running] (int)")]
		CreateNewWallet,
		[Command(typeof(QueryBlockBinaryTransactions), LongName = nameof(QueryBlockBinaryTransactions), Description = "Query block's transaction in binary format (List<object>)")]
		QueryBlockBinaryTransactions,
		[Command(typeof(CanPublishAccount), LongName = nameof(CanPublishAccount), Description = "Can we publish our account? (bool)")]
		CanPublishAccount,
		[Command(typeof(PublishAccount), LongName = nameof(PublishAccount), Description = "Send the account publication transaction [long-running] (int)")]
		PublishAccount,
		[Command(typeof(QueryElectionContext), LongName = nameof(QueryElectionContext), Description = "Query a block's election context (object)")]
		QueryElectionContext,
		[Command(typeof(StartMining), LongName = nameof(StartMining), Description = "Starts the mining")]
		StartMining,
		[Command(typeof(StopMining), LongName = nameof(StopMining), Description = "Stops the mining")]
		StopMining,
		[Command(typeof(IsMiningEnabled), LongName = nameof(IsMiningEnabled), Description = "Are we currently mining? (bool)")]
		IsMiningEnabled,
		[Command(typeof(QueryBlock), LongName = nameof(QueryBlock), Description = "Query a block's details (string)")]
		QueryBlock,
		[Command(typeof(QueryCompressedBlock), LongName = nameof(QueryCompressedBlock), Description = "Query a block's compressed details (byte[])")]
		QueryCompressedBlock,
		[Command(typeof(QueryWalletTransactionHistory), LongName = nameof(QueryWalletTransactionHistory), Description = "Query the wallet's transactions history (List<object>)")]
		QueryWalletTransactionHistory,
		[Command(typeof(QueryWalletAccounts), LongName = nameof(QueryWalletAccounts), Description = "Query the wallet's accounts (List<object>)")]
		QueryWalletAccounts,
		[Command(typeof(PresentAccountPublicly), LongName = nameof(PresentAccountPublicly), Description = "Send the account presentation transaction [long-running] (int)")]
		PresentAccountPublicly,
		[Command(typeof(QueryAccountTotalNeuraliums), LongName = nameof(QueryAccountTotalNeuraliums), Description = "Query an account's total amount of Neuraliums (object)")]
		QueryAccountTotalNeuraliums,
		[Command(typeof(SendNeuraliums), LongName = nameof(SendNeuraliums), Description = "Send neuralium to another account [long-running] (int)")]
		SendNeuraliums,
		[Command(typeof(ExitCommand), LongName = "exit", Description = "Exits the shell")]
        Exit
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

	        foreach (var property in properties)
	        {
		        this.parameters.Add(property.GetValue(this).ToString());
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
    
    public class EnterWalletPassphrase : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int CorrelationId { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public int KeyCorrelationCode { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    public string Passphrase { get; set; }
    }
    
    public class EnterKeyPassphrase : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int CorrelationId { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public int KeyCorrelationCode { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    public string Passphrase { get; set; }
    }
    public class WalletKeyFileCopied : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int CorrelationId { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public int KeyCorrelationCode { get; set; }
    }
    public class QuerySupportedChains : NamedOperation{}
    public class QueryBlockHeight : NamedOperation{}
    public class QueryChainStatus : NamedOperation{}
    public class IsWalletLoaded : NamedOperation{}
    public class LoadWallet : NamedLongRunningOperation{}
    public class WalletExists : NamedOperation{}
    public class CompleteLongRunningEvent : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int CorrelationId { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public string Data { get; set; }
    }
    public class RenewLongRunningEvent : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public int CorrelationId { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    public int KeyCorrelationCode { get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    public string Passphrase { get; set; }
    }
    public class Test : NamedOperation{}
    public class Ping : NamedOperation{}
    public class Shutdown : NamedOperation{}
    public class QueryBlockChainInfo : NamedOperation{}
    public class CreateNewWallet : NamedLongRunningOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    public string AccountName { get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    int AccountType{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    bool EncryptWallet{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 3)]
	    bool EncryptKey{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 4)]
	    bool EncryptKeysIndividually{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 5, AllowEmpty = true)]
	    Dictionary<string, string> Passphrases{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 6)]
	    bool PublishAccount { get; set; }
    }
    public class QueryBlockBinaryTransactions : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    int BlockId{ get; set; }
    }
    public class CanPublishAccount : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    string AccountCode{ get; set; }
    }
    public class PublishAccount : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    string AccountCode{ get; set; }
    }
    public class QueryElectionContext : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    int BlockId{ get; set; }
    }
    public class StartMining : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    string DelegateAccountId{ get; set; }
	    
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    int Tier{ get; set; }
    }
    public class StopMining : NamedOperation{}
    public class IsMiningEnabled : NamedOperation{}
    public class QueryBlock : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    int BlockId{ get; set; }
    }
    public class QueryCompressedBlock : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    int BlockId{ get; set; }
    }
    public class QueryWalletTransactionHistory : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    string AccountCode{ get; set; }
    }
    public class QueryWalletAccounts : NamedOperation{}
    public class PresentAccountPublicly : NamedOperation{}
    public class QueryAccountTotalNeuraliums : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    string AccountCode{ get; set; }
    }
    public class SendNeuraliums : NamedOperation
    {
	    [PositionalArgument(ArgumentFlags.Required, Position = 0)]
	    string TargetAccountId{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 1)]
	    decimal Amount{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 2)]
	    decimal Tp{ get; set; }
	    [PositionalArgument(ArgumentFlags.Required, Position = 3)]
	    string note{ get; set; }
    }
}