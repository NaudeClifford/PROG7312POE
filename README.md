PROG7312POE – SmartX
Project Overview

SmartX is a WPF-based sensor management application designed to help organisations monitor, manage and control their connected sensor devices through a centralised platform.

The solution is divided into six projects:

SmartX.API – REST API responsible for handling HTTP requests, authentication and communication between the application and backend services.
SmartX.Application – Contains application services, business logic, validation and use cases.
SmartX.Domain – Contains core business entities, enums, interfaces and domain rules.
SmartX.Infrastructure – Handles data persistence, repositories, Firebase integration and external services.
SmartX.Shared – Contains shared DTOs, request models, response models and mapping definitions.
SmartX.WPF – The desktop application, providing the user interface through WPF and MVVM architecture.
Main Features
Role-based access control: Super administrators manage the platform, while company administrators manage their company, technicians and gateways.
User management: Administrators can create and manage technicians and other permitted users.
Company management: Companies operate within their own isolated environment.
Gateway management: Gateways provide the connection point between the platform and registered sensors.
Sensor management: Create, update, delete and monitor sensors associated with gateways.
Telemetry monitoring: Collect and display sensor readings.
Historical data: Review previously collected sensor information.
Alerts: Monitor sensor conditions and associated alerts.
Command Stream: Monitor real-time command activity.
Offline read-only functionality: The application supports reading cached information while offline. Create, update and delete operations require an active connection. Changes can synchronise when connectivity is restored.
Part 3 – Mesh Networking

Part 3 introduces mesh networking functionality to SmartX.

The mesh implementation provides three dedicated WPF pages:

1. Mesh Dashboard

Provides an overview of the selected gateway's mesh network, including:

Total registered mesh nodes.
Gateway and connected sensor nodes.
Online and offline node counts.
Network health indicators.
Link quality and signal information.
Routing performance analytics.
Mesh network activity and status.
2. Network Topology

Provides a visual representation of the mesh network.

It displays:

The selected gateway.
Registered mesh nodes.
Connections between nodes.
Link availability.
Signal strength.
Connection quality.

The topology is based on the mesh node and mesh link records associated with the selected gateway.

3. Mesh Routing

Provides information about network routing and communication paths.

It includes:

Mesh node connectivity.
Available and unavailable links.
Routing performance.
Link quality.
Latency information.
Energy cost information where available.
Mesh Architecture

The mesh implementation uses the following domain entities:

MeshNode
MeshLink
MeshLog

The corresponding DTOs are located in:

SmartX.Shared.DTOs.Mesh

The application services are responsible for mesh discovery, topology management and synchronisation.

Mesh discovery is handled through:

IMeshDiscoveryService
IMeshDiscoveryProvider
IMeshGatewayDiscoveryClient

Topology management is handled through:

IMeshTopologyService
IMeshNodeRepository
IMeshLinkRepository
IMeshLogRepository

The discovery synchronisation process retrieves discovered nodes and links, checks existing records and updates or registers them as necessary.

Important: Mesh nodes are separate records from ordinary sensor records. Creating a sensor does not automatically create a mesh node. Mesh discovery or the appropriate registration process must populate the mesh repositories before nodes and links appear on the mesh pages.

Requirements
Visual Studio 2022
.NET SDK 10
Firebase project and authentication
Firebase Admin SDK
Firebase service account JSON file
SmartX Firebase project: https://console.firebase.google.com/project/smartx-d8206/
Access to the SmartX database or configured local JSON repositories
Postman for API testing
FluentValidation
Mapster
LiveCharts2 for WPF chart visualisations
Any additional NuGet packages referenced by the solution
Getting Started
1. Clone the repository

Clone the GitHub repository and open the solution in Visual Studio 2022.

Allow Visual Studio to restore all NuGet packages.

2. Configure the API

Open the SmartX.API project.

Configure the appropriate application settings file.

The API configuration must include the following sections where applicable:

Database connection string or JSON repository configuration.
Firebase project configuration.
Firebase service account location.
Authentication settings.
API-specific settings.
CORS configuration, if required.
Any additional external service configuration.

Example configuration structure:

json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_DATABASE_CONNECTION"
  },
  "Firebase": {
    "ProjectId": "YOUR_FIREBASE_PROJECT_ID",
    "ServiceAccountPath": "YOUR_SERVICE_ACCOUNT_PATH"
  },
  "AppSettings": {
    "Environment": "Development"
  }
}


The example is a placeholder. Use the actual configuration keys expected by the application.

3. Configure the WPF application

Open the SmartX.WPF project.

Configure its application settings to include the API address and any required Firebase or application configuration.

Example:

json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:YOUR_API_PORT/"
  },
  "Firebase": {
    "ProjectId": "YOUR_FIREBASE_PROJECT_ID"
  }
}


Make sure the WPF API address matches the URL and port configured when running SmartX.API.

