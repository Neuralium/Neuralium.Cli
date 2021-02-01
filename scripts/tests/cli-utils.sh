#!/bin/bash

LOG_LEVEL=$1
. ../../../../Neuralium/src/scripts/bash-tools/utils.sh $LOG_LEVEL
if (( $? != 0)); then
    echo "Could not import utils.sh, you're in the wrong directory"
    exit 1
fi

cli()
{
    echo "${GREEN}Running command ${YELLOW}$@${RESET}..."
    DIR=$PWD
    cd ../../bin/Debug/net5.0/
    RETURN_VALUE=$(./neuraliumcli $@)
    echo "returned value is ${YELLOW}$RETURN_VALUE${RESET}, error code is $?"
    cd $DIR
}
cli_expect_true()
{
    cli $@
    if [ ! "$RETURN_VALUE" = "true" ]; then
        echo "${RED}Command ${YELLOW}$@${RED} returned \"$RETURN_VALUE\", expected \"true\", aborting..."
        exit 1
    fi
}

trim_first_last_line()
{
    echo $(sed -e '$ d' -e '1d' <<< $1)
}