//    #[license]
//    SmartsheetClient SDK for C#
//    %%
//    Copyright (C) 2014 SmartsheetClient
//    %%
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//        
//            http://www.apache.org/licenses/LICENSE-2.0
//        
//    Unless required by applicable law or agreed To in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//    %[license]

using System.Net.Http;
using System.Net.Http.Headers;

namespace Smartsheet.Api.Internal.Http
{
    using System.Collections.Generic;
    using Util = Api.Internal.Utility.Utility;
	using System;
	using System.IO;
	using System.Net;
	using System.Linq;
	using System.Reflection;
	using System.Diagnostics;
	using System.Text;
	using NLog;

	/// <summary>
	/// This is the RestSharp based HttpClient implementation.
	/// 
	/// Thread Safety: This class is thread safe because it is immutable and the underlying http client is
	/// thread safe.
	/// </summary>

	public class DefaultHttpClient : HttpClient
	{
		/// <summary>
		/// Represents the underlying http client.
		/// 
		/// It will be initialized in constructor and will not change afterwards.
		/// </summary>
        private readonly System.Net.Http.HttpClient _httpClient;

		/// <summary>
		/// static logger 
		/// </summary>
		private static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Callback to determine if this request can be retried.
		/// </summary>
		private ShouldRetryCallback shouldRetryCallback;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="previousAttempts"></param>
		/// <param name="totalElapsedTime"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		private static bool DefaultShouldRetry(int previousAttempts, long totalElapsedTime, HttpResponse response)
		{
			return false;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DefaultHttpClient()
			: this(new System.Net.Http.HttpClient(), DefaultShouldRetry)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DefaultHttpClient(ShouldRetryCallback shouldRetryCallback)
			: this(new System.Net.Http.HttpClient(), shouldRetryCallback)
		{
		}

        /// <summary>
        /// Constructor.
        /// 
        /// Parameters: - HttpClient : the http client to use
        /// 
        /// Exceptions: - IllegalArgumentException : if any argument is null
        /// </summary>
        /// <param name="httpClient"> the http client </param>
        /// <param name="shouldRetryCallback">User-supplied retry implementation</param>
        public DefaultHttpClient(System.Net.Http.HttpClient httpClient, ShouldRetryCallback shouldRetryCallback)
		{
			Util.ThrowIfNull(httpClient);

            _httpClient = new System.Net.Http.HttpClient();
		    _httpClient.DefaultRequestHeaders.Add("User-Agent", buildUserAgent());

			this.shouldRetryCallback = shouldRetryCallback;
		}

		/// <summary>
		/// Make a multipart HTTP request and return the response.
		/// </summary>
		/// <param name="smartsheetRequest"> the Smartsheet request </param>
		/// <param name="file">the full file path</param>
		/// <param name="fileType">the file type, or also called the conent type of the file</param>
		/// <param name="objectType">the object name, for example 'comment', or 'discussion'</param>
		/// <returns> the HTTP response </returns>
		/// <exception cref="HttpClientException"> the HTTP client exception </exception>
		public virtual HttpResponse Request(HttpRequest smartsheetRequest, string objectType, string file, string fileType)
		{
			Util.ThrowIfNull(smartsheetRequest);
			if (smartsheetRequest.Uri == null)
			{
				throw new ArgumentException("A Request URI is required.");
			}

			var request = CreateRequest(smartsheetRequest);

		    var byteArrayContent = new ByteArrayContent(File.ReadAllBytes(file));
		    byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(fileType);

		    var content = new MultipartFormDataContent
		    {
		        request.Content,
		        {byteArrayContent, "\"file\"", $"\"{new FileInfo(file).Name}\""}
		    };

		    request.Content = content;
            
			return CreateSmartsheetResponse(request);
		}

	    // Create HTTP request based on the smartsheetRequest request Type
	    private static HttpRequestMessage CreateRequest(HttpRequest smartsheetRequest)
	    {
	        var mapping = new Dictionary<HttpMethod?, System.Net.Http.HttpMethod>
	        {
	            {HttpMethod.GET, System.Net.Http.HttpMethod.Get},
	            {HttpMethod.POST, System.Net.Http.HttpMethod.Post},
	            {HttpMethod.PUT, System.Net.Http.HttpMethod.Put},
	            {HttpMethod.DELETE, System.Net.Http.HttpMethod.Delete}
	        };

	        if (!mapping.ContainsKey(smartsheetRequest.Method))
	        {
	            throw new NotSupportedException("Request method " + smartsheetRequest.Method + " is not supported!");
	        }

	        var request = new HttpRequestMessage(mapping[smartsheetRequest.Method], smartsheetRequest.Uri);

	        if (smartsheetRequest.Entity?.GetContent() != null)
	        {
	            request.Headers.Add("Accept", "application/json");
	            request.Content = new StringContent(
	                Encoding.Default.GetString(smartsheetRequest.Entity.Content),
	                Encoding.UTF8,
	                "application/json"
	            );
	        }

            // Set HTTP Headers
            if (smartsheetRequest.Headers != null)
	        {
	            foreach (var header in smartsheetRequest.Headers)
	            {
	                request.Headers.Add(header.Key, header.Value);
	            }
	        }

	        return request;
	    }

	    private HttpResponse CreateSmartsheetResponse(HttpRequestMessage restRequest)
	    {
	        // Set the client base Url.
            //httpClient.BaseUrl = httpClientBaseUrl;

	        Stopwatch timer = new Stopwatch();
	        timer.Start();

            // Make the HTTP request
	        var restResponse = _httpClient.SendAsync(restRequest).Result;

            timer.Stop();

	        LogRequest(restRequest, restResponse, timer.ElapsedMilliseconds);

	        restResponse.EnsureSuccessStatusCode();
           
	        HttpResponse smartsheetResponse = new HttpResponse
	        {
	            Headers = restResponse.Headers.ToDictionary(x => x.Key, x => string.Join(",", x.Value)),
	            StatusCode = restResponse.StatusCode
	        };

	        // Set returned entities
	        if (restResponse.Content != null)
	        {
	            smartsheetResponse.Entity = new HttpEntity
	            {
	                ContentType = restResponse.Content.Headers.ContentType.MediaType,
	                ContentLength = restResponse.Content.Headers.ContentLength.Value,
	                Content = restResponse.Content.ReadAsByteArrayAsync().Result
	            };
	        }

	        return smartsheetResponse;
	    }

	    /// <summary>
		/// Make an HTTP request and return the response.
		/// </summary>
		/// <param name="smartsheetRequest"> the Smartsheet request </param>
		/// <returns> the HTTP response </returns>
		/// <exception cref="HttpClientException"> the HTTP client exception </exception>
		public virtual HttpResponse Request(HttpRequest smartsheetRequest)
		{
			Util.ThrowIfNull(smartsheetRequest);

            if (smartsheetRequest.Uri == null)
			{
				 throw new ArgumentException("A Request URI is required.");
			}

			int attempt = 0;
			HttpResponse smartsheetResponse;

			Stopwatch totalElapsed = new Stopwatch();
			totalElapsed.Start();

			while (true)
			{
                // TODO move up
			    var restRequest = CreateRequest(smartsheetRequest);
                
			    smartsheetResponse = CreateSmartsheetResponse(restRequest);

                if (smartsheetResponse.StatusCode == HttpStatusCode.OK)
				{
					break;
				}

				if (!shouldRetryCallback(++attempt, totalElapsed.ElapsedMilliseconds, smartsheetResponse))
				{
					break;
				}
			}
			return smartsheetResponse;
		}

		/// <summary>
		/// Close the HttpClient.
		/// </summary>
		public virtual void Close()
		{
			LogManager.Flush();
		}

		/// <summary>
		/// Release connection - not currently used.
		/// </summary>
		public virtual void ReleaseConnection()
		{
			// Not necessary with restsharp
		}

		private string buildUserAgent()
		{
			// Set User Agent
			string thisVersion = "";
			string title = "";
			Assembly assembly = Assembly.GetEntryAssembly();
			if (assembly != null)
			{
				thisVersion = assembly.GetName().Version.ToString();
				title = assembly.GetName().Name;
			}
			return Uri.EscapeDataString($"smartsheet-csharp-sdk({title})/{thisVersion}");
		}

		/// <summary>
		/// Log URL and response code to INFO, message bodies to DEBUG
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="durationMs"></param>
        private void LogRequest(HttpRequestMessage request, HttpResponseMessage response, long durationMs)
        {
			logger.Info(() => string.Format("{0} {1}, Response Code:{2}, Request completed in {3} ms", 
			    request.Method.ToString(), request.RequestUri.ToString(), response.StatusCode, durationMs));
            logger.Debug(() =>
            {
                StringBuilder builder = new StringBuilder();

                foreach (var header in request.Headers)
				{
				    if (header.Key == "Authorization")
				    {
				        builder
                            .Append("\"" + header.Key + "\"")
                            .Append(":")
                            .Append("\"[Redacted]\" ");
				    }
				    else
				    {
				        builder
                            .Append("\"" + header.Key + "\"")
                            .Append(":")
                            .Append("\"" + header.Value + "\" ");
				    }
				}

                string body = request.Content.ReadAsStringAsync().Result ?? "N/A";
                
                return string.Format("Request Headers {0} Request Body: {1}", builder.ToString(), body);
			});
			logger.Debug(() =>
			{
				var builder = new StringBuilder();

                foreach (var header in response.Headers)
				{
					builder
                        .Append("\"" + header.Key + "\"")
                        .Append(":")
                        .Append("\"" + header.Value + "\" ");
				}

                return string.Format("Response Headers: {0}Response Body: {1}", builder.ToString(), response.Content.ToString());
            });
        }
    }
}