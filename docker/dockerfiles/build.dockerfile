FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /App

CMD ["dotnet", "build", "SimFell/SimFell.csproj"]