using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Shared.Models
{
    public class Sensor
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        //[BsonElement("sensor_name")]
        public string Name { get; set; } = string.Empty;
        //[BsonIgnore]
        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string MacAdress { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
