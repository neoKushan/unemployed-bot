# --- Build Stage ---
# Use the official .NET SDK image matching your project's TargetFramework
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Used to embed Git commit data
ARG GIT_COMMIT=unknown 

# Copy the solution file and all project files it references (assuming standard layout)
# Copy .sln first
COPY Omsirp.slnx .
# Copy the project file(s) - adjust the pattern if your project is in a subdirectory
COPY src/*.csproj ./src/ 
COPY test/*.csproj ./test/ 
# If your project is in a subdirectory (e.g., src/YourProject), you might need:
# COPY src/YourProject/*.csproj ./src/YourProject/ 

# Restore dependencies using the solution file
RUN dotnet restore Omsirp.slnx

# Copy the rest of the application code
# This assumes your source code is at the root alongside the .sln
# Adjust if your code structure is different (e.g., COPY src/ ./src/)
COPY . .
COPY src/ ./src/
COPY test/ ./test/

# Build and publish the application using the solution file
# Configure for Release and specify the output directory
# This assumes 'YourProjectName' is the main executable project within the solution.
# If the project name differs from the DLL name needed for ENTRYPOINT, adjust accordingly.
# Often, you might need to specify the project within the solution to publish:
# RUN dotnet publish Omsirp.sln -c Release -o /app --no-restore /p:Project=YourProjectName/YourProjectName.csproj
# However, if the solution only contains one publishable project, this might suffice:
RUN dotnet publish src/Omsirp.csproj -c Release -o /app /p:SourceRevisionId=${GIT_COMMIT}

# --- Final Stage ---
# Use the official .NET runtime image, smaller than the SDK
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app .

# Define the entry point for the container
# IMPORTANT: Replace 'YourProjectName.dll' with the actual name of your compiled DLL
# This should be the output DLL of the main executable project within your solution.
ENTRYPOINT ["dotnet", "Omsirp.dll"] 
# Example: If your main project is MyDiscordBot.csproj, this would likely be MyDiscordBot.dll
