using AutoMapper;
using SmartX.Shared.Models;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Sensors;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Application.Queries.Sensors
{
    public class GetSensorByIdHandler
    {
        private readonly IMapper _mapper;
        private readonly ISensorRepository _sensorRepository;

        public GetSensorByIdHandler(ISensorRepository sensorRepository,
            IMapper mapper)
        {
            _sensorRepository = sensorRepository;
            _mapper = mapper;
        }

        public async Task<Result<SensorDto?>> HandleAsync(
            GetSensorByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var sensor = await _sensorRepository.GetByIdAsync(query.SensorId,
                cancellationToken);

            if (sensor is null)
            {
                return Result<SensorDto>.Fail("Sensor not found.");
            }

            var dto = _mapper.Map<SensorDto>(sensor);

            return Result<SensorDto>.Ok(dto);
        }
    }
}
