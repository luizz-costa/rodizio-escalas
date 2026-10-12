# Imagem da API + telas (wwwroot) do sistema de escalas.
# Dois estágios: o SDK compila; a imagem final leva só o runtime.

# 1) Compilação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Primeiro só o .csproj: o restore fica em cache enquanto os pacotes não mudam.
COPY Rodizio.Api.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

# 2) Execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

# Usuário sem privilégios, que já vem na imagem do .NET 8.
USER app

# A API atende na porta 8080 dentro do container (padrão do .NET 8).
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rodizio.Api.dll"]
