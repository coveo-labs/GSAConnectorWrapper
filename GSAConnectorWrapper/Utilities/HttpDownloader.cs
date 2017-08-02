// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.IO;
using System.Threading;
using Coveo.Connectors.Utilities.PushApiSdk.Response;
using Coveo.Connectors.Utilities.Rest.Http;
using Coveo.Connectors.Utilities.Rest.Request;
using Coveo.Connectors.Utilities.Rest.Response;

namespace GSAFeedPushConverter.Utilities
{
    /// <summary>
    /// Used to download content from the web.
    /// </summary>
    public class HttpDownloader : IDisposable
    {
        private readonly IHttpRequestManager m_HttpRequestManager;

        /// <summary>
        /// Constructor. Initialize internal data.
        /// </summary>
        public HttpDownloader()
        {
            m_HttpRequestManager = new HttpRequestManager(new HttpClientWrapper(120), new HttpResponseParser());
        }

        /// <summary>
        /// Download the response of the specify URL and only return the body of the response as a string.
        /// </summary>
        /// <param name="p_Url">The URL to download the content.</param>
        /// <returns>String containing the body.</returns>
        public string DownloadContent(string p_Url)
        {
            return m_HttpRequestManager.Execute<string, string>(new RestRequest(new Uri(p_Url)), CancellationToken.None).ResponseContent;
        }

        /// <summary>
        /// Download the response of the specify URL and return the complete response.
        /// </summary>
        /// <param name="p_Url">URL of the request.</param>
        /// <returns>The response to the request.</returns>
        public IHttpRestResponse<Stream, string> CompleteDownload(string p_Url)
        {
            return m_HttpRequestManager.Execute<Stream, string>(new RestRequest(new Uri(p_Url)), CancellationToken.None);
        }

        /// <summary>
        /// Dispose the download manager.
        /// </summary>
        public void Dispose()
        {
            m_HttpRequestManager?.Dispose();
        }
    }
}
