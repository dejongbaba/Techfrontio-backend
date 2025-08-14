#!/bin/bash

# Function to wait for database to be ready
wait_for_db() {
    echo "Waiting for PostgreSQL database to be ready..."
    
    # Extract connection details from DATABASE_URL if available
    if [ ! -z "$DATABASE_URL" ]; then
        echo "Parsing DATABASE_URL: $DATABASE_URL"
        
        # Parse DATABASE_URL (format: postgres://user:password@host:port/database)
        # Remove protocol prefix
        URL_WITHOUT_PROTOCOL=$(echo $DATABASE_URL | sed 's/^postgres:\/\///')
        
        # Extract user:password part
        USER_PASS=$(echo $URL_WITHOUT_PROTOCOL | cut -d'@' -f1)
        DB_USER=$(echo $USER_PASS | cut -d':' -f1)
        
        # Extract host:port/database part
        HOST_PORT_DB=$(echo $URL_WITHOUT_PROTOCOL | cut -d'@' -f2)
        
        # Extract host
        DB_HOST=$(echo $HOST_PORT_DB | cut -d':' -f1)
        
        # Extract port and database
        PORT_DB=$(echo $HOST_PORT_DB | cut -d':' -f2)
        DB_PORT=$(echo $PORT_DB | cut -d'/' -f1)
        DB_NAME=$(echo $PORT_DB | cut -d'/' -f2 | cut -d'?' -f1)
        
        # Validate extracted values
        if [ -z "$DB_HOST" ] || [ -z "$DB_PORT" ] || [ -z "$DB_USER" ] || [ -z "$DB_NAME" ]; then
            echo "Failed to parse DATABASE_URL properly. Using defaults."
            DB_HOST=localhost
            DB_PORT=5432
            DB_USER=postgres
            DB_NAME=Techfrontio
        fi
        
        echo "Parsed connection details: Host=$DB_HOST, Port=$DB_PORT, User=$DB_USER, Database=$DB_NAME"
    else
        # Fallback to default values
        DB_HOST=${DB_HOST:-localhost}
        DB_PORT=${DB_PORT:-5432}
        DB_USER=${DB_USER:-postgres}
        DB_NAME=${DB_NAME:-Techfrontio}
        echo "Using default connection details: Host=$DB_HOST, Port=$DB_PORT, User=$DB_USER, Database=$DB_NAME"
    fi
    
    echo "Checking connection to $DB_HOST:$DB_PORT..."
    
    # For Render deployment, we trust that DATABASE_URL is valid when provided
    if [ ! -z "$DATABASE_URL" ]; then
        echo "DATABASE_URL provided. Assuming database is managed by Render."
        return 0
    fi
    
    # For local development, do a simple connection test
    echo "Testing local database connection..."
    
    # Try to connect using .NET Entity Framework (more reliable than pg_isready)
    if dotnet ef database ensure-created --dry-run > /dev/null 2>&1; then
        echo "Database connection successful!"
        return 0
    else
        echo "Database connection failed. Please ensure PostgreSQL is running locally."
        return 1
    fi
}

# Function to run database migrations
run_migrations() {
    echo "Setting up database schema..."
    
    # Set environment for production
    export ASPNETCORE_ENVIRONMENT=Production
    
    # Create fresh PostgreSQL migrations if none exist
    if [ ! -d "Migrations" ]; then
        echo "Creating PostgreSQL migrations..."
        if ! dotnet ef migrations add InitialPostgreSQL; then
            echo "Failed to create migrations. Exiting."
            return 1
        fi
    fi

    echo "Running database migrations..."
    if ! dotnet ef database update; then
        echo "Migration failed. Trying to ensure database exists..."
        if ! dotnet ef database ensure-created; then
            echo "Database creation failed. Exiting."
            return 1
        fi
    fi
    
    echo "Database setup completed successfully"
    return 0
}

# Main startup sequence
echo "Starting Course Management Application..."

# Wait for database
if ! wait_for_db; then
    echo "Failed to connect to database. Exiting."
    exit 1
fi

# Run migrations
if ! run_migrations; then
    echo "Database migration failed. Exiting."
    exit 1
fi

# Start the application
echo "Starting the application..."
exec dotnet "Course management.dll"