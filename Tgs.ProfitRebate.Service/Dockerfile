#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.ProfitRebate.Service/Tgs.ProfitRebate.Service.csproj", "Tgs.ProfitRebate.Service/"]
COPY ["Tgs.ProfitRebate.Core/Tgs.ProfitRebate.Core.csproj", "Tgs.ProfitRebate.Core/"]
COPY ["Tgs.ProfitRebate.Repository/Tgs.ProfitRebate.Repository.csproj", "Tgs.ProfitRebate.Repository/"]
RUN dotnet restore "Tgs.ProfitRebate.Service/Tgs.ProfitRebate.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.ProfitRebate.Service"
RUN dotnet build "Tgs.ProfitRebate.Service.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.ProfitRebate.Service.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.ProfitRebate.Service.dll"]