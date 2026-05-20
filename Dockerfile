FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["APPMVC/APPMVC.csproj", "APPMVC/"]
RUN dotnet restore "APPMVC/APPMVC.csproj"
COPY . .
WORKDIR "/src/APPMVC"
RUN dotnet publish "APPMVC.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p wwwroot/uploads
ENTRYPOINT ["dotnet", "APPMVC.dll"]
