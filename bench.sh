ps aux | grep [d]isque-server | awk '{print $2}' | xargs kill -9
nohup ./Server/disque/src/disque-server --maxclients 100000 > /dev/null 2>&1 &

# Project下に追加/削除する。
# <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
#    <DefineConstants>LOGIC_BENCH</DefineConstants>
#  </PropertyGroup>

sudo dotnet run -c release

