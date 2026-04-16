# ConnectSphere Backend

## Introduction

The ConnectSphere backend is designed using a microservices architecture to ensure scalability, modularity, and independent deployment of core features. Instead of building a monolithic system, each domain concern such as authentication, post management, and user interactions is separated into its own service.

This approach allows each service to evolve independently, improves maintainability, and reflects real-world production system design patterns.

---

## Architecture Overview

The backend consists of multiple independent services:

* **Auth Service** – Handles user authentication and authorization using JWT
* **Post Service** – Manages post creation, retrieval, and storage
* **Like Service** – Handles user interactions such as liking/unliking posts

Each service:

* Runs independently
* Has its own API endpoints
* Communicates over HTTP
* Can be deployed separately

---

## Technology Stack

The backend is built using modern .NET technologies:

* **.NET 8 Web API** – Core framework for building REST APIs
* **Entity Framework Core** – ORM for database interaction
* **JWT Authentication** – Secure user authentication
* **Docker** – Containerization for consistent deployment
* **REST Architecture** – Standard communication between services

---

## Service Breakdown

### 1. Auth Service

The Auth Service is responsible for managing user identity and security.

#### Responsibilities:

* User registration and login
* Google OAuth integration (if configured)
* JWT token generation
* Token validation for protected endpoints

#### Key Concepts:

* Stateless authentication using JWT
* Secure token-based communication
* Integration with frontend via Authorization headers

---

### 2. Post Service

The Post Service manages all content-related operations.

#### Responsibilities:

* Creating posts
* Fetching posts
* Managing post data storage

#### Key Concepts:

* Separation of business logic from authentication
* Independent scaling (high-read systems)
* RESTful API design

---

### 3. Like Service

The Like Service handles user engagement features.

#### Responsibilities:

* Like a post
* Unlike a post
* Track user interactions

#### Key Concepts:

* Lightweight service with focused responsibility
* Designed for high-frequency operations
* Can be optimized independently

---

## Communication Between Services

Currently, services communicate via HTTP APIs. Each service exposes endpoints which can be consumed by the frontend or other services if needed.

Future improvements may include:

* API Gateway (single entry point)
* Service-to-service authentication
* Message queues for async communication

---

## Local Development

Each microservice can be run independently using:

```bash
dotnet run
```

This allows developers to test services in isolation or together.

---

## Containerization

Docker is used to ensure consistency across environments.

Each service includes a Dockerfile which:

* Builds the application
* Exposes a fixed port (e.g., 8080)
* Runs the service inside a container

This makes deployment platform-independent.

---

## Deployment Strategy

The backend is deployed using **Render**, where:

* Each microservice is deployed as an individual web service
* Docker is used as the runtime environment
* Each service gets its own public URL

Example:

* Auth Service → https://auth-service.onrender.com
* Post Service → https://post-service.onrender.com
* Like Service → https://like-service.onrender.com

---

## Security Considerations

* JWT tokens are used for authentication
* Sensitive configuration is stored in environment variables
* CORS is configured to allow frontend communication

---

## Design Principles Followed

* **Single Responsibility Principle** – Each service has one job
* **Loose Coupling** – Services are independent
* **Scalability** – Services can scale individually
* **Maintainability** – Easier to debug and extend

---

## Future Enhancements

* API Gateway (YARP / Azure APIM)
* Centralized logging (Serilog + Seq)
* Distributed tracing
* CI/CD pipelines
* Database per service (if not already separated)

---

## Conclusion

This backend demonstrates a real-world microservices approach using .NET, focusing on clean separation of concerns, scalability, and modern deployment practices. It provides a strong foundation for building production-grade distributed systems.

---
