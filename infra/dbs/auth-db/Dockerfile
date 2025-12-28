FROM postgres:17-bookworm

# Install .NET SDK for EF Core migrations
RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    && wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet-install.sh \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install EF Core tools
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef

# Copy entrypoint wrapper
COPY entrypoint.sh /usr/local/bin/entrypoint-wrapper.sh
RUN chmod +x /usr/local/bin/entrypoint-wrapper.sh

ENTRYPOINT ["/usr/local/bin/entrypoint-wrapper.sh"]
CMD ["postgres"]
