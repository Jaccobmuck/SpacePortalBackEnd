# SpacePortalBackEnd
# SpacePortal Backend

This is the backend API for my **SpacePortal** capstone project.

SpacePortal is a space-weather and space-debris–oriented application that pulls in real-world data (solar flares, CMEs, geomagnetic storms, APOD images, etc.) from public APIs like NASA and related providers, stores it in a SQL Server database, and exposes it via a secure REST API for my frontend to consume.

The backend is built with **ASP.NET Core Web API**, **Entity Framework Core**, and **SQL Server**, and uses **JWT-based authentication** with role-based authorization.

---

## Table of Contents

- [Project Goals](#project-goals)
- [Tech Stack](#tech-stack)
- [Architecture Overview](#architecture-overview)
- [Key Features](#key-features)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Cloning the Repo](#cloning-the-repo)
  - [Configuration](#configuration)
  - [Entity Framework Migrations](#entity-framework-migrations)
  - [Running the API](#running-the-api)
- [API Overview](#api-overview)
  - [Auth Endpoints](#auth-endpoints)
  - [User & Profile Endpoints](#user--profile-endpoints)
  - [NASA / Space-Weather Endpoints](#nasa--space-weather-endpoints)
- [Error Handling & Logging](#error-handling--logging)
- [Security](#security)
- [Development Workflow](#development-workflow)
- [What I Learned](#what-i-learned)
- [Future Work](#future-work)

---

## Project Goals

For this backend, my goals were:

- Build a **real, production-style Web API**, not just a toy example.
- Consume external **space-weather APIs** (NASA DONKI, NASA APOD, and related endpoints), persist that data, and serve it to my frontend in a structured way.
- Implement **JWT authentication** with **role-based authorization** so that some features are public, some are for authenticated users, and some are admin-only.
- Use **Entity Framework Core** and **SQL Server** to model and store users, events, alerts, and other domain objects.
- Keep the architecture clean and organized so that adding new features (new data sources, new entities, etc.) is manageable.

---

## Tech Stack

**Core stack:**

- **.NET / ASP.NET Core Web API** (targeting .NET 8 or .NET 7 depending on the project settings)
- **C#**
- **Entity Framework Core** (Code-First with migrations)
- **SQL Server** (LocalDB / Developer Edition)
- **JWT Authentication** (Bearer tokens)
- **Swagger / Swashbuckle** for API documentation and manual testing

**External services / APIs (as implemented in this project):**

- **NASA APOD (Astronomy Picture of the Day)**  
  - Used for daily/archived astronomy imagery and metadata.
- **NASA DONKI / other space-weather APIs**  
  - Used for solar events (flares, CMEs, geomagnetic storms, etc.), when those controllers/services are wired up.

> Note: The exact set of NASA / space-weather endpoints available depends on which controllers/services I’ve implemented so far.

---

## Architecture Overview

This backend follows a fairly standard layered Web API structure:

- **Controllers**  
  - Define HTTP routes and shape responses.
  - Use dependency-injected services, DB contexts, and HTTP clients to perform work.
- **Domain Models / Entities (in `Models/`)**  
  - Represent tables in SQL Server (e.g., `User`, `Role`, `ApodImage`, and various space-weather event entities).
- **DTOs**  
  - Used to separate external API shapes and data transfer objects from internal EF entities.
  - Example: NASA APOD response DTOs vs. my `ApodImage` entity.
- **Data Access (DbContext)**  
  - `MyContext` (or similarly named) is my EF Core `DbContext`.
  - Exposes `DbSet<T>` properties for the main domain entities.
- **Services / Integration Logic**  
  - Classes that call NASA APIs using `IHttpClientFactory`, map the responses to DTOs/entities, and handle persistence.
- **Authentication & Authorization**  
  - JWT token generation and validation.
  - Role-based endpoints (e.g., admin-only import operations).

ASP.NET Core’s built-in **dependency injection** is used throughout to keep the code loosely coupled and testable.

---

## Key Features

- **JWT Authentication & Roles**
  - User registration and login.
  - JWT-based authentication.
  - Role-based checks for admin-only operations (such as import jobs).

- **NASA APOD Integration**
  - Import APOD data from NASA using an API key.
  - Store APOD metadata and URLs in SQL Server.
  - Expose endpoints to:
    - Import APOD data for a given date range or the latest date.
    - Retrieve APOD entries from the database for the frontend.

- **Space-Weather / DONKI Integration (as implemented)**
  - Endpoints to fetch and optionally store space-weather events (e.g., flares, CMEs, geomagnetic storms), depending on which controllers you’ve finished.
  - Designed so I can add more event types without rewriting the whole backend.

- **Persistent Storage with EF Core**
  - Database models for:
    - Users / roles.
    - APOD entries.
    - (Optionally) space-weather events, alerts, subscriptions, etc.
  - EF Core migrations to version my schema in a controlled way.

- **Swagger UI**
  - Automatically generated API docs.
  - Quick way to test endpoints during development.

---

## Project Structure

> This is a *conceptual* structure; exact folder names may differ slightly based on how I set up the project.

```text
SpacePortalBackEnd/
├─ Controllers/
│  ├─ AuthController.cs
│  ├─ UsersController.cs
│  ├─ ApodController.cs
│  ├─ ApodImportController.cs
│  └─ [Other NASA/space-weather controllers...]
│
├─ Models/
│  ├─ User.cs
│  ├─ Role.cs
│  ├─ ApodImage.cs
│  ├─ [Other event/entities...]
│  └─ MyContext.cs
│
├─ DTOs/
│  ├─ Auth/
│  │  ├─ LoginRequestDto.cs
│  │  ├─ RegisterRequestDto.cs
│  │  └─ AuthResponseDto.cs
│  ├─ Nasa/
│  │  ├─ ApodResponseDto.cs
│  │  └─ [Other NASA DTOs...]
│  └─ [Other DTO groupings...]
│
├─ Services/
│  ├─ NasaApodService.cs
│  ├─ [Other Nasa/space-weather services...]
│  └─ TokenService.cs
│
├─ appsettings.json
├─ appsettings.Development.json
├─ Program.cs
└─ README.md   ← (this file)
