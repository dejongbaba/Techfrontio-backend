# Course Management API

A comprehensive course management system built with ASP.NET Core 6.0 and PostgreSQL.

## Features

- User authentication and authorization (JWT + Google OAuth)
- Role-based access control (Admin, Tutor, Student)
- Course management
- Enrollment system
- Payment processing
- Review and rating system
- RESTful API with Swagger documentation

## Prerequisites

- .NET 6.0 SDK
- Docker and Docker Compose
- PostgreSQL (for local development without Docker)

## Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd "Course management"
   ```

2. **Run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

3. **Access the application**
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger

## Production Deployment (Render)

### 1. Database Setup

1. Create a PostgreSQL database on Render
2. Copy the `DATABASE_URL` from your Render PostgreSQL dashboard

### 2. Application Deployment

1. **Environment Variables**
   Set the following environment variables in your Render service:
   ```
   DATABASE_URL=<your-render-postgresql-url>
   ASPNETCORE_ENVIRONMENT=Production
   JWT_ISSUER=YourIssuer
   JWT_AUDIENCE=YourAudience
   JWT_KEY=<your-secure-jwt-key>
   ```

2. **Deploy**
   - Connect your GitHub repository to Render
   - Set the build command: `docker build -t app .`
   - Set the start command: `docker run -p $PORT:80 app`

### 3. Database Migration

The application automatically runs database migrations on startup. The startup script:
1. Waits for database connection
2. Runs EF Core migrations
3. Seeds initial data
4. Starts the application

## Local Development

### With Docker
```bash
docker-compose up
```

### Without Docker

1. **Install PostgreSQL**
   - Install PostgreSQL locally
   - Create a database named `Techfrontio`

2. **Update Connection String**
   Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=Techfrontio;Username=postgres;Password=your_password;Port=5432"
     }
   }
   ```

3. **Run Migrations**
   ```bash
   dotnet ef database update
   ```

4. **Start the Application**
   ```bash
   dotnet run
   ```

## API Endpoints

### Authentication
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - User login
- `GET /api/users/google-login` - Google OAuth login

### Courses
- `GET /api/courses` - Get all courses
- `POST /api/courses` - Create course (Tutor/Admin)
- `GET /api/courses/{id}` - Get course details
- `PUT /api/courses/{id}` - Update course (Owner/Admin)
- `DELETE /api/courses/{id}` - Delete course (Owner/Admin)

### Enrollments
- `GET /api/enrollments/user` - Get user enrollments
- `POST /api/enrollments` - Enroll in course
- `DELETE /api/enrollments/{id}` - Unenroll from course

### Reviews
- `GET /api/reviews` - Get all reviews
- `POST /api/reviews` - Create review
- `PUT /api/reviews/{id}` - Update review (Owner/Admin)
- `DELETE /api/reviews/{id}` - Delete review (Owner/Admin)

### Admin
- `GET /api/admins/dashboard` - Admin dashboard
- `GET /api/admins/users` - Get all users
- `PUT /api/admins/users/{id}/role` - Update user role

## Default Users

The application seeds the following test users:

- **Admin**: admin@demo.com / Admin123!
- **Tutor**: tutor@demo.com / Tutor123!
- **Student**: student@demo.com / Student123!

## Health Check

The application includes a health check endpoint at `/health` for monitoring.

## Docker Configuration

The Dockerfile includes:
- PostgreSQL client tools
- Automatic database migration
- Health checks
- Proper startup sequence

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `DATABASE_URL` | PostgreSQL connection string | Yes |
| `ASPNETCORE_ENVIRONMENT` | Application environment | Yes |
| `JWT_ISSUER` | JWT token issuer | Yes |
| `JWT_AUDIENCE` | JWT token audience | Yes |
| `JWT_KEY` | JWT signing key (min 32 chars) | Yes |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID | No |
| `GOOGLE_CLIENT_SECRET` | Google OAuth client secret | No |

## Troubleshooting

### Database Connection Issues
1. Verify `DATABASE_URL` is correctly set
2. Check database server is running
3. Ensure firewall allows connections

### Migration Issues
1. Check database permissions
2. Verify connection string format
3. Review application logs

### Docker Issues
1. Ensure Docker is running
2. Check port availability (5432, 8080)
3. Review container logs: `docker-compose logs`

## License

MIT License - see LICENSE file for details.