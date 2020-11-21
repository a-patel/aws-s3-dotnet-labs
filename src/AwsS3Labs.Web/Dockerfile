#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["AWSS3Labs.Web/AWSS3Labs.Web.csproj", "AWSS3Labs.Web/"]
RUN dotnet restore "AWSS3Labs.Web/AWSS3Labs.Web.csproj"
COPY . .
WORKDIR "/src/AWSS3Labs.Web"
RUN dotnet build "AWSS3Labs.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AWSS3Labs.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AWSS3Labs.Web.dll"]