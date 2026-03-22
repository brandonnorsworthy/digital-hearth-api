namespace DigitalHearth.Api.Services;

public enum ServiceResultStatus
{
    Ok,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
}

public class ServiceResult
{
    public ServiceResultStatus Status { get; init; }
    public string? Error { get; init; }

    public bool IsSuccess => Status == ServiceResultStatus.Ok;

    public static ServiceResult Ok() => new() { Status = ServiceResultStatus.Ok };
    public static ServiceResult BadRequest(string error) => new() { Status = ServiceResultStatus.BadRequest, Error = error };
    public static ServiceResult Unauthorized(string error) => new() { Status = ServiceResultStatus.Unauthorized, Error = error };
    public static ServiceResult Forbidden() => new() { Status = ServiceResultStatus.Forbidden };
    public static ServiceResult NotFound(string error) => new() { Status = ServiceResultStatus.NotFound, Error = error };
    public static ServiceResult Conflict(string error) => new() { Status = ServiceResultStatus.Conflict, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Ok(T value) => new() { Status = ServiceResultStatus.Ok, Value = value };
    public new static ServiceResult<T> BadRequest(string error) => new() { Status = ServiceResultStatus.BadRequest, Error = error };
    public new static ServiceResult<T> Unauthorized(string error) => new() { Status = ServiceResultStatus.Unauthorized, Error = error };
    public new static ServiceResult<T> Forbidden() => new() { Status = ServiceResultStatus.Forbidden };
    public new static ServiceResult<T> NotFound(string error) => new() { Status = ServiceResultStatus.NotFound, Error = error };
    public new static ServiceResult<T> Conflict(string error) => new() { Status = ServiceResultStatus.Conflict, Error = error };
}
