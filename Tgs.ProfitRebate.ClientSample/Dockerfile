#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Tgs.ProfitRebate.ClientSample/Tgs.ProfitRebate.ClientSample.csproj", "Tgs.ProfitRebate.ClientSample/"]
RUN dotnet restore "Tgs.ProfitRebate.ClientSample/Tgs.ProfitRebate.ClientSample.csproj"
COPY . .
WORKDIR "/src/Tgs.ProfitRebate.ClientSample"
RUN dotnet build "Tgs.ProfitRebate.ClientSample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tgs.ProfitRebate.ClientSample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.ProfitRebate.ClientSample.dll"]