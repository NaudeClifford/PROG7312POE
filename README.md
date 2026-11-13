<p align="center">
<strong>Smart X Sensor Management and Monitoring Platform</strong>
  
<br>
PROG7312 POE | .NET 10 | WPF | ASP.NET Core
</p>

<h2 align="center">Table of Contents</h2>
<div align="center">
1. <a href="#1-project-overview">Project Overview</a><br>
2. <a href="#2-project-architecture">Project Architecture</a><br>
3. <a href="#3-main-features">Main Features</a><br>
4. <a href="#4-technologies-used">Technologies Used</a><br>
5. <a href="#5-requirements">Requirements</a><br>
6. <a href="#6-installation-and-setup">Installation and Setup</a><br>
7. <a href="#7-configuration">Configuration</a><br>
8. <a href="#8-firebase-setup">Firebase Setup</a><br>
9. <a href="#9-running-the-application">Running the Application</a><br>
10. <a href="#10-development-and-production-environments">Development and Production Environments</a><br>
11. <a href="#11-json-repositories-and-seed-data">JSON Repositories and Seed Data</a><br>
12. <a href="#12-part-3--mesh-networking">Part 3 – Mesh Networking</a><br>
13. <a href="#13-api-testing">API Testing</a><br>
14. <a href="#14-part-2-3-improvements">Part 2–3 Improvements</a><br>
15. <a href="#15-security-and-configuration">Security and Configuration</a><br>
16. <a href="#16-important-notes">Important Notes</a><br>
17. <a href="#17-author">Author</a><br>
</div>
<h2 id="1-project-overview">1. Project Overview</h2>

SmartX is a WPF-based sensor management and monitoring application designed to help organisations monitor, manage and control connected sensor devices through a centralised platform.

The application follows a layered architecture, separating the user interface, business logic, domain entities, infrastructure and data access responsibilities.

SmartX provides functionality for managing companies, administrators, technicians, gateways, sensors, telemetry, alerts and historical sensor readings.

The application also supports offline read-only functionality through locally cached information, allowing users to access previously synchronised data when an internet connection is unavailable.

Part 3 extends the platform by introducing mesh networking functionality, including mesh dashboards, network topology visualisation and routing information.

<h2 id="2-project-architecture">2. Project Architecture</h2>

SmartX follows a layered architecture consisting of the following projects:

Project	Responsibility
* SmartX.API	REST API responsible for handling HTTP requests, authentication and communication between the application and backend services.
* SmartX.Application	Contains application services, business logic, validation and use cases.
* SmartX.Domain	Contains core business entities, enums, interfaces and domain rules.
* SmartX.Infrastructure	Handles data persistence, repositories, Firebase integration and external services.
* SmartX.Shared	Contains shared DTOs, request models, response models and mapping definitions.
* SmartX.WPF	Desktop application providing the user interface through WPF and the MVVM architectural pattern.

<h3 align="center">Architecture Overview</h3>

text
                 SmartX.WPF
                     |
                     v
                  SmartX.API
                     |
                     v
             SmartX.Application
                     |
                     v
                SmartX.Domain
                     |
                     v
             SmartX.Infrastructure
                     |
                     v
                 Data Storage
              (JSON / SQLite)


SmartX.Shared provides shared data transfer objects and request/response models across the relevant projects.

The WPF application uses MVVM to separate presentation logic from application functionality.

The API handles incoming HTTP requests, while the Application layer processes business operations using the Domain and Infrastructure layers.

<h2 id="3-main-features">3. Main Features</h2>

<h3>User and Role Management</h3>

Role-based access control.
Super administrators manage the overall platform.
Company administrators manage their respective organisations.
Company administrators can create and manage technicians.
Company administrators can request additional administrator accounts.
Administrator deletion requests require super administrator review and confirmation with the relevant company.

<h3>Company Management</h3>

Create and manage registered companies.
Maintain company-specific data.
Isolate company resources and operational information.
Prevent company administrators from accessing other companies' resources.

<h3>Gateway Management</h3>

Register and manage gateways.
Associate sensors with their respective gateways.
Monitor gateway connectivity and operational status.

<h3>Sensor Management</h3>

Create, update and delete sensors.
Associate sensors with gateways.
Monitor registered sensor devices.
Retrieve sensor information and associated telemetry.

<h3>Telemetry and Historical Data</h3>

Collect and display sensor readings.
Monitor historical sensor information.
Review previously recorded telemetry.
Support sensor monitoring and operational analysis.

