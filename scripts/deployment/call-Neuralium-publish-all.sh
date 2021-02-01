cd ../../../../Neuralium/src/scripts/deployment/

./publish-all.sh ${@:1:5} cli "--no-incremental" "/p:PublishTrimmed=true /p:PublishSingleFile=true" "/p:PublishTrimmed=true" "/p:PublishTrimmed=true" 