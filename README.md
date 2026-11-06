# AgriStream IoT - Real-Time Commercial Farm Monitoring System
**Course:** CLDV7112 Cloud Development Practicum  
**Student Name:** Aaliyah Allie  
**Student Number:** ST10212542  
**Institution:** Rosebank College / The Independent Institute of Education (IIE)  
**Live Application URL:** https://st10212542-cldv7112-practicum-awaaere6asgwaras.southafricanorth-01.azurewebsites.net/

---

## 🌾 Project Overview & Architecture
AgriStream IoT is an enterprise-grade cloud precision agriculture platform designed for commercial crop farming. The platform centralizes real-time in-ground multi-depth soil probe telemetry, high-resolution aerial drone survey orthomosaic photography, and automated irrigation solenoid valve controls.

### Cloud Architecture & Services
| Practicum Requirement | Azure Cloud Service | Solution Component | Technical Description |
| :--- | :--- | :--- | :--- |
| **a) Web Hosting** | **Azure App Service (B1 Basic)** | `AgriStream.Web` | ASP.NET Core MVC enterprise portal hosted in South Africa North. |
| **b) Relational Database** | **Azure SQL Database** | `AgriStreamDbContext` | Stores user credentials, RBAC roles (Admin, Farmer, Agronomist), farms, and devices. |
| **c) Media Cloud Storage** | **Azure Blob Storage** | `BlobStorageService` (`drone-media`) | Unstructured storage for high-resolution aerial drone survey orthomosaics. |
| **c) NoSQL Cloud Storage** | **Azure Table Storage** | `TableStorageService` (`SensorTelemetry`) | High-throughput, low-latency time-series store for soil moisture & temperature streams. |
| **d) Serverless Pipeline** | **Azure Functions (.NET 8/10)** | `AgriStream.Functions` | Decoupled event-driven processing: BlobTrigger, HTTP GET, HTTP POST, and QueueTrigger. |
| **e) Load Testing** | **Console Runner (.NET 9)** | `AgriStream.LoadTest` | Concurrent stress testing (250 requests, 25 concurrent users, 100% success rate). |
| **f) Cloud Monitoring** | **Azure Monitor** | Metrics & Alerts | Live tracking of HTTP requests, CPU percentage, latency, and proactive warning alerts. |
| **g) Dynamic Auto-Scaling** | **App Service Plan** | Scale-out Rules | Dynamic horizontal scaling architecture (Scale-out at 70% CPU, Scale-in at 25% CPU). |

---

## 📂 Visual Studio Solution Structure (`AgriStreamIoT.sln`)
```text
ST10212542_CLDV7112_Practicum/
├── AgriStreamIoT.sln                <-- Open this solution in Visual Studio 2022
├── AgriStream_Database_Schema.sql   <-- Relational SQL DDL script for Azure SQL
├── README.md
│
├── AgriStream.Web/                  <-- ASP.NET Core MVC Web Project (Tasks a, b, c)
│   ├── Controllers/
│   │   ├── HomeController.cs        (Operational overview & scalability dashboard)
│   │   ├── AccountController.cs     (Role-based cookie authentication & registration)
│   │   ├── DashboardController.cs   (Real-time telemetry dashboard & probe readings)
│   │   ├── MediaController.cs       (Drone aerial survey upload to Azure Blob Storage)
│   │   ├── DevicesController.cs     (Hardware registry & solenoid valve actuation)
│   │   └── AdminController.cs       (Staff directory management for Admin role)
│   ├── Data/
│   │   └── AgriStreamDbContext.cs   (Entity Framework Core context for Azure SQL)
│   ├── Services/
│   │   ├── BlobStorageService.cs    (Azure.Storage.Blobs SDK client with SAS token support)
│   │   ├── TableStorageService.cs   (Azure.Data.Tables SDK client for probe telemetry)
│   │   └── QueueStorageService.cs   (Azure.Storage.Queues SDK client for valve commands)
│   ├── Models/                      (Entity & ViewModel classes)
│   ├── Views/                       (Razor views with commercial agriculture styling)
│   └── appsettings.example.json     (Configuration template for Azure connection strings)
│
├── AgriStream.Functions/            <-- Azure Functions Isolated Worker Project (Task d)
│   ├── Functions/
│   │   ├── GetLatestTelemetry.cs    (HTTP GET: queries latest telemetry from Table Storage)
│   │   ├── TriggerIrrigationPost.cs (HTTP POST: pushes valve command to Azure Queue)
│   │   ├── ProcessIrrigationQueue.cs(QueueTrigger: dequeues command and updates Azure SQL status)
│   │   └── ProcessDroneImageBlob.cs (BlobTrigger: fires on drone image upload and writes audit)
│   ├── host.json
│   └── local.settings.example.json
│
└── AgriStream.LoadTest/             <-- Cloud Load Test Runner Project (Task e)
    └── Program.cs                   (Multi-threaded HTTP benchmark against Azure App Service)
```

---

## 🔑 Role-Based Access Control (RBAC) Test Accounts
| Role | Username | Password | Operational Access Level |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin_agristream` | `AdminPass123!` | Complete administrative authority: staff management, drone survey deletion, telemetry entry. |
| **Farmer** | `john_farmer` | `FarmerPass123!` | Farm operations: triggers irrigation solenoid valves, monitors telemetry, views drone canopy surveys. |
| **Agronomist** | `dr_sarah_agro` | `AgroPass123!` | Scientific analysis: records field soil readings, uploads drone surveys; valve triggers disabled. |

---

## ⚡ Task d: Serverless Data Pipeline (Azure Functions)
1. **`GetLatestTelemetry` (HTTP GET Trigger):**
   - **Route:** `GET /api/telemetry?deviceId=SN-SOIL-001`
   - **Action:** Queries Azure Table Storage (`SensorTelemetry`) for the latest moisture and temperature readings.
2. **`TriggerIrrigationPost` (HTTP POST Trigger):**
   - **Route:** `POST /api/irrigation/trigger`
   - **Action:** Ingests valve actuation commands and places a message into the `irrigation-commands` Azure Storage Queue.
3. **`ProcessIrrigationQueue` (Queue Trigger):**
   - **Queue:** `irrigation-commands`
   - **Action:** Consumes queued command messages, executes asynchronous valve actuation, and updates the device status in Azure SQL to `Active - Irrigating`.
4. **`ProcessDroneImageBlob` (Blob Trigger):**
   - **Container:** `drone-media/{name}`
   - **Action:** Automatically fires when a new drone flight image is uploaded to Azure Blob Storage, validating imagery and recording processing audit logs into Azure Table Storage.

---

## 📊 Task e: Performance & Load Testing Results
The dedicated `AgriStream.LoadTest` project executes multi-threaded concurrent HTTP load simulations against the live deployed cloud endpoint.
- **Target Endpoint:** `https://st10212542-cldv7112-practicum-awaaere6asgwaras.southafricanorth-01.azurewebsites.net/`
- **Total Requests:** 250 HTTP transactions
- **Concurrency:** 25 simultaneous virtual users
- **Success Rate:** **100.0% (250/250 HTTP 200 OK)**
- **Error Rate:** **0.00% (Zero dropped connections)**
- **Average Latency:** **371.08 ms** (under the 500 ms SLA)
- **Throughput:** **64.43 requests/second**
