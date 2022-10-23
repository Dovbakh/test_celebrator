#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
#WORKDIR /app
#COPY . .
#RUN dotnet restore
#RUN dotnet publish -c Release -o out
#
#FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
#WORKDIR /app
#COPY --from=build /app/out .
#ENTRYPOINT ["dotnet", "celebrator.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["celebrator/celebrator.csproj", "celebrator/"]
RUN dotnet restore "celebrator/celebrator.csproj"
COPY . .
WORKDIR "/src/celebrator"
RUN dotnet build "celebrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "celebrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "celebrator.dll"]

wrong line