<h3>Alerts and Command Stream</h3>

Monitor sensor conditions and associated alerts.
Review command activity through the Command Stream.
Monitor operational events and device communication.

<h3>Offline Read-Only Functionality</h3>

SmartX supports offline access to previously synchronised information.

Cached information can be accessed without an active internet connection.
Create, update and delete operations require an active connection.
Changes and synchronisation processes are handled through the synchronisation service when connectivity is available.
JSON repositories provide data persistence.
SQLite is used for local caching.

The repository and caching implementations are designed to be replaceable, allowing alternative storage providers to be introduced as the application evolves.

<h2 id="4-technologies-used">4. Technologies Used</h2>

Technology	Purpose
.NET 10	Application framework
C#	Primary programming language
WPF	Desktop user interface
ASP.NET Core	REST API
MVVM	Presentation architecture
Firebase Authentication	User authentication
Firebase Admin SDK	Server-side Firebase integration
JSON	Local repository persistence
SQLite	Local cached data storage
FluentValidation	Request and model validation
Mapster	Object mapping
LiveCharts2	WPF chart visualisations
Postman	API testing
Visual Studio 2022	Development environment

Additional NuGet packages may be required depending on the solution configuration.

<h2 id="5-requirements">5. Requirements</h2>

Before running SmartX, ensure that the following are installed or available:

Visual Studio 2022 with the required .NET desktop and ASP.NET development workloads.
.NET 10 SDK.
A Firebase project.
Firebase Authentication configuration.
Firebase Admin SDK.
A valid Firebase service account JSON file.
Access to the configured database or local JSON repositories.
Postman for API testing.
All NuGet packages referenced by the solution.

<strong>SmartX Firebase Project:</strong>

<a href="https://console.firebase.google.com/project/smartx-d8206/" target="_blank">Open SmartX Firebase Console</a>

<h2 id="6-installation-and-setup">6. Installation and Setup</h2>

<h3>Step 1: Clone the Repository</h3>

Clone the GitHub repository and open the solution in Visual Studio 2022.

Allow Visual Studio to restore the required NuGet packages.

<h3>Step 2: Configure the API</h3>

Open the <code>SmartX.API</code> project.

Configure the appropriate application settings file.

The API configuration may include:

Database connection strings or JSON repository settings.
Firebase project configuration.
Firebase service account location.
Authentication settings.
API-specific settings.
CORS configuration, where required.
External service configuration.

<strong>Example configuration structure:</strong>

appsettings.Production.json 
```json
{
  {
    "SeedData": {
      "Enabled": false,
      "ResetOnStartup": false,
      "CleanupOnProduction": true
    }
  }
}
```

appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },

    "SeedData": {
      "Enabled": true,
      "ResetOnStartup": false,
      "CleanupOnProduction": true
    }
  }
}
```

appsettings.json
```json
{
  "Mongo": {
    "ConnectionString": "",
    "Database": "SmartX"
  },

  "Firebase": {
    "ProjectId": "",
    "ServiceAccountPath": "firebase-service-account.json"
  },
  

  "MeshMonitoring": {
    "LinkTimeoutMinutes": 2,
    "NodeTimeoutMinutes": 5,
    "MinimumLinkQuality": 20,
    "DegradedLinkQuality": 50,
    "MaximumHealthyLatency": 100,
    "MinimumNetworkAvailability": 95,
    "MaximumLatencyMilliseconds": 100,
    "MaximumEnergyCost": 100,
    "RecordFailedRoutes": true
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

<h3>Step 3: Configure the WPF Application</h3>

Open the <code>SmartX.WPF</code> project.

Configure its application settings to include the API address and any required Firebase or application configuration.

<strong>Example:</strong>

```json
{
  "Firebase": {
    "ApiKey": ""
  },
  "Api": {
    "BaseUrl": "https://localhost:7149/",
    "ApiKey": ""
  }
}
```


Ensure that the WPF API address matches the URL and port configured when running <code>SmartX.API</code>.

<h3>Step 4: Configure Firebase</h3>

Firebase is used for user authentication.

Steps:

Open the Firebase Console.
Select the SmartX Firebase project.
Navigate to Project Settings.
Obtain the appropriate service account credentials.
Store the service account JSON file securely.
Configure the API to locate the service account file.
Ensure that the Firebase project ID matches the configured application settings.
Ensure that Firebase Authentication is enabled and configured correctly.

<strong>Important:</strong> The Firebase service account JSON file must never be committed to GitHub.

<h3>Step 5: Configure Startup Projects</h3>

SmartX requires the API and WPF application to run together.

In Visual Studio:

Right-click the solution.
Select <strong>Configure Startup Projects</strong>.
Select <strong>Multiple startup projects</strong>.
Set <code>SmartX.API</code> to <strong>Start</strong>.
Set <code>SmartX.WPF</code> to <strong>Start</strong>.
Apply the changes.

Run the solution.

The API must be available when the WPF application attempts to communicate with it.

<h2 id="7-configuration">7. Configuration</h2>

SmartX uses separate configuration settings for the API and WPF application.

These settings allow the application to communicate with the correct backend services and use the appropriate environment configuration.

Configuration may include:

API addresses.
Firebase project identifiers.
Authentication settings.
Database connection information.
JSON repository locations.
SQLite cache locations.
External service settings.

Configuration values should be maintained separately from application source code wherever possible.

<h2 id="8-firebase-setup">8. Firebase Setup</h2>

Firebase provides the authentication infrastructure used by SmartX.

The Firebase integration supports:

User authentication.
Email verification.
Firebase user identity validation.
Server-side authentication integration through Firebase Admin SDK.

<h3>Firebase Configuration Requirements</h3>

Ensure that:

The correct Firebase project is selected.
Email/password authentication is enabled.
The Firebase project ID matches the application configuration.
The API has access to the Firebase service account.
The service account JSON file is stored outside source control.
Authentication tokens are validated by the API.

<strong>Important:</strong> Firebase authentication credentials and service account files must never be exposed in public repositories.

<h2 id="9-running-the-application">9. Running the Application</h2>

<h3>Configure Multiple Startup Projects</h3>

In Visual Studio:

Right-click the solution.
Select <strong>Configure Startup Projects</strong>.
Select <strong>Multiple startup projects</strong>.
Set <code>SmartX.API</code> to <strong>Start</strong>.
Set <code>SmartX.WPF</code> to <strong>Start</strong>.
Apply the changes.

Run the solution.

<h3>Logging In</h3>

Register a new account or use an existing development account configured in Firebase.

<strong>Development testing accounts:</strong>

Role	Email	Password
Super Admin	super12@gmail.com	123456789!
Administrator	admin@gmail.com	123456789!
User	user@gmail.com	123456789!

<strong>Note:</strong> These are development testing credentials, not production credentials. Their availability depends on the Firebase project's current user configuration. Remove or rotate them before any production deployment.

<h2 id="10-development-and-production-environments">10. Development and Production Environments</h2>

SmartX supports separate development and production configurations.

<h3>Development Configuration</h3>

Development configuration is intended for local testing and may contain:

Local database settings.
JSON repository configuration.
Development Firebase settings.
Local API URLs.
Development service configuration.
Test data and seed data.

<h3>Production Configuration</h3>

Production configuration is intended for deployment and should contain:

Production database connection strings.
Production Firebase configuration.
Production API addresses.
Production service configuration.
Secure authentication settings.
No development seed data.

<h3>Configuration Structure</h3>

text
SmartX.API/
    appsettings.json
    appsettings.Development.json
    appsettings.Production.json

SmartX.WPF/
    appsettings.json

<h3>Selecting the API Environment</h3>

For ASP.NET Core, the environment can be selected using:

text
ASPNETCORE_ENVIRONMENT=Development


Or:

text
ASPNETCORE_ENVIRONMENT=Production


<h3>Selecting the WPF Environment</h3>

WPF does not automatically use ASP.NET Core environment configuration.

The WPF application must use its own configured environment selection mechanism.

Check the application startup configuration to ensure that the correct settings are loaded.

<strong>Important:</strong> Changing environments does not automatically migrate or delete existing database records. Development seed-data cleanup must be explicitly implemented in the repository or seeding mechanism.

<h2 id="11-json-repositories-and-seed-data">11. JSON Repositories and Seed Data</h2>

SmartX supports local JSON-based repositories for development and testing.

These repositories allow the application to operate with sample data without requiring a fully populated production database.

Development seed data can include:

Companies.
Users and role assignments.
Gateways.
Sensors.
Telemetry records.
Alerts.
Historical readings.
Mesh nodes.
Mesh links.
Mesh logs.
Routing and topology test data, where supported.

Seed data is intended for development and testing only.

When switching to production, development seed data must not be loaded.

If environment-based seed cleanup is implemented, it should only remove development seed records and must not delete legitimate production information.

<h2 id="12-part-3--mesh-networking">12. Part 3 – Mesh Networking</h2>

Part 3 introduces mesh networking functionality to SmartX.

The mesh implementation extends the existing sensor management platform by providing network discovery, topology management, connectivity monitoring and routing information.

<h3>12.1 Mesh Dashboard</h3>

The Mesh Dashboard provides an overview of the selected gateway's mesh network.

It includes:

Total registered mesh nodes.
Gateway and connected sensor nodes.
Online and offline node counts.
Network health indicators.
Link quality and signal information.
Routing performance analytics.
Mesh network activity and status.

<h3>12.2 Network Topology</h3>

The Network Topology page provides a visual representation of the mesh network.

It displays:

The selected gateway.
Registered mesh nodes.
Connections between nodes.
Link availability.
Signal strength.
Connection quality.

The topology is based on mesh node and mesh link records associated with the selected gateway.

<h3>12.3 Mesh Routing</h3>

The Mesh Routing page provides information about network routing and communication paths.

It includes:

Mesh node connectivity.
Available and unavailable links.
Routing performance.
Link quality.
Latency information.
Energy cost information, where available.

<h3>12.4 Mesh Architecture</h3>

The mesh implementation uses the following domain entities:

Entity	Responsibility
<code>MeshNode</code>	Represents a registered node within a mesh network.
<code>MeshLink</code>	Represents a connection between mesh nodes.
<code>MeshLog</code>	Stores mesh-related activity and event information.

The corresponding DTOs are located in:

text
SmartX.Shared.DTOs.Mesh


<h3>Mesh Discovery Services</h3>

Mesh discovery is handled through:

text
IMeshDiscoveryService
IMeshDiscoveryProvider
IMeshGatewayDiscoveryClient


<h3>Topology Management</h3>

Topology management is handled through:

text
IMeshTopologyService
IMeshNodeRepository
IMeshLinkRepository
IMeshLogRepository


The discovery synchronisation process retrieves discovered nodes and links, checks existing records and updates or registers them as necessary.

<h3>Important Mesh Networking Note</h3>

Mesh nodes are separate records from ordinary sensor records.

Creating a sensor does not automatically create a mesh node.

Mesh discovery or the appropriate registration process must populate the mesh repositories before nodes and links appear on the mesh pages.

Mesh pages operate against the currently selected gateway.

<h2 id="13-api-testing">13. API Testing</h2>

Postman can be used to test the SmartX API independently of the WPF application.

<h3>Testing Procedure</h3>

Open Postman.
Import or create the SmartX API request collection from the GitHub repository.
Configure the request URL to match the running SmartX API.
Select the appropriate HTTP method.
Add the required authentication headers or bearer token.
Include the required request body.
Send requests in the correct order.
Review the API responses and validation results.

<h3>Recommended Testing Sequence</h3>

The application follows a hierarchical relationship between entities.

A typical testing sequence is:

Create a company.
Create or assign a user.
Register a gateway.
Create sensors associated with the gateway.
Generate telemetry.
Register or discover mesh nodes.
Register mesh links.
Retrieve mesh topology.
Retrieve mesh routing information.

Requests must respect the relationships between entities.

For example, a sensor cannot be associated with a gateway that does not exist.

The API also performs request validation, allowing successful requests and validation failures to be tested independently.

<h2 id="14-part-2-3-improvements">14. Part 2–3 Improvements</h2>

Part 3 builds on the foundation established in Part 2 by extending SmartX beyond standard sensor and gateway management.

The improvements focus on improving application functionality, maintainability, data accessibility and network monitoring.

The main improves from part 2 is based on the provided feedback from the lecturer: 

* No Real Time Command Stream item.
* The command and undo features are hidden inside the History page.
* Incoming critical alerts are not routed through a priority queue ahead of normal packets.
* Undo Stack is in WPF instead of API.
* No registry dictionary keyed by MAC address used when a packet arrives.
* Hash set of active errors are in WPF instead of API and not for disconnected nodes in the API.
* The predictive engine is not built on the history of searches and manual commands.

Fixes
* Added Real time command stream page and viewmodel, then took the logic from the history page and separated it into separate services for the command page.
* The command and undo features are now in the Command page as intended.
* Fixed the queue ordering as critical alerts now show first and are separate service from normal and priotie queue in the alerts page.
* Moved undo stack, alerts, predictive engine and other services to the API and created 3 new controllers, Sensor Command controller, Alerts controller, and predictive controller
* Created a sensor registry service to be keyed by MAC address for when telemetry is received and the service is used by telemetry page.
* Hash set has been moved to the api with the alerts queues and other features and the alerts controller checks for disconnected nodes using sensor connection status.
* the predictive engine was built on history of manual commands using userbehaviour pipeline but not fully noticed by lecturer. searches is not a feature implimented in the project yet but will be for future development.

Added for part 3
* I had added AVL tree to the telemetry history store pipeline, this is a extra service and works with current sorted dictionary and allows for a reduncy if the sorted dictionary fails. Both receive the same data and the application can choose on which data structure to use.

<h3>14.1 Mesh Networking Integration</h3>

<strong>Improvement:</strong> Introduced mesh networking functionality.

Part 2 focused primarily on managing companies, gateways, sensors and telemetry.

Part 3 extends these capabilities by introducing:

Mesh node management.
Mesh link management.
Mesh discovery services.
Network topology visualisation.
Mesh routing information.
Mesh activity logging.

This allows SmartX to provide a broader view of connected devices and their network relationships.

<h3>14.2 Network Topology Visualisation</h3>

<strong>Improvement:</strong> Introduced a dedicated visual representation of mesh networks.

The topology functionality provides a clearer understanding of:

How mesh nodes are connected.
Which links are available.
How nodes relate to the selected gateway.
The overall structure of the network.

This improves network visibility compared with relying exclusively on individual sensor and gateway records.

<h3>14.3 Mesh Routing and Network Performance</h3>

<strong>Improvement:</strong> Added dedicated routing information and network performance indicators.

The routing functionality provides additional information about:

Connectivity.
Link quality.
Latency.
Routing performance.
Energy cost information, where available.

This provides additional monitoring capabilities for understanding network communication and operational conditions.

<h3>14.4 Improved Offline Data Accessibility</h3>

<strong>Improvement:</strong> Expanded the application's local data accessibility through JSON repositories and SQLite caching.

The application supports:

Reading previously synchronised information while offline.
Local caching through SQLite.
JSON-based repository persistence.
Synchronisation when connectivity becomes available.

Write operations remain dependent on an active connection.

This improves accessibility during temporary connectivity interruptions.

<h3>14.5 Improved Separation of Responsibilities</h3>

<strong>Improvement:</strong> Maintained a layered architecture with dedicated responsibilities.

The implementation separates:

Domain entities.
Application services.
Infrastructure and repository operations.
API communication.
WPF presentation logic.
Shared DTOs and request models.

This separation improves maintainability and makes it easier to extend individual application components without unnecessarily affecting unrelated functionality.

<h3>14.6 Improved Data Validation and API Testing</h3>

<strong>Improvement:</strong> Supports structured validation and independent API testing.

FluentValidation is used for request validation, while Postman allows API functionality to be tested independently of the desktop application.

This makes it easier to identify invalid requests, verify entity relationships and investigate backend issues.

<h3>14.7 Improved Role-Based Administration</h3>

<strong>Improvement:</strong> Extended administrative management through company-level roles and administrator approval processes.

Company administrators can manage their own company resources, while super administrators retain responsibility for platform-level administration and administrator deletion requests.

This supports a more structured administrative workflow.

<h2 id="15-security-and-configuration">15. Security and Configuration</h2>

The following information must never be committed to source control:

Firebase service account JSON files.
Database passwords.
Database connection strings containing credentials.
API secrets.
Authentication tokens.
Private keys.
Production configuration secrets.

Use local configuration files, environment variables or a secure secrets-management solution.

Ensure sensitive configuration files are included in <code>.gitignore</code>.

Additional security considerations:

Validate authentication tokens on protected API endpoints.
Apply role-based authorisation to administrative operations.
Enforce company-level data isolation on the backend.
Avoid exposing sensitive information in application logs.
Use HTTPS for production API communication.
Rotate development credentials before production deployment.

<h2 id="16-important-notes">16. Important Notes</h2>

Firebase is required for authentication.
The API and WPF application must use matching API configuration.
Mesh nodes and links must be registered or discovered independently of ordinary sensor creation.
Mesh pages operate against the selected gateway.
Development seed data should only be used in development.
Production must use the appropriate production repositories and configuration.
Offline functionality is intended for reading cached information.
Create, update and delete operations require connectivity.
Firebase service account credentials must never be committed to GitHub.

<h2 id="17-author">17. Author</h2>

<p align="center">
<strong>Clifford Naude</strong>
<br>
PROG7312 – POE Final
<br>
SmartX WPF Application
<br>
Student Number: ST10258513
</p>
