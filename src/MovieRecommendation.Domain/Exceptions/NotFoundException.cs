using System.Net;

namespace MovieRecommendation.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException()
        : base(HttpStatusCode.NotFound, "The requested resource was not found")
    {
    }

    public NotFoundException(string message)
        : base(HttpStatusCode.NotFound, message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(HttpStatusCode.NotFound, message, innerException)
    {
    }
}
