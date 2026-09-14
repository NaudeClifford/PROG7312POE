# PROG7312POE

This is my SmartX WPF application designed to help organizations to manage their sensor devices though one platform. This is a 6 split project consisting of: API, Application, Domain, Infrastructure, Shared, WPF. I as super admin can monitor the application and the users while the admins are company controlled users that can create Techs and manage gateways. CRUD is done between admin and techs. The application has a readonly offline state for reading information but will automatically sync when becomes online. All update, create and delete is limited to online only. This chose is for performance as a cache improves read and write operations without interfacing with the UI.

SmartX — Setup and Running
Requirements
Visual Studio 2022
.NET SDK 10
Firebase Authentication
Firebase Admin SDK
Firebase service json file
Firebase project, (https://console.firebase.google.com/SmartX/smartx-d8206/)
Postman/Newman for API testing (To be Implimented in part 2)
Access to SmartX database (Local for now)
FluencyValidation
Mapstar
Any extra packages used by applicaiton, these provided are the basic ones required.

1. Open Solution by cloning repository, open is VS, let it restore packages.
2. Configure Database, configure database connection string in appsettings
3. Configure firebase and credentials for the project. make sure project matches firebase confirguration for authentication of email verification.
4. Build the solution, Run new profile for api and wpf to run together. startup project, mutiple startup projects.
5. Log in the application using new account or the standard testing accounts in my current firebase project. super12@gmail.com: 123456789!, admin@gmail.com: 123456789!, user@gmail.com: 123456789!. Do note that these are dev accounts, not real passwords or emails.
   
Testing the API in postman
Open postman, create or import the SmartX Api request collection that is provided on github. (be aware, part 1 postman collection may be incomplete/missing authentication headers.)
Set the request URL to the SmartXApi address
Pick the HTTPS header
Add authentication information where endpoint is required
send the request in order, you can't create a sensor before a user and company is set. its a hierachy.
Independent testing is better thanks to the hierachy.
The api can test validation failure and requests since the api validates requests before it gets processed.

Project structure
WPF -----> Application and domain ----> API ----> database/local cache(Infrustructure)

Firebase credentials, database passwords, connection strings and other sensitive configuration values should be kept out of source control.

This project is intended to be run in a development environment using the configuration supplied with the solution.

firebase is used for authenticating the users and is a requirement in this project.
