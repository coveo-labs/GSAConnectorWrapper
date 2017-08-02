// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Coveo.Connectors.Utilities;

namespace GSAFeedPushConverter
{
    /// <summary>
    /// Class used to create web server that listen on the given URLs then forward the request to given methods.
    /// </summary>
    public class WebServer
    {
        private const string LISTENING_TO = "{1}:Web server is listening to : {0}";
        private readonly HttpListener m_Listener = new HttpListener();
        private readonly Func<HttpListenerRequest, FeedRequestResponse> m_ResponderMethod;
        private readonly Action<HttpListenerRequest> m_ProcessMethod;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_Prefixes">The URLs to listen to.</param>
        /// <param name="p_ResponderMethod">The method that will construct the response from the requests.</param>
        /// <param name="p_ProcessMethod">The method that will only get information from the requests.</param>
        public WebServer(string[] p_Prefixes,
            Func<HttpListenerRequest, FeedRequestResponse> p_ResponderMethod,
            Action<HttpListenerRequest> p_ProcessMethod)
        {
            if (!HttpListener.IsSupported) {
                throw new NotSupportedException("Http Listener is unsupported on this machine.");
            }

            if (p_Prefixes == null || p_Prefixes.Length == 0) {
                throw new ArgumentException(nameof(p_Prefixes));
            }

            if (p_ResponderMethod == null) {
                throw new ArgumentException(nameof(p_ResponderMethod));
            }

            foreach (string prefix in p_Prefixes) {
                m_Listener.Prefixes.Add(prefix);
            }

            m_ResponderMethod = p_ResponderMethod;
            m_ProcessMethod = p_ProcessMethod;
            m_Listener.Start();
        }

        /// <summary>
        /// Constructor params as the prefixes.
        /// </summary>
        /// <param name="p_ResponderMethod">The method that will construct the response from the requests.</param>
        /// <param name="p_ProcessMethod">The method that will only get information from the requests.</param>
        /// <param name="p_Prefixes">The URLs to listen to.</param>
        public WebServer(Func<HttpListenerRequest, FeedRequestResponse> p_ResponderMethod,
            Action<HttpListenerRequest> p_ProcessMethod,
            params string[] p_Prefixes)
            : this(p_Prefixes, p_ResponderMethod, p_ProcessMethod)
        {
        }

        /// <summary>
        /// Starting the listening of the server.
        /// </summary>
        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o => {
                Console.WriteLine("Registering...");
                m_Listener.Prefixes.ForEach(prefix => Console.WriteLine("> {0}", prefix));
                Console.WriteLine();
                ConsoleUtilities.WriteLine(LISTENING_TO, ConsoleColor.Green, m_Listener.Prefixes.First(), DateTime.Now);
                Console.WriteLine();

                while (m_Listener.IsListening) {
                    try {
                        ThreadPool.QueueUserWorkItem(listenerContext => {
                            HttpListenerContext context = listenerContext as HttpListenerContext;
                            try {
                                if (context != null) {
                                    //In the case of the GSA Mock, we do not have a process method.
                                    try {
                                        m_ProcessMethod?.Invoke(context.Request);
                                    } catch (Exception exception) {
                                        Console.WriteLine(exception);
                                    }

                                    FeedRequestResponse response = m_ResponderMethod(context.Request);
                                    context.Response.ContentLength64 = response.Message.Length;
                                    context.Response.OutputStream.Write(new UTF8Encoding(true).GetBytes(response.Message), 0, response.Message.Length);
                                    context.Response.StatusCode = (int) response.HttpStatusCode;

                                    Console.WriteLine();
                                    ConsoleUtilities.WriteLine(LISTENING_TO, ConsoleColor.Green, m_Listener.Prefixes.First(), DateTime.Now);
                                    Console.WriteLine();
                                }
                            } finally {
                                context?.Response.OutputStream.Close();
                            }
                        }, m_Listener.GetContext());
                    } catch (Exception exception) {
                        Console.WriteLine(exception);
                    }
                }
            });
        }

        /// <summary>
        /// Stop the listening for request
        /// </summary>
        public void Stop()
        {
            m_Listener.Stop();
            m_Listener.Close();
        }
    }
}
