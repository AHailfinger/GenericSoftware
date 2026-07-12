# Unter https://aka.ms/customizecontainer erfahren Sie, wie Sie Ihren Debugcontainer anpassen und wie Visual Studio dieses Dockerfile verwendet, um Ihre Images für ein schnelleres Debuggen zu erstellen.

# Diese Stufe wird verwendet, wenn sie von VS im Schnellmodus ausgeführt wird (Standardeinstellung für Debugkonfiguration).
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Zeitzone setzen
ENV TZ=Europe/Berlin
RUN apt-get update && apt-get install -y tzdata \
    && ln -fs /usr/share/zoneinfo/$TZ /etc/localtime \
    && echo $TZ > /etc/timezone \
    && DEBIAN_FRONTEND=noninteractive dpkg-reconfigure -f noninteractive tzdata

# Install Python
RUN apt-get update && \
    apt-get install -y python3-full python3-pip python3-venv python3-dev && \
    ln -s /usr/bin/python3 /usr/bin/python

# Install required Python packages

RUN apt-get install -y python3-paho-mqtt

#RUN python3 -m venv /opt/venv
#RUN /opt/venv/bin/pip install --no-cache-dir --upgrade paho-mqtt

# Set environment variable for pythonnet to find libpython
ENV PYTHONNET_PYDLL=/usr/lib/x86_64-linux-gnu/libpython3.11.so

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Diese Stufe wird zum Erstellen des Dienstprojekts verwendet.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EnergyAutomate/EnergyAutomate/EnergyAutomate.csproj", "EnergyAutomate/EnergyAutomate/"]
COPY ["EnergyAutomate.BlazorMonaco/EnergyAutomate.BlazorMonaco.csproj", "EnergyAutomate.BlazorMonaco/"]
COPY ["EnergyAutomate.Growatt/EnergyAutomate.Growatt.csproj", "EnergyAutomate.Growatt/"]
COPY ["EnergyAutomate.OpenMeteo/EnergyAutomate.OpenMeteo.csproj", "EnergyAutomate.OpenMeteo/"]
COPY ["EnergyAutomate.Tibber/EnergyAutomate.Tibber.csproj", "EnergyAutomate.Tibber/"]
COPY ["EnergyAutomate/EnergyAutomate.Client/EnergyAutomate.Client.csproj", "EnergyAutomate/EnergyAutomate.Client/"]
RUN dotnet restore "./EnergyAutomate/EnergyAutomate/EnergyAutomate.csproj"
COPY . .
WORKDIR "/src/EnergyAutomate/EnergyAutomate"
RUN dotnet build "./EnergyAutomate.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Diese Stufe wird verwendet, um das Dienstprojekt zu veröffentlichen, das in die letzte Phase kopiert werden soll.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EnergyAutomate.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Diese Stufe wird in der Produktion oder bei Ausführung von VS im regulären Modus verwendet (Standard, wenn die Debugkonfiguration nicht verwendet wird).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnergyAutomate.dll"]