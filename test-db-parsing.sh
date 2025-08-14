#!/bin/bash

# Test script to verify DATABASE_URL parsing
echo "Testing DATABASE_URL parsing..."

# Test with actual Render PostgreSQL URL format (without port)
test_url="postgresql://admin:D9q0vVboLBOPw92uDpgk5lJvTfkbDcVD@dpg-d2emfps9c44c7394sb80-a/techfrontdb"

echo "Testing with URL: $test_url"
echo ""

# Set the test URL
export DATABASE_URL="$test_url"

# Extract connection details (same logic as startup.sh)
if [ ! -z "$DATABASE_URL" ]; then
    echo "Parsing DATABASE_URL: $DATABASE_URL"
    
    # Parse DATABASE_URL (format: postgres://user:password@host:port/database or postgres://user:password@host/database)
        # Remove protocol prefix (handle both postgres:// and postgresql://)
        URL_WITHOUT_PROTOCOL=$(echo $DATABASE_URL | sed 's/^postgres[ql]*:\/\///')
        
        # Extract user:password part
        USER_PASS=$(echo $URL_WITHOUT_PROTOCOL | cut -d'@' -f1)
        DB_USER=$(echo $USER_PASS | cut -d':' -f1)
        
        # Extract host:port/database part
        HOST_PORT_DB=$(echo $URL_WITHOUT_PROTOCOL | cut -d'@' -f2)
        
        # Check if port is specified (contains colon before slash)
        if echo "$HOST_PORT_DB" | grep -q ':[0-9]*/'; then
            # Port is specified: host:port/database
            DB_HOST=$(echo $HOST_PORT_DB | cut -d':' -f1)
            PORT_DB=$(echo $HOST_PORT_DB | cut -d':' -f2)
            DB_PORT=$(echo $PORT_DB | cut -d'/' -f1)
            DB_NAME=$(echo $PORT_DB | cut -d'/' -f2 | cut -d'?' -f1)
        else
            # No port specified: host/database
            DB_HOST=$(echo $HOST_PORT_DB | cut -d'/' -f1)
            DB_PORT=5432
            DB_NAME=$(echo $HOST_PORT_DB | cut -d'/' -f2 | cut -d'?' -f1)
        fi
    
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
echo "  Host: dpg-d2emfps9c44c7394sb80-a"
echo "  Port: 5432"
echo "  User: admin"
echo "  Database: techfrontdb"