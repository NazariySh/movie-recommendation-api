using System.Net;

namespace MovieRecommendation.Domain.Exceptions;

public class LockedException : DomainException
{
    public LockedException()
        : base(HttpStatusCode.Locked, "Account is locked")
    {
    }

    public LockedException(string message)
        : base(HttpStatusCode.Locked, message)
    {
    }

    public LockedException(string message, Exception innerException)
        : base(HttpStatusCode.Locked, message, innerException)
    {
    }
}
