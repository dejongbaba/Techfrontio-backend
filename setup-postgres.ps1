# PowerShell script to set up PostgreSQL migrations
# This script removes SQL Server migrations and creates fresh PostgreSQL ones

Write-Host "Setting up PostgreSQL migrations..." -ForegroundColor Green

# Check if we're in the right directory
if (!(Test-Path "Course management.csproj")) {
    Write-Host "Error: Please run this script from the project root directory" -ForegroundColor Red
    exit 1
}

# Remove existing migrations completely
if (Test-Path "Migrations") {
    Write-Host "Removing existing SQL Server migrations..." -ForegroundColor Yellow
    Remove-Item "Migrations" -Recurse -Force
    Write-Host "SQL Server migrations removed." -ForegroundColor Green
}

# Clean and restore packages to ensure PostgreSQL provider is available
Write-Host "Cleaning and restoring packages..." -ForegroundColor Yellow
dotnet clean
dotnet restore

# Create fresh PostgreSQL migrations
Write-Host "Creating fresh PostgreSQL migrations..." -ForegroundColor Yellow
try {
    $result = dotnet ef migrations add InitialPostgreSQL 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "PostgreSQL migrations created successfully!" -ForegroundColor Green
        Write-Host "Migration output: $result" -ForegroundColor Cyan
    } else {
        Write-Host "Migration creation failed with output: $result" -ForegroundColor Red
        throw "Migration creation failed"
    }
} catch {
    Write-Host "Error creating migrations: $_" -ForegroundColor Red
    Write-Host "Please ensure you have the Npgsql.EntityFrameworkCore.PostgreSQL package installed." -ForegroundColor Yellow
    exit 1
}

# Test build
Write-Host "Testing build..." -ForegroundColor Yellow
$buildResult = dotnet build 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed: $buildResult" -ForegroundColor Red
    Write-Host "You may need to manually fix any remaining issues." -ForegroundColor Yellow
}

Write-Host "Setup complete! You can now run the application with PostgreSQL." -ForegroundColor Green
Write-Host "To test locally with Docker: docker-compose up" -ForegroundColor Cyan
Write-Host "To run locally: dotnet run" -ForegroundColor Cyan