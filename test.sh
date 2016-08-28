ps aux | grep [d]isque-server | awk '{print $2}' | xargs kill -9
nohup ./Server/disque/src/disque-server > /dev/null 2>&1 &

dotnet run
