using System.Net;

namespace MovieRecommendation.Domain.Exceptions;

public class EmailNotVerifiedException : DomainException
{
    public EmailNotVerifiedException()
        : base(HttpStatusCode.Forbidden, "Email is not verified")
    {
    }

    public EmailNotVerifiedException(string message)
        : base(HttpStatusCode.Forbidden, message)
    {
    }

    public EmailNotVerifiedException(string message, Exception innerException)
        : base(HttpStatusCode.Forbidden, message, innerException)
    {
    }
}
