using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WalletWasabi.Logging;

namespace WalletWasabi.Backend.Middlewares;

public record ProcessedRequest(string Path, long Size);

public record ProcessedResponse(int StatusCode, long Size);

public class RequestResponseLoggingMiddleware
{
	private readonly RequestDelegate _next;

	public RequestResponseLoggingMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task Invoke(HttpContext context)
	{
		//First, get the incoming request
		var request = FormatRequest(context.Request);

		//Copy a pointer to the original response body stream
		var originalBodyStream = context.Response.Body;

		//Create a new memory stream...
		using var responseBody = new MemoryStream();
		//...and use that for the temporary response body
		context.Response.Body = responseBody;

		//Continue down the Middleware pipeline, eventually returning to this class
		await _next(context);

		//Format the response from the server
		var response = FormatResponse(context.Response);

		// Log WabiSabi related stuff:
		if (request.Path.Contains("wabisabi", StringComparison.OrdinalIgnoreCase))
		{
			Logger.LogDebug($"Request  [{request.Path}] [{request.Size} Byte] - Response [statuscode: {response.StatusCode}] [{response.Size} Byte]");
		}

		//Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
		await responseBody.CopyToAsync(originalBodyStream);
	}

	private static ProcessedRequest FormatRequest(HttpRequest request)
	{
		//This line allows us to set the reader for the request back at the beginning of its stream.
		request.EnableBuffering();

		// reset the stream position to 0, which is allowed because of EnableBuffering()
		request.Body.Seek(0, SeekOrigin.Begin);

		return new ProcessedRequest(request.Path, Convert.ToInt32(request.ContentLength));
	}

	private static ProcessedResponse FormatResponse(HttpResponse response)
	{
		//We need to read the response stream from the beginning...
		response.Body.Seek(0, SeekOrigin.Begin);

		//We need to reset the reader for the response so that the client can read it.
		response.Body.Seek(0, SeekOrigin.Begin);

		//Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
		return new ProcessedResponse(response.StatusCode, Convert.ToInt32(response.ContentLength));
	}
}
