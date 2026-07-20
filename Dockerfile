FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EduBridge.csproj ./
RUN dotnet restore EduBridge.csproj

COPY . ./
RUN dotnet publish EduBridge.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish ./

RUN mkdir -p /app/wwwroot/uploads/chat \
    /app/wwwroot/uploads/parents \
    /app/wwwroot/uploads/teachers

ENTRYPOINT ["dotnet", "EduBridge.dll"]
