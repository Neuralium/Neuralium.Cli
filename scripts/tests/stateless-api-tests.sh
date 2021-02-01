#!/bin/bash

. ./cli-utils.sh 1
if (( $? != 0)); then
    echo "Could not import cli-utils.sh, you're in the wrong directory"
    exit 1
fi

check_expected_path "$0" "neuralium/Neuralium.Cli/src/scripts/tests"

cli Ping
cli WalletExists
cli IsWalletLoaded
cli LoadWallet
cli QueryWalletAccounts
ACCOUNT=$(jq -r '.accountCode' <<< $(trim_first_last_line "$RETURN_VALUE") )
cli QueryWalletTransactionHistory $ACCOUNT

# echo $?