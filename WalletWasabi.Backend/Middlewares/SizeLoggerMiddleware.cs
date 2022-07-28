using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NBitcoin;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.WabiSabi;

namespace WalletWasabi.Backend.Middlewares;

public record ProcessedRequest(string Path, long Size);

public record ProcessedResponse(long Size);

/// <summary>
/// https://github.com/exceptionnotfound/AspNetCoreRequestResponseMiddlewareDemo
/// </summary>
public class RequestResponseLoggingMiddleware
{
	private readonly RequestDelegate _next;

	private readonly string _measurementFilePath = Path.Combine(EnvironmentHelpers.GetDataDir(Path.Combine("WalletWasabi", "Backend")),"measurement.csv");
	private readonly string _fileHeader = @$"MaxRegistrableAmount (BTC),API,request size (Byte),response size (Byte){Environment.NewLine}";

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
		var response = await FormatResponse(context.Response);

		// Log WabiSabi related stuff:
		if (request.Path.Contains("wabisabi", StringComparison.OrdinalIgnoreCase))
		{
			var maxSuggestedAmount = Money.Satoshis(ProtocolConstants.MaxAmountPerAlice);
			Logger.LogDebug($"{nameof(ProtocolConstants.MaxAmountPerAlice)}:{maxSuggestedAmount.ToString(false)} BTC, [{request.Path}] Request[{request.Size} Byte] Response[{response.Size} Byte]");

			if (!File.Exists(_measurementFilePath))
			{
				await File.WriteAllTextAsync(_measurementFilePath, _fileHeader);
			}

			var csv = $"{maxSuggestedAmount.ToString(false)},{request.Path.Split("/").Last()},{request.Size},{response.Size}{Environment.NewLine}";
			await File.AppendAllTextAsync(_measurementFilePath, csv);
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

	private async Task<ProcessedResponse> FormatResponse(HttpResponse response)
	{
		//We need to read the response stream from the beginning...
		response.Body.Seek(0, SeekOrigin.Begin);

		string text = await new StreamReader(response.Body).ReadToEndAsync();
		var size = Encoding.Unicode.GetByteCount(text);

		//We need to reset the reader for the response so that the client can read it.
		response.Body.Seek(0, SeekOrigin.Begin);

		return new ProcessedResponse(size);
	}
}
