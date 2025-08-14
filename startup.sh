#!/bin/bash

# Function to wait for database to be ready
wait_for_db() {
    echo "Waiting for PostgreSQL database to be ready..."
    
    # Extract connection details from DATABASE_URL if available
    if [ ! -z "$DATABASE_URL" ]; then
        # Parse DATABASE_URL (format: postgres://user:password@host:port/database)
        DB_HOST=$(echo $DATABASE_URL | sed -n 's/.*@\([^:]*\):.*/\1/p')
        DB_PORT=$(echo $DATABASE_URL | sed -n 's/.*:\([0-9]*\)\/.*/\1/p')
        DB_USER=$(echo $DATABASE_URL | sed -n 's/.*:\/\/\([^:]*\):.*/\1/p')
        DB_NAME=$(echo $DATABASE_URL | sed -n 's/.*\/\([^?]*\).*/\1/p')
        
        # If port is not found, default to 5432
        if [ -z "$DB_PORT" ]; then
            DB_PORT=5432
        fi
    else
        # Fallback to default values
        DB_HOST=${DB_HOST:-localhost}
        DB_PORT=${DB_PORT:-5432}
        DB_USER=${DB_USER:-postgres}
        DB_NAME=${DB_NAME:-Techfrontio}
    fi
    
    echo "Checking connection to $DB_HOST:$DB_PORT..."
    
    # Wait for database to be ready (max 60 seconds)
    for i in {1..60}; do
        if pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" > /dev/null 2>&1; then
            echo "Database is ready!"
            return 0
        fi
        echo "Database not ready yet... waiting ($i/60)"
        sleep 1
    done
    
    echo "Database connection timeout after 60 seconds"
    return 1
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