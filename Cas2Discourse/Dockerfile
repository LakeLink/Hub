#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Cas2Discourse/Cas2Discourse.csproj", "Cas2Discourse/"]
RUN dotnet restore "Cas2Discourse/Cas2Discourse.csproj"
COPY . .
WORKDIR "/src/Cas2Discourse"
RUN dotnet build "Cas2Discourse.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cas2Discourse.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cas2Discourse.dll"]