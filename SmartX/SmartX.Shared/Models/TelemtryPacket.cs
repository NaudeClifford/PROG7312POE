using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Shared.Models
{
    public class TelemtryPacket
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string SensorId { get; set; } = string.Empty;

        public double Voltage { get; set; }

        public double Current { get; set; }

        public double Temperature { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
