namespace MES.Web.Services;

public sealed class CookieForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var cookie = httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        var requestScheme = httpContextAccessor.HttpContext?.Request.Scheme;
        if (!string.IsNullOrWhiteSpace(requestScheme))
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", requestScheme);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
