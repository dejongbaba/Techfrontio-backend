#!/bin/bash

# Test script to verify DATABASE_URL parsing
echo "Testing DATABASE_URL parsing..."

# Test with a sample Render PostgreSQL URL format
test_url="postgresql://admin:D9q0vVboLBOPw92uDpgk5lJvTfkbDcVD@dpg-d2emfps9c44c7394sb80-a.oregon-postgres.render.com/techfrontdb"

echo "Testing with URL: $test_url"
echo ""

# Set the test URL
export DATABASE_URL="$test_url"

# Extract connection details (same logic as startup.sh)
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
    
    echo "Parsed connection details:"
    echo "  Host: $DB_HOST"
    echo "  Port: $DB_PORT"
    echo "  User: $DB_USER"
    echo "  Database: $DB_NAME"
    
    # Validate extracted values
    if [ -z "$DB_HOST" ] || [ -z "$DB_PORT" ] || [ -z "$DB_USER" ] || [ -z "$DB_NAME" ]; then
        echo "❌ FAILED: Some values are empty!"
        exit 1
    else
        echo "✅ SUCCESS: All values parsed correctly!"
    fi
else
    echo "❌ FAILED: DATABASE_URL is empty"
    exit 1
fi

echo ""
echo "Expected values:"
echo "  Host: dpg-abc123-a.oregon-postgres.render.com"
echo "  Port: 5432"
echo "  User: user"
echo "  Database: mydb_xyz"