# neuraliumcli

##### Version Release Candidate VI

The Neuralium crypto token console remote API.

## Usage
The Neuralium Cli's documentation can be obtained from the `help` command

```
$ neuraliumcli help
Usage:
    <Command$

Required Parameters:
    <Command$
        CanPublishAccount - Can we publish our account? (bool)
        CompleteLongRunningEvent - Wait for a long running event's completion (bool)
        CreateNewWallet - Creates a new wallet [long-running] (int)
        EnterKeyPassphrase - Key's passphrase
        EnterWalletPassphrase - Wallet's passphrase
        exit - Exits the shell
        help - Help [Alias: h]
        IsMiningEnabled - Are we currently mining? (bool)
        IsWalletLoaded - Is the wallet loaded? (bool)
        LoadWallet - Loads the wallet [long running] (int or object)
        Ping - Pings the server (bool)
        PresentAccountPublicly - Send the account presentation transaction [long-running] (int)
        PublishAccount - Send the account publication transaction [long-running] (int)
        QueryAccountTotalNeuraliums - Query an account's total amount of Neuraliums (object)
        QueryBlock - Query a block's details (string)
        QueryBlockBinaryTransactions - Query block's transaction in binary format (List<object$)
        QueryBlockChainInfo - Query the blockchain's info (object)
        QueryBlockHeight - Query what is the current block height (long)
        QueryChainStatus - Query the chain's status (object)
        QueryCompressedBlock - Query a block's compressed details (byte[])
        QueryElectionContext - Query a block's election context (object)
        QuerySupportedChains - Query what chain we support (object)
        QueryWalletAccounts - Query the wallet's accounts (List<object$)
        QueryWalletTransactionHistory - Query the wallet's transactions history (List<object$)
        RenewLongRunningEvent - Renew a long running event (bool)
        run - Operation [Alias: r]
        SendNeuraliums - Send neuralium to another account [long-running] (int)
        Shutdown - Send a shutdown request to the server (bool)
        StartMining - Starts the mining
        StopMining - Stops the mining
        Test - Tests the server
        WalletExists - Can the wallet be found? (bool)
        WalletKeyFileCopied - Wallet's Key file is copied

```
## Usage instructions.

There are two modes of operation, the interactive mode and the stateless mode. To enter in the interractive mode, simply do

```
$ neuraliumcli
> [you are now in terrective mode, you can enter any command, including 'help']
```

otherwise you can use the "stateless mode"

```
$ neuraliumcli help
[...]
```

## Examples
The following examples use the `stateless mode` but the sytax is the same for the `interractive mode`, ommitting the `neuraliumcli` of course.

### Json parameters

*$ neuraliumcli SendNeuraliums --jparams "[{\"Name\":\"targetAccountId\", \"Value\":\"{SF3}\"},{\"Name\":\"amount\", \"Value\":\"1.1\"}]"*

### Named Parameters List

*$ neuraliumcli SendNeuraliums --params "targetAccountId={SF3};amount=1.1;tip=0.001"*

### Ordered Parameters List

*$ neuraliumcli SendNeuraliums --params "{SF3};1.1;0.001"*


### Example commands

Run individual commands:

*$ neuraliumcli Ping*

*$ neuraliumcli QueryBlockChainInfo*

*$ neuraliumcli IsWalletLoaded*

*$ neuraliumcli WalletExists*

*$ neuraliumcli LoadWallet*

*$ neuraliumcli CreateNewWallet --params "accountName=account name;accountType=1;encryptWallet=false;encryptKey=false;encryptKeysIndividually=false"*

*$ neuraliumcli CreateNewWallet --params "accountName=account name;accountType=1;encryptWallet=true;encryptKey=true;encryptKeysIndividually=true;passphrases={\"0\":\"pass1\",\"1\":\"pass2\"}"*

*$ neuraliumcli QueryBlock 108*

*$ neuraliumcli QueryWalletAccounts*

*$ neuraliumcli QueryDefaultWalletAccountId*

*$ neuraliumcli QueryDefaultWalletAccountUuid*

*$ neuraliumcli QueryAccountTotalNeuraliums --params "accountUuid=016d2570-7b0a-44dd-b410-bb479776d6c2"*

*$ neuraliumcli PublishAccount --params "accountUuid=016d4f5a-c134-4141-8e35-3278b9baed67"*

*$ neuraliumcli SendNeuraliums --params "targetAccountId={SF3};amount=1.1;tip=0.001;note\"some note\""*

*$ neuraliumcli StartMining*

*$ neuraliumcli StopMining*

*$ neuraliumcli Shutdown*

## Build Instructions

#### First, ensure dotnet core 3.1 SDK is installed

#### The first step is to ensure that the dependencies have been built and copied into the nuget-source folder.

##### the source code to the below dependencies can be found here: [Neuralia Technologies source code](https://github.com/Neuralia) 

 - Neuralia.Blockchains.Tools
 - Neuralia.NClap
 - Neuralia.Open.Nat
 - Neuralia.BouncyCastle
 - Neuralia.Blockchains.Core

Then, simply invoke the right build file for your needs
$cd targets
$ ./linux.sh
this will produce the executable in the folder /build

