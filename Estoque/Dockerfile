# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Estoque/Estoque.csproj", "Estoque/"]
RUN dotnet restore "./Estoque/Estoque.csproj"
COPY . .
WORKDIR "/src/Estoque"
RUN dotnet build "./Estoque.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Estoque.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Estoque.dll"]