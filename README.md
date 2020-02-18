# neuraliumcli

##### Version Release Candidate III

The Neuralium crypto token console remote API 

## Usage instructions.

#### note: this is a very early release.

Can be used in run and interactive mode.  

#### Run mode

Run mode allows to run single stateless commands.

##### Parameter passing

Parameters can be passed in various ways. 

##### Target IP and Port

*> neuraliumcli run -h "127.0.0.1" -p 12033 -o Ping

###### Json file

*> neuraliumcli run --config=/home/user/config.json*

config.json:

> {
"Operation" : "SendNeuraliums",
"Parameters" : [{"Name":"targetAccountId", "Value":"{SF3}"},
		{"Name":"amount", "Value":"1.1"},
		{"Name":"tip", "Value":"0.001"},
		{"Name":"note", "Value":""}]
}

###### Json parameters

*> neuraliumcli run -o SendNeuraliums --jparams "[{\"Name\":\"targetAccountId\", \"Value\":\"{SF3}\"},{\"Name\":\"amount\", \"Value\":\"1.1\"}]"*

###### Named Parameters List

*> neuraliumcli run -o SendNeuraliums --params "targetAccountId={SF3};amount=1.1;tip=0.001"*

###### Ordered Parameters List

*> neuraliumcli run -o SendNeuraliums --params "{SF3};1.1;0.001"*


##### Example commands

Run individual commands:

*> neuraliumcli run -o Ping*

*> neuraliumcli run -o QueryBlockChainInfo*

*> neuraliumcli run -o IsWalletLoaded*

*> neuraliumcli run -o WalletExists*

*> neuraliumcli run -o LoadWallet*

*> neuraliumcli run -o CreateNewWallet --params "accountName=account name;encryptWallet=false;encryptKey=false;encryptKeysIndividually=false"*

*> neuraliumcli run -o CreateNewWallet --params "accountName=account name;encryptWallet=true;encryptKey=true;encryptKeysIndividually=true;passphrases={\"0\":\"pass1\",\"1\":\"pass2\"}"*

*> neuraliumcli run -o QueryBlock 108*

*> neuraliumcli run -o QueryWalletAccounts*

*> neuraliumcli run -o QueryDefaultWalletAccountId*

*> neuraliumcli run -o QueryDefaultWalletAccountUuid*

*> neuraliumcli run -o QueryAccountTotalNeuraliums --params "accountUuid=016d2570-7b0a-44dd-b410-bb479776d6c2"*

*> neuraliumcli run -o PublishAccount --params "accountUuid=016d4f5a-c134-4141-8e35-3278b9baed67"*

*> neuraliumcli run -o SendNeuraliums --params "targetAccountId={SF3};amount=1.1;tip=0.001;note\"some note\""*

*> neuraliumcli run -o StartMining*

*> neuraliumcli run -o StopMining*

*> neuraliumcli run -o Shutdown*

#### Interactive mode

neuraliumcli interactive

(note: interactive mode not working yet).

## Build Instructions

##### First, ensure dotnet core 3.1 SDK is installed

#### The first step is to ensure that the dependencies have been built and copied into the nuget-source folder.

##### the source code to the below dependencies can be found here: [Neuralia Technologies source code](https://github.com/Neuralia) 

 - Neuralia.Blockchains.Tools
 - Neuralia.Data.HashFunction.xxHash
 - Neuralia.STUN
 - Neuralia.BouncyCastle
 - Neuralia.Blockchains.Core

Then, simply invoke the right build file for your needs
>cd targets
> ./linux.sh
this will produce the executable in the folder /build

