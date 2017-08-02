// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace GSAFeedPushConverter
{
    /// <summary>
    /// Container for the all the configuration needed to run the tool.
    /// </summary>
    public class Configuration
    {
        private const string LISTENNING_HOST = "localhost";
        private const string LISTENING_URL_FEED = "http://" + LISTENNING_HOST + ":19900/xmlfeed/";
        private const string LISTENING_URL_GROUPS = "http://" + LISTENNING_HOST + ":19900/xmlgroups/";
        private const string GSA_AUTH_URL = "http://" + LISTENNING_HOST + ":8000/accounts/ClientLogin/";

        private const bool DELETE_ON_INVALID_URL = true;
        private const bool REQUIRE_DISPLAY_URL = false;
        private const bool PUSH_RECORDS_WITHOUT_ACL = false;

        [JsonProperty(PropertyName = "PLATFORM_API_ENDPOINT")]
        public string PlatformApiEndpointUrl { get; set; } // = PLATFORM_API_ENDPOINT;

        [JsonProperty(PropertyName = "PUSH_API_ENDPOINT")]
        public string PushApiEndpointUrl { get; set; } // = PUSH_API_ENDPOINT;

        [JsonProperty(PropertyName = "ORGANIZATION_ID")]
        public string OrganizationId { get; set; } //= ORGANIZATION_ID;

        [JsonProperty(PropertyName = "PROVIDER_ID")]
        public string ProviderId { get; set; } //= PROVIDER_ID;

        [JsonProperty(PropertyName = "API_KEY")]
        public string ApiKey { get; set; } //= API_KEY;

        [JsonProperty(PropertyName = "PUSH_SOURCE_ID")]
        public string PushSourceId { get; set; } //= PUSH_SOURCE_ID;

        [JsonProperty(PropertyName = "PUSH_SOURCE_DICTIONNARY")]
        public Dictionary<string, string> DataSourceToSourceId { get; set; } = new Dictionary<string, string>();

        [JsonProperty(PropertyName = "LISTENING_URL_FEED")]
        public string ListeningUrlFeed { get; set; } = LISTENING_URL_FEED;

        [JsonProperty(PropertyName = "LISTENING_URL_GROUPS")]
        public string ListeningUrlGroups { get; set; } = LISTENING_URL_GROUPS;

        [JsonProperty(PropertyName = "GSA_AUTH_URL")]
        public string GsaMockAuth { get; set; } = GSA_AUTH_URL;

        [JsonProperty(PropertyName = "DELETE_ON_INVALID_URL")]
        public bool DeleteOnInvalidUrl = DELETE_ON_INVALID_URL;

        [JsonProperty(PropertyName = "REQUIRE_DISPLAY_URL")]
        public bool RequireDisplayUrl = REQUIRE_DISPLAY_URL;

        //This option has not been tested. The tested connectors always had ACL with the records.
        [JsonProperty(PropertyName = "PUSH_RECORDS_WITHOUT_ACL")]
        public bool PushRecordsWithoutAcl = PUSH_RECORDS_WITHOUT_ACL;

        /// <summary>
        /// Return the list of missing parameters.
        /// </summary>
        /// <returns>List of missing parameters.</returns>
        public List<string> ValidateConfig()
        {
            List<string> missingParameters = new List<string>();
            foreach (PropertyInfo propertyInfo in this.GetType().GetProperties()) {
                if (propertyInfo.PropertyType == typeof(string)) {
                    if (string.IsNullOrEmpty((string) propertyInfo.GetValue(this))) {
                        missingParameters.Add(propertyInfo.Name);
                    }
                }
            }

            return missingParameters;
        }
    }
}
