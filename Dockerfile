FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

COPY app/bin/Release/netcoreapp3.1/publish/ app/
EXPOSE 80

ENTRYPOINT ["dotnet", "app/docker_validator_netcore.dll"]
