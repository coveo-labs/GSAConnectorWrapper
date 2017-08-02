// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System.Net;

namespace GSAFeedPushConverter
{
    /// <summary>
    /// Class used to encapsulate the responses to the web server.
    /// </summary>
    public class FeedRequestResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; }

        public string HttpStatusDescription => HttpStatusCode.ToString();

        public string Message { get; set; }
    }
}
