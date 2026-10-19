namespace Gold.Domain.Common;

/// <summary>Thrown when a domain invariant is violated (invalid weight, karat, percentage, ...).</summary>
public sealed class DomainException(string message) : Exception(message);
