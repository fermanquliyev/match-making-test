FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["MatchMaking.Service/MatchMaking.Service.csproj", "MatchMaking.Service/"]
COPY ["MatchMaking.ServiceDefaults/MatchMaking.ServiceDefaults.csproj", "MatchMaking.ServiceDefaults/"]
COPY ["MatchMaking.Application/MatchMaking.Application.csproj", "MatchMaking.Application/"]
COPY ["MatchMaking.Infrastructure/MatchMaking.Infrastructure.csproj", "MatchMaking.Infrastructure/"]
COPY ["MatchMaking.Domain/MatchMaking.Domain.csproj", "MatchMaking.Domain/"]
COPY ["MatchMaking.Contracts/MatchMaking.Contracts.csproj", "MatchMaking.Contracts/"]
RUN dotnet restore "MatchMaking.Service/MatchMaking.Service.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "MatchMaking.Service/MatchMaking.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MatchMaking.Service/MatchMaking.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MatchMaking.Service.dll"]
