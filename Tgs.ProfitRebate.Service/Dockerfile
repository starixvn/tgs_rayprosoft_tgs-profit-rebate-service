#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
RUN --mount=type=secret,id=nuget,dst=/pat cp /pat ~/.nuget/NuGet/NuGet.Config
RUN dotnet nuget remove source 'Microsoft Visual Studio Offline Packages' || true
WORKDIR /src
COPY ["Tgs.ProfitRebate.Service/Tgs.ProfitRebate.Service.csproj", "Tgs.ProfitRebate.Service/"]
COPY ["Tgs.ProfitRebate.ApiClientLogger/Tgs.ProfitRebate.ApiClientLogger.csproj", "Tgs.ProfitRebate.ApiClientLogger/"]
COPY ["Tgs.ProfitRebate.Core/Tgs.ProfitRebate.Core.csproj", "Tgs.ProfitRebate.Core/"]
COPY ["Tgs.ProfitRebate.Repository/Tgs.ProfitRebate.Repository.csproj", "Tgs.ProfitRebate.Repository/"]
RUN dotnet restore "Tgs.ProfitRebate.Service/Tgs.ProfitRebate.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.ProfitRebate.Service"
RUN dotnet build "Tgs.ProfitRebate.Service.csproj" -c ${BuildConfiguration} -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.ProfitRebate.Service.csproj" -c ${BuildConfiguration} -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.ProfitRebate.Service.dll"]