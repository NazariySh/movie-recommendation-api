using System.Net;

namespace MovieRecommendation.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException()
        : base(HttpStatusCode.Forbidden, "Forbidden")
    {
    }

    public ForbiddenException(string message)
        : base(HttpStatusCode.Forbidden, message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(HttpStatusCode.Forbidden, message, innerException)
    {
    }
}
