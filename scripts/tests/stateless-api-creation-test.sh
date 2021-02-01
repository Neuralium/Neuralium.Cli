#!/bin/bash

. ./cli-utils.sh 1
if (( $? != 0)); then
    echo "Could not import cli-utils.sh, you're in the wrong directory"
    exit 1
fi
cli Ping
cli CreateNewWallet "test" 1 false false false \0 false
cli run CreateNewWallet jparams='[{"AccountName":"account name","AccountType":1,"EncryptWallet":false,"EncryptKey":false,"EncryptKeysIndividually":false}]'
cli LoadWallet
cli IsWalletLoaded
cli QueryWalletAccounts
# ACCOUNT_CODE=$(jq -r '.accountCode' <<< $(trim_first_last_line "$RETURN_VALUE") )
ACCOUNT_CODE="TOTO"
cli CanPublishAccount
cli CanPublishAccount $ACCOUNT_CODE
cli PublishAccount $ACCOUNT_CODE