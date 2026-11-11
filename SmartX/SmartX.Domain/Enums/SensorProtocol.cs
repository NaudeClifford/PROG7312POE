namespace SmartX.Domain.Enums;

public enum SensorProtocol
{
    Unknown = 0,
    Simulation = 1,
    ModbusTcp = 2,
    Mqtt = 3,
    Http = 4,
    OpcUa = 5
}