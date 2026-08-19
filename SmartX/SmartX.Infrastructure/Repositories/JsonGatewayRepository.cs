using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories
{
    public class JsonGatewayRepository : IGatewayRepository
    {
        private readonly string _filePath;

        public JsonGatewayRepository()
        {
            _filePath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "Local", "gateway.json");
        }

        public async Task<Gateway?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var gateways = await GetAllAsync(cancellationToken);

            return gateways.FirstOrDefault(x => x.Id == id);
        }

        public async Task<IReadOnlyList<Gateway>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_filePath)) return [];

            string json = await File.ReadAllTextAsync(
                _filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json)) return [];

            var gateways = JsonSerializer.Deserialize<List<Gateway>>(json);

            return gateways ?? [];
        }

        public async Task AddAsync(
            Gateway gateway,
            CancellationToken cancellationToken = default)
        {
            var gateways = await GetAllAsync(cancellationToken);

            var gatewayList = gateways.ToList();

            gatewayList.Add(gateway);

            string json = JsonSerializer.Serialize(gatewayList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

        public async Task UpdateAsync(
            Gateway gateway,
            CancellationToken cancellationToken = default)
        {
            var gateways = await GetAllAsync(cancellationToken);

            var gatewayList = gateways.ToList();

            var existingSensor = gatewayList.FirstOrDefault(
                x => x.Id == gateway.Id);

            if (existingSensor is null) return;

            var index = gatewayList.IndexOf(existingSensor);

            gatewayList[index] = gateway;

            string json = JsonSerializer.Serialize(gatewayList,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var gateways = await GetAllAsync(cancellationToken);

            var gatewayList = gateways.ToList();

            var gateway = gatewayList.FirstOrDefault(x => x.Id == id);

            if (gateway is null) return;

            gatewayList.Remove(gateway);

            string json = JsonSerializer.Serialize(gatewayList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }
    }
}
