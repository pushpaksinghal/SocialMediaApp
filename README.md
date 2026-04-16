# ConnectSphere Backend

## Overview

ConnectSphere backend is a microservices-based architecture built using .NET. Each service is independently deployable and communicates over HTTP.

### Services Included

* Auth Service (JWT Authentication)
* Post Service (Create & Manage Posts)
* Like Service (Like/Unlike functionality)

---

## Tech Stack

* .NET 8 Web API
* Entity Framework Core
* SQL Server / PostgreSQL (depending on setup)
* JWT Authentication
* Docker

---

## Project Structure

```
/src
  /AuthService
  /PostService
  /LikeService
```

---

## Prerequisites

* .NET SDK 8+
* Docker
* Git

---

## Running Locally

### 1. Clone Repository

```
git clone <your-backend-repo-url>
cd backend
```

### 2. Run Each Service

Navigate into each service folder:

```
cd AuthService
dotnet run
```

Repeat for PostService and LikeService.

---

## Environment Variables

Each service may require:

```
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=your_secret_key
DB_CONNECTION_STRING=your_connection_string
```

---

## Docker Setup

### Build Image

```
docker build -t auth-service .
```

### Run Container

```
docker run -p 8080:8080 auth-service
```

---

## Deployment (Render)

### Steps

1. Push code to GitHub
2. Go to Render Dashboard
3. Create **New Web Service**
4. Connect repository
5. Select Docker environment
6. Set port: `8080`

Repeat for each microservice.

---

## API Endpoints (Example)

### Auth Service

```
POST /api/auth/login
POST /api/auth/register
```

### Post Service

```
POST /api/posts
GET /api/posts
```

### Like Service

```
POST /api/likes
DELETE /api/likes/{id}
```

---

## CORS Configuration

Ensure CORS is enabled:

```
AllowAnyOrigin
AllowAnyHeader
AllowAnyMethod
```

---

## Notes

* All services must be running for full functionality
* Update service URLs when deploying
* Use environment variables in production

---

## Future Improvements

* API Gateway integration
* Centralized logging
* Service discovery
* CI/CD pipeline

---
