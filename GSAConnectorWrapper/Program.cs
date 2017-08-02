// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.IO;

namespace GSAFeedPushConverter
{
    internal class Program
    {
        private const string DEFAULT_CONFIG_PATH = "Samples\\defaultconfig.json";

        private static void Main(string[] p_Args)
        {
            //First of all, we load the configuration file.
            string errorMessage = null;
            string filePath = "";
            if (p_Args.Length > 0) {
                filePath = p_Args[0];
                if (!File.Exists(filePath)) {
                    errorMessage = "The given configuration file path is invalid";
                }
            } else {
                errorMessage = "No configuration file path given";
            }
            if (errorMessage != null) {
                Console.WriteLine("{0}, returning to the default file ({1}).", errorMessage, DEFAULT_CONFIG_PATH);
                filePath = DEFAULT_CONFIG_PATH;
                if (!File.Exists(DEFAULT_CONFIG_PATH)) {
                    //We cannot work without a configuration file.
                    Console.WriteLine("The default configuration file is missing, closing now.");
                    return;
                }
            }

            ConnectorWrapper wrapper = new ConnectorWrapper(filePath);
            try {
                wrapper.Start();
            } catch (Exception exception) {
                Console.WriteLine(exception.Message);
            }
            Console.ReadLine();
            wrapper.Stop();
        }
    }
}
