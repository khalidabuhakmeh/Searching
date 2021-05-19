FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Searching.csproj", "./"]
RUN dotnet restore "Searching.csproj"
COPY . .
WORKDIR "/src/Searching"
RUN dotnet build "Searching.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Searching.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Searching.dll"]
