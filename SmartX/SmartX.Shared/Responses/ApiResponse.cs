using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Shared.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public T? Data { get; init; }

        public List<string> Errors { get; init; } = new();
    }
}
