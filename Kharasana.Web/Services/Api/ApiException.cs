using System;
using System.Net;

namespace Kharasana.Web.Services.Api
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public string? ResponseBody { get; }

        public ApiException(HttpStatusCode statusCode, string? responseBody = null)
            : base($"API returned status code {(int)statusCode}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}