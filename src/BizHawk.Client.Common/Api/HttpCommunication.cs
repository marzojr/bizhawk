using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public sealed class HttpCommunication
	{
		private const string MIME_FORM_URLENC = "application/x-www-form-urlencoded";

		public static HttpContent ContentObjectFor(string payload, string mimeType)
			=> mimeType switch
			{
				MIME_FORM_URLENC => new FormUrlEncodedContent([ new("payload", payload) ]),
#pragma warning disable BHI1005 // exception type
				_ => throw new NotImplementedException()
#pragma warning restore BHI1005
			};

		private readonly HttpClient _client = new HttpClient();

		private readonly Func<byte[]> _takeScreenshotCallback;

		public int DefaultTimeout { get; set; } = 500;

		public string GetUrl;

		public string PostUrl;

		public int Timeout { get; set; }

		public int ExpectContinueThreshold { get; set; } = 1024 * 1024; // 1MB

		public HttpCommunication(Func<byte[]> takeScreenshotCallback, string getURL, string postURL)
		{
			_takeScreenshotCallback = takeScreenshotCallback;
			GetUrl = getURL;
			PostUrl = postURL;
			_client.DefaultRequestHeaders.UserAgent.ParseAdd(VersionInfo.UserAgentEscaped);
		}

		public string ExecGet(string url = null) => Get(url ?? GetUrl).Result;

		/// <inheritdoc cref="ExecPostAsForm"/>
		[Obsolete($"renamed to {nameof(ExecPostAsForm)}")]
		public string ExecPost(string url = null, string payload = "")
			=> ExecPostAsForm(url: url, payload: payload);

#pragma warning disable DOC105 // false positive detecting param name in doc comment
		/// <remarks>
		/// uses <see cref="FormUrlEncodedContent"/> (<c>application/x-www-form-urlencoded</c>),
		/// containing a single pair with key <c>payload</c> (literal) and value <paramref name="payload"/> (argument)
		/// </remarks>
		public string ExecPostAsForm(string url = null, string payload = "")
		{
			return Post(
				url ?? PostUrl,
				ContentObjectFor(payload: payload, mimeType: MIME_FORM_URLENC),
				sendAdvanceRequest: payload.Length >= ExpectContinueThreshold
			).Result;
		}
#pragma warning restore DOC105

		public string ExecPostRaw(string url = null, string payload = "", string contentType = "text/plain")
		{
			return Post(
				url ?? PostUrl,
				new StringContent(payload, Encoding.UTF8, contentType),
				payload.Length > ExpectContinueThreshold
			).Result;
		}

		public async Task<string> Get(string url)
		{
			_client.DefaultRequestHeaders.ConnectionClose = false;
			_client.DefaultRequestHeaders.ExpectContinue = false;
			var response = await _client.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsStringAsync();
			}
			return null;
		}

		public async Task<string> Post(string url, HttpContent content, bool sendAdvanceRequest = false)
		{
			_client.DefaultRequestHeaders.ConnectionClose = true;
			_client.DefaultRequestHeaders.ExpectContinue = sendAdvanceRequest;
			HttpResponseMessage response;
			try
			{
				response = await _client.PostAsync(url, content).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				return e.ToString();

			}
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			return await response.Content.ReadAsStringAsync();
		}

		public string SendScreenshot(string url = null, string parameter = "screenshot")
		{
			var url1 = url ?? PostUrl;
			var content = new FormUrlEncodedContent(new Dictionary<string, string> { [parameter] = Convert.ToBase64String(_takeScreenshotCallback()) });
			Task<string> postResponse = null;
			var trials = 5;
			while (postResponse == null && trials > 0)
			{
				postResponse = Post(url1, content);
				trials -= 1;
			}
			return postResponse?.Result;
		}

		public void SetTimeout(int timeout)
		{
			if (Timeout == 0 && timeout == 0)
			{
				Timeout = DefaultTimeout;
			}

			if (timeout != 0)
			{
				_client.Timeout = new TimeSpan(0, 0, 0, timeout / 1000, timeout % 1000);
				Timeout = timeout;
			}
		}
	}
}
