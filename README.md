# Payroll Backend System

A backend service for processing clinician payroll, ingesting payment data, and generating structured pay statements.
Built with a focus on **clean architecture, idempotent data ingestion, and real-world backend design patterns**.

---

## Tech Stack

* **ASP.NET Core (C#)**
* **PostgreSQL**
* **Entity Framework Core**
* **Docker & Docker Compose**
* **JWT Authentication**
* **Hangfire (Background Jobs)**

---

## Key Features

### Idempotent CSV Ingestion

* Safely processes payment data from CSV files
* Prevents duplicate imports using fingerprinting
* Tracks import batches and row-level failures

### Payroll Processing Engine

* Groups payments by clinician
* Generates pay statements with:

  * Total payments
  * Adjustments
  * Cost-share calculations

### Clean Architecture

* Separation of concerns across:

  * **Domain**
  * **Application**
  * **Infrastructure**
  * **API**

### Background Job Processing

* Handles long-running CSV ingestion using Hangfire
* Keeps API responsive

### Secure Authentication

* JWT-based authentication
* Role-based access (e.g., clinician vs admin)

---

## Project Structure

```plaintext
PayrollBackendProject/
│
├── PayrollBackendProject/
│   ├── API/              # Controllers (entry points)
│   ├── Application/      # DTOs, services, interfaces
│   ├── Domain/           # Core business logic & entities
│   ├── Infrastructure/   # EF Core, repositories, DB access
│   ├── Migrations/       # Database schema history
│   ├── Dockerfile
│   ├── Program.cs
│   └── PayrollBackendProject.csproj
│
├── docker-compose.yml
├── .env.example
├── .gitignore
└── README.md
```

---

## Setup & Run

### 1. Clone the repository

```bash
git clone https://github.com/your-username/PayrollBackendProject.git
cd PayrollBackendProject
```

---

### 2. Configure environment variables

```bash
cp .env.example .env
```

Update values as needed.

---

### 3. Run with Docker

```bash
docker compose up --build
```

---

### 4. Access the API

* API: http://localhost:5000
* Swagger UI: http://localhost:5000/swagger

---

## Database

* PostgreSQL runs in a Docker container
* Connection is configured via environment variables
* EF Core migrations are included for schema management

To apply migrations manually:

```bash
dotnet ef database update
```

---

## Environment Variables

Configuration is managed via `.env` (not committed to Git) but an example file is provided.

---

## Example Workflow

1. Upload CSV file via API endpoint
2. Background job processes file
3. Payment line items stored in database
4. System groups data by clinician
5. Pay statements generated

---

## Design Highlights

* **Idempotency-first ingestion design**
* **Separation of domain vs infrastructure logic**
* **Snapshot-based payroll calculations (immutable once approved)**
* **Scalable background job processing**
* **Environment-based configuration (no hardcoded secrets)**

---

## Future Improvements

* Auto-run EF migrations on startup
* Health check endpoint (`/health`)
* Deployment (Render / AWS)
* Cloud storage for uploaded files
* Audit logging enhancements

---

## Author

**Winston Heinrichs**

[View my other projects on my portfolio](https://www.winstonheinrichs.com)

---

## Notes

This project was built to demonstrate **real-world backend engineering skills**, including:

* API design
* database modeling
* background processing
* secure configuration management
* Dockerized environments

---
