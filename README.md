# Neuralium.Cli

##### Version Trial run III

The Neuralium crypto token console remote API 

## Usage instructions.

#### this is a very early release.

Can be used in query and interactive mode.  

#### Query mode

Run individual commands:

Neuralium.Cli query Ping

Neuralium.Cli query QueryBlock 108

Neuralium.Cli query QueryWalletAccounts

Neuralium.Cli query QueryAccountTotalNeuraliums "016d2570-7b0a-44dd-b410-bb479776d6c2"

Neuralium.Cli query StartMining

Neuralium.Cli query StopMining

Neuralium.Cli query Shutdown

#### Interactive mode

Neuralium.Cli interactive

(note: interactive mode not working yet).

## Build Instructions

##### First, ensure dotnet core 2.2 SDK is installed

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

