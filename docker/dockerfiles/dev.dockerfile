FROM mcr.microsoft.com/dotnet/sdk:9.0

ENV SIMFELL_CONFIG_DIR=/App/SimFell

WORKDIR /App

CMD ["dotnet", "run", "--project", "SimFell/SimFell.csproj"]