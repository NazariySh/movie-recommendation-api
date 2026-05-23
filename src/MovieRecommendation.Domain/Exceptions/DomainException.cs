using System.Net;

namespace MovieRecommendation.Domain.Exceptions;

public class DomainException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public DomainException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public DomainException(HttpStatusCode statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
