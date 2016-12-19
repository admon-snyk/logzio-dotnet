using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Logzio.DotNet.Core.WebClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Logzio.DotNet.Core.Shipping
{
	public interface IBulkSender
	{
		Task SendAsync(ICollection<LogzioLoggingEvent> logz);
	}

	public class BulkSender : IBulkSender
	{
		private const string LogzioHost = "listener.logz.io";
		private const string HttpUrlTemplate = "http://" + LogzioHost + ":8070/?token={0}&type={1}";
		private const string HttpsUrlTemplate = "https://" + LogzioHost + ":8071/?token={0}&type={1}";

		public IWebClientFactory WebClientFactory = new WebClientFactory();

		private readonly BulkSenderOptions _options;
		private readonly JsonSerializer _jsonSerializer;

		public BulkSender(BulkSenderOptions options)
		{
			_options = options;
			_jsonSerializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };
		}

		public Task SendAsync(ICollection<LogzioLoggingEvent> logz)
		{
			return Task.Run(() => Send(logz));
		}

		private void Send(ICollection<LogzioLoggingEvent> logz, int attempt = 0)
		{
			var url = string.Format(_options.IsSecured ? HttpsUrlTemplate : HttpUrlTemplate, _options.Token, _options.Type);
			try
			{
				using (var client = WebClientFactory.GetWebClient())
				{
					var serializedLogLines = logz.Select(x => Serialize(x.LogData)).ToArray();
					var body = String.Join(Environment.NewLine, serializedLogLines);
					client.UploadString(url, body);
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Logzio.DotNet ERROR: " + ex);

				if (attempt < _options.RetriesMaxAttempts - 1)
					Task.Delay(_options.RetriesInterval).ContinueWith(task => Send(logz, attempt + 1));
			}
		}

		private string Serialize(object obj)
		{
			using (var sb = new StringWriter())
			{
				_jsonSerializer.Serialize(sb, obj);
				return sb.ToString();
			}
		}
	}
}