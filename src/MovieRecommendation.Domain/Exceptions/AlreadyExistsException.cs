using System.Net;

namespace MovieRecommendation.Domain.Exceptions;

public class AlreadyExistsException : DomainException
{
    public AlreadyExistsException()
        : base(HttpStatusCode.Conflict, "The resource already exists")
    {
    }

    public AlreadyExistsException(string message)
        : base(HttpStatusCode.Conflict, message)
    {
    }

    public AlreadyExistsException(string message, Exception innerException)
        : base(HttpStatusCode.Conflict, message, innerException)
    {
    }
}
