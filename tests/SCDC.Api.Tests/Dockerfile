FROM mcr.microsoft.com/dotnet/sdk:10.0

# ACL integration tests exercise the same provisioner used by make init.
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace
