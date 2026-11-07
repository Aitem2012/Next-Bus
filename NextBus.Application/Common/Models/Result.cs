using System.Collections.Generic;

namespace NextBus.Application.Common.Models
{
    public class Result
    {
        public bool Succeeded { get; init; }
        public string? Message { get; init; }
        public string[] Errors { get; init; } = [];

        public static Result Success(string? message = null) => new() { Succeeded = true, Message = message };
        public static Result Failure(string error) => new() { Succeeded = false, Errors = [error] };
        public static Result Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = [.. errors] };
    }

    public class Result<T>
    {
        public bool Succeeded { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public string[] Errors { get; init; } = [];

        public static Result<T> Success(T data, string? message = null) => new() { Succeeded = true, Data = data, Message = message };
        public static Result<T> Failure(string error) => new() { Succeeded = false, Errors = [error] };
        public static Result<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = [.. errors] };
    }
}

