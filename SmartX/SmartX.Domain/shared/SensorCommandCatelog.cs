
using SmartX.Domain.Enums;

namespace SmartX.Domain.shared;

public static class SensorCommandCatalog
{
    private static readonly IReadOnlyDictionary<SensorCommandType, string>
        CommandNames = new Dictionary<SensorCommandType, string>
        {
            [SensorCommandType.On] = "ON",
            [SensorCommandType.Off] = "OFF",

            [SensorCommandType.PowerOn] = "POWER ON",
            [SensorCommandType.PowerOff] = "POWER OFF",

            [SensorCommandType.Start] = "START",
            [SensorCommandType.Stop] = "STOP",

            [SensorCommandType.Enable] = "ENABLE",
            [SensorCommandType.Disable] = "DISABLE",

            [SensorCommandType.Open] = "OPEN",
            [SensorCommandType.Close] = "CLOSE",

            [SensorCommandType.Lock] = "LOCK",
            [SensorCommandType.Unlock] = "UNLOCK",

            [SensorCommandType.Activate] = "ACTIVATE",
            [SensorCommandType.Deactivate] = "DEACTIVATE",

            [SensorCommandType.Reset] = "RESET"
        };

    // Precomputed reverse lookup for efficient command parsing.
    private static readonly IReadOnlyDictionary<string, SensorCommandType>
        CommandsByName = CommandNames.ToDictionary(
            pair => pair.Value,
            pair => pair.Key,
            StringComparer.OrdinalIgnoreCase);

    // Inverse commands are stored as enum mappings.
    // RESET intentionally has no inverse.
    private static readonly IReadOnlyDictionary<SensorCommandType, SensorCommandType>
        InverseCommands = new Dictionary<SensorCommandType, SensorCommandType>
        {
            [SensorCommandType.On] = SensorCommandType.Off,
            [SensorCommandType.Off] = SensorCommandType.On,

            [SensorCommandType.PowerOn] = SensorCommandType.PowerOff,
            [SensorCommandType.PowerOff] = SensorCommandType.PowerOn,

            [SensorCommandType.Start] = SensorCommandType.Stop,
            [SensorCommandType.Stop] = SensorCommandType.Start,

            [SensorCommandType.Enable] = SensorCommandType.Disable,
            [SensorCommandType.Disable] = SensorCommandType.Enable,

            [SensorCommandType.Open] = SensorCommandType.Close,
            [SensorCommandType.Close] = SensorCommandType.Open,

            [SensorCommandType.Lock] = SensorCommandType.Unlock,
            [SensorCommandType.Unlock] = SensorCommandType.Lock,

            [SensorCommandType.Activate] = SensorCommandType.Deactivate,
            [SensorCommandType.Deactivate] = SensorCommandType.Activate
        };

    // Commands that may be relevant to any warning.
    private static readonly HashSet<string> GeneralRelevantKeywords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "reset",
            "reboot",
            "restart",
            "stop",
            "shutdown",
            "turn off",
            "start",
            "start up",
            "turn on"
        };

    // Keywords specific to each telemetry warning.
    private static readonly IReadOnlyDictionary<string, HashSet<string>>
        WarningKeywords = new Dictionary<string, HashSet<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Temperature"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "temperature",
                "cooling",
                "cool",
                "fan",
                "ventilation",
                "overheat",
                "heater"
            },

            ["Voltage"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "voltage",
                "power supply",
                "power",
                "electrical supply",
                "voltage regulator"
            },

            ["Current"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "current",
                "load",
                "current limit",
                "motor load",
                "reduce load"
            }
        };

    /// <summary>
    /// All supported command types.
    /// </summary>
    public static IReadOnlyCollection<SensorCommandType> SupportedCommands =>
        CommandNames.Keys.ToArray();

    /// <summary>
    /// Gets the text sent to the sensor for a command type.
    /// </summary>
    public static string GetText(SensorCommandType command)
    {
        return CommandNames.TryGetValue(command, out var text)
            ? text
            : throw new ArgumentOutOfRangeException(
                nameof(command),
                command,
                "Unsupported sensor command.");
    }

    /// <summary>
    /// Determines whether the supplied text is a supported command.
    /// </summary>
    public static bool IsSupported(string? command)
    {
        return TryParse(command, out _);
    }

    /// <summary>
    /// Parses command text into its enum representation.
    /// </summary>
    public static bool TryParse(
        string? text,
        out SensorCommandType command)
    {
        command = default;

        return !string.IsNullOrWhiteSpace(text) &&
               CommandsByName.TryGetValue(text.Trim(), out command);
    }

    /// <summary>
    /// Gets the inverse command type, if one exists.
    /// </summary>
    public static SensorCommandType? GetInverseType(
        SensorCommandType command)
    {
        return InverseCommands.TryGetValue(command, out var inverse)
            ? inverse
            : null;
    }

    /// <summary>
    /// Gets the inverse command text, or null if no inverse exists.
    /// Accepts unsupported text safely.
    /// </summary>
    public static string? GetInverseCommand(string? command)
    {
        if (!TryParse(command, out var commandType))
            return null;

        var inverseType = GetInverseType(commandType);

        return inverseType.HasValue
            ? GetText(inverseType.Value)
            : null;
    }

    /// <summary>
    /// Checks whether a command is relevant to a telemetry warning.
    /// Matching is case-insensitive and supports partial keyword matches.
    /// </summary>
    public static bool IsCommandRelevant(
        string? command,
        string? warningType)
    {
        if (string.IsNullOrWhiteSpace(command) ||
            string.IsNullOrWhiteSpace(warningType))
        {
            return false;
        }

        // General commands can apply to any warning.
        if (ContainsAny(command, GeneralRelevantKeywords))
            return true;

        // Find the matching warning category.
        foreach (var warning in WarningKeywords)
        {
            if (!warningType.Contains(
                    warning.Key,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return ContainsAny(command, warning.Value);
        }

        return false;
    }

    private static bool ContainsAny(
        string value,
        IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (value.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
