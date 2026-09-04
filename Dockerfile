FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet publish -c Release -o cmp



FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app

COPY --from=build /src/cmp .

ENTRYPOINT ["dotnet", "ActivityRag.dll"]



