ps aux | grep [d]isque-server | awk '{print $2}' | xargs kill -9
nohup ./Server/disque/src/disque-server --maxclients 100000 > /dev/null 2>&1 &

sudo dotnet run -c release
