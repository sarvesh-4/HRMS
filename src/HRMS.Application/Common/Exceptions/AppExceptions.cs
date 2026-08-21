namespace HRMS.Application.Common.Exceptions;

/// <summary>404 — entity was not found.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.") { }
}

/// <summary>400 — request failed validation / business rules.</summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

/// <summary>409 — conflicts with existing data (e.g. duplicate email).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>401 — invalid credentials.</summary>
public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>403 — authenticated but not allowed to perform this action on this resource.</summary>
public class ForbiddenAppException : Exception
{
    public ForbiddenAppException(string message) : base(message) { }
}
