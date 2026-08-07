using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers
{
    /*public class TelemetryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }*/
}

/*
 Sensor                    Telemetry
────────────────────────────────────────
Sensor entity       →     Telemetry entity
CreateSensorCommand →     CreateTelemetryCommand
UpdateSensorCommand →     UpdateTelemetryCommand
CreateSensorHandler →     CreateTelemetryHandler
UpdateSensorHandler →     UpdateTelemetryHandler
GetSensorsHandler   →     GetTelemetryHandler
GetSensorByIdHandler→     GetTelemetryByIdHandler
DeleteSensorHandler →     DeleteTelemetryHandler
SensorsController   →     TelemetryController
ISensorRepository   →     ITelemetryRepository
*/