4. Configure Firebase

Firebase is used for user authentication.

The project requires a Firebase project and a valid Firebase service account.

Steps:

Open the Firebase Console.
Select the SmartX Firebase project.
Navigate to Project Settings.
Generate or obtain the appropriate service account credentials.
Store the service account JSON file securely.
Configure the API to locate the service account file.
Ensure the Firebase project ID matches the project configured in the application.

The Firebase service account JSON file must never be committed to GitHub.

The application also relies on Firebase Authentication for user authentication and email verification.

5. Configure the startup projects

SmartX requires the API and WPF application to run together.

In Visual Studio:

Right-click the solution.
Select Configure Startup Projects.
Select Multiple startup projects.
Set SmartX.API to Start.
Set SmartX.WPF to Start.
Apply the changes.

Run the solution.

The API should start first or be available when the WPF application attempts to communicate with it.

6. Log in

Register a new account or use the existing development accounts configured in the Firebase project.

Development testing accounts:

Role	Email	Password
Super Admin	super12@gmail.com	123456789!
Administrator	admin@gmail.com	123456789!
User	user@gmail.com	123456789!

Note: These are development testing accounts, not production credentials. Their availability depends on the Firebase project's current user configuration.

Application Configuration – Development and Production

SmartX supports different application configurations for development and production.

The application uses separate settings files so that development testing and production execution can use different configuration values.

Development configuration

Development configuration is used for local testing.

It can contain:

Local database settings.
JSON repository configuration.
Development Firebase settings.
Local API URLs.
Development service configuration.
Test data and seed data.
Production configuration

Production configuration is intended for deployment.

It should contain:

Production database connection strings.
Production Firebase configuration.
Production API addresses.
Production service configuration.
No development seed data.
Switching between configurations

The API and WPF projects maintain their own configuration files.

Depending on the configuration implemented in each project, the files may follow this structure:

text
SmartX.API/
    appsettings.json
    appsettings.Development.json
    appsettings.Production.json

SmartX.WPF/
    appsettings.json
    appsettings.Development.json
    appsettings.Production.json


For the API, the environment is selected through the ASP.NET Core environment variable:

text
ASPNETCORE_ENVIRONMENT=Development


or:

text
ASPNETCORE_ENVIRONMENT=Production


For WPF, use the application's configured environment selection mechanism. WPF does not automatically switch environments using the ASP.NET Core environment variable unless this behaviour has been implemented.

In Visual Studio, check the launch profiles and application startup configuration to ensure the correct settings are loaded.

Important: Changing the environment does not automatically migrate or delete existing database records. Any development seed-data cleanup must be explicitly implemented in the JSON repository or seeding mechanism.

JSON Repository and Seed Data

SmartX supports local JSON-based repositories for development and testing.

These repositories allow the application to run with sample data without requiring a fully populated production database.

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

The seed data is intended for development and testing only.

When switching to production, development seed data must not be loaded.

If the repository implements environment-based seed cleanup, it should only remove development seed records and must not delete legitimate production information.

Testing the API with Postman

Postman can be used to test the SmartX API independently of the WPF application.

Open Postman.
Import or create the SmartX API request collection from the GitHub repository.
Configure the request URL to match the running SmartX API.
Select the appropriate HTTP method.
Add the required authentication headers or bearer token.
Send requests in the correct order.

The application follows a hierarchical relationship between entities.

For example:

Create a company.
Create or assign a user.
Register a gateway.
Create sensors associated with the gateway.
Generate telemetry.
Register or discover mesh nodes.
Register mesh links.
Retrieve mesh topology and routing information.

Requests must respect the relationships between entities. For example, a sensor cannot be associated with a gateway that does not exist.

The API also performs request validation, allowing validation failures and successful requests to be tested independently.

Project Architecture

The general architecture follows:

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
          (Database / JSON)


SmartX.Shared provides shared DTOs, request models and response models used across the relevant projects.

The WPF application uses MVVM to separate the user interface from application logic.

The API handles incoming requests, while the Application project processes business operations using the Domain and Infrastructure layers.

Security and Configuration

The following information must never be committed to source control:

Firebase service account JSON files.
Database passwords.
Database connection strings containing credentials.
API secrets.
Authentication tokens.
Private keys.
Production configuration secrets.

Use local configuration files, environment variables or a secure secrets-management solution.

Ensure sensitive configuration files are included in .gitignore.

Important Notes
Firebase is a requirement for authentication.
The API and WPF application must use matching API configuration.
Mesh nodes and links must be registered or discovered independently of ordinary sensor creation.
Mesh pages operate against the selected gateway.
Development seed data should only be used in development.
Production must use the appropriate production repositories and configuration.
Offline functionality is intended for reading cached information; create, update and delete operations require connectivity.
Author

Clifford Naude

PROG7312 – Programming 3

SmartX WPF Application

Student Number: ST10258513