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

### 3. PostgreSQL Client Dependency
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

## What the Fixed Startup Script Does

1. **Database URL Parsing**: Correctly parses the Render PostgreSQL URL format:
   ```
   postgres://user:password@host:port/database
   ```

2. **Smart Connection Handling**:
   - For Render: Trusts that `DATABASE_URL` is valid (no timeout loops)
   - For Local: Uses .NET EF for connection testing

3. **Migration Management**:
   - Creates PostgreSQL migrations if none exist
   - Runs `dotnet ef database update`
   - Falls back to `ensure-created` if migrations fail

4. **Application Startup**: Starts the .NET application

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