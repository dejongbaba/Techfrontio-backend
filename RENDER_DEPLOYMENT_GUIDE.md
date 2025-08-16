# Render Deployment Guide

## Issues Fixed

The deployment issues have been resolved by fixing the following problems:

### 1. .NET SDK and EF Tools Missing
**Problem**: The application was failing with "The application 'ef' does not exist" and "No .NET SDKs were found" because the runtime container didn't have the .NET SDK or Entity Framework tools.

**Solution**: 
- Changed the final Docker stage from `aspnet:6.0` (runtime-only) to `sdk:6.0` (includes SDK)
- Installed Entity Framework tools in the final container
- Ensured proper PATH configuration for dotnet tools

### 2. DATABASE_URL Parsing Issue
**Problem**: The startup script was failing to parse Render's `DATABASE_URL` format correctly, especially URLs without explicit port numbers (e.g., `postgresql://user:pass@host/db` vs `postgres://user:pass@host:5432/db`).

**Solution**: Updated `startup.sh` with improved parsing logic that:
- Handles both `postgres://` and `postgresql://` prefixes
- Correctly parses URLs with and without explicit port numbers
- Defaults to port 5432 when not specified
- Properly extracts host, port, username, and database name

### 3. Migration Approach Simplified
**Problem**: Using shell scripts for database migrations was complex and error-prone, requiring EF CLI tools and source files in the container.

**Root Cause**: The startup script approach required additional dependencies and complexity in the Docker container.

**Solution**:
- Moved database migration logic directly into Program.cs using `context.Database.MigrateAsync()`
- Removed dependency on EF CLI tools and startup scripts
- Simplified Dockerfile by using runtime image instead of SDK
- Added comprehensive logging for migration process
- Automatic error handling and database creation

### 4. Improved DATABASE_URL Parsing
**Problem**: The original parsing logic couldn't handle Render's PostgreSQL URL format correctly, especially URLs without explicit port numbers.

**Root Cause**: The parsing logic assumed all URLs would have explicit ports and didn't handle the database name extraction properly.

**Solution**:
- Updated parsing to handle URLs with and without ports
- Improved database name extraction from the URL path
- Added comprehensive validation and logging

### 5. Port Binding Issue
**Problem**: The application was not binding to the correct host and port for Render, causing "No open ports detected" errors.

**Root Cause**: The application was using default Kestrel configuration which doesn't bind to 0.0.0.0 and the PORT environment variable that Render expects.

**Solution**:
- Configured Kestrel to listen on 0.0.0.0 and use the PORT environment variable (defaults to 10000)
- Removed HTTPS redirection since Render handles SSL termination
- Application now properly binds to the port Render expects

### 6. PostgreSQL Client Dependency
**Problem**: The script was using `pg_isready` which required PostgreSQL client tools and was causing connection timeouts.

**Solution**: 
- Removed `pg_isready` dependency
- For Render deployments, we now trust that `DATABASE_URL` is valid when provided
- For local development, we use .NET Entity Framework for connection testing

## Deployment Steps for Render

### 1. Environment Variables
Ensure these environment variables are set in your Render service:

```
DATABASE_URL=<your-render-postgresql-url>
ASPNETCORE_ENVIRONMENT=Production
JWT_SECRET=<your-jwt-secret>
```

### 2. Build Command
```bash
docker build -t course-management-app .
```

### 3. Start Command
```bash
./startup.sh
```

## What the Updated Application Does

1. **Automatic Database Migration**: The application now handles database migrations automatically during startup in Program.cs
2. **Proper Logging**: Comprehensive logging for migration process with detailed error handling
3. **Simplified Deployment**: No longer requires startup scripts or EF CLI tools in the container
4. **Database Creation**: Automatically ensures the database exists and applies any pending migrations
5. **Data Seeding**: Seeds the database with test data after successful migration
6. **Error Handling**: Proper exception handling with detailed logging for troubleshooting

## Troubleshooting

### If deployment still fails:

1. **Check Environment Variables**:
   - Verify `DATABASE_URL` is set correctly in Render dashboard
   - Ensure PostgreSQL service is running and connected

2. **Check Logs**:
   - Look for "Parsing DATABASE_URL" messages
   - Verify parsed connection details are correct

3. **Database Service**:
   - Ensure your Render PostgreSQL service is in the same region
   - Check that the database service is not suspended

4. **Port Configuration**:
   - Render expects your app to listen on the port specified by `PORT` environment variable
   - The app should bind to `0.0.0.0:$PORT` not `localhost`

5. **Migration Issues**: Check application logs for database migration errors during startup

6. **Connection String Issues**:
   - **Problem**: `Format of the initialization string does not conform to specification starting at index 0` or `Invalid port: -1`
   - **Cause**: DATABASE_URL environment variable is empty, null, malformed, or missing port specification
   - **Solution**: 
     - Verify DATABASE_URL is set in Render environment variables
     - Check that the PostgreSQL URL format is correct: `postgresql://username:password@host:port/database`
     - Note: If port is omitted from the URL, the application will default to PostgreSQL port 5432
     - Review application logs for connection string debugging information and URL parsing details
     - Ensure the ConvertDatabaseUrl helper method properly parses the URL components
     - Check for proper URL encoding of special characters in username/password

7. **Port Binding Issues**:
   - Ensure the application binds to `0.0.0.0` and uses the `PORT` environment variable
   - Remove HTTPS redirection for Render deployments
   - Check that the application listens on the correct port (default 10000)

8. **Docker Configuration**:
   - Verify the application starts correctly with `dotnet "Course management.dll"`
   - Check that migrations are included in the published application
   - Ensure DATABASE_URL environment variable is properly set in Render
   - Review connection string parsing and validation logs

### Common Render PostgreSQL URL Format:
```
postgres://username:password@dpg-xxxxx-a.region-postgres.render.com:5432/database_name
```

## Testing Locally

To test the DATABASE_URL parsing locally:

1. Run the test script:
   ```bash
   bash test-db-parsing.sh
   ```

2. Set up local PostgreSQL and test:
   ```bash
   export DATABASE_URL="postgres://postgres:password@localhost:5432/Techfrontio"
   ./startup.sh
   ```

## Next Steps

After deploying with these fixes:

1. The application should start without the 60-second timeout
2. Database migrations will run automatically
3. The application will be available on your Render URL
4. Health checks will ensure the service stays running

The deployment should now complete successfully without the database connection timeout issues.