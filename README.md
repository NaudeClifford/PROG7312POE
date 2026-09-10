# PROG7312POE

This is my SmartX IoT monitoring application. The system connects gateways to sensors, collects sensor telemetry, processes that data through the application and API layers, stores it, and then displays the telemetry to the user through the WPF interface. I chose this 6 project layout for larger applications as this application can greatly grow into one. 

SmartX — Setup and Running
Requirements
.NET SDK 10
Visual Studio 2022/2026
Firebase project

1. Clone/open the SmartX solution.
2. Configure the API database connection string.
3. Configure Firebase credentials.
4. Build the solution.
5. Start SmartX.API.
6. Start SmartX.WPF.
7. Log in using a configured Firebase account or create new account.

Visual Studio → Solution → Properties → Multiple startup projects

SmartX.API
    ↓
running REST API

SmartX.WPF
    ↓
desktop application

Then log in.
