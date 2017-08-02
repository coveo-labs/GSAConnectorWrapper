// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Web;
using Coveo.Connectors.Utilities;
using Coveo.Connectors.Utilities.Rest.Response;
using GSAFeedPushConverter.Model;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace GSAFeedPushConverter.Utilities
{
    /// <summary>
    /// Static class for parsing the web pages containing the records
    /// </summary>
    public static class GsaWebParser
    {
        /// <summary>
        /// Parse the pages starting at the given url to find all the records.
        /// </summary>
        /// <param name="p_UrlToParse">Url to start the parsing. All pages need to start with this url to be parse.</param>
        /// <param name="p_DeleteOnInvalidUrl">If this is true and the page returning anything else then a code 200/ok,
        ///  we construct a delete record with it.</param>
        /// <returns>The list of parsed records.</returns>
        public static List<GsaFeedRecord> ExtractRecordsFromUrl(string p_UrlToParse,
            bool p_DeleteOnInvalidUrl)
        {
            //we go recursively through all the links to find all the records
            //the links need to be in the domain
            return ExtractRecordsFromUrl(p_UrlToParse, p_UrlToParse, new List<string>(), p_DeleteOnInvalidUrl);
        }

        /// <summary>
        /// The inner method for ExtractRecordsFromUrl. Also receive the domain and the list of founds URL.
        /// </summary>
        /// <param name="p_UrlToParse">The URL to parse.</param>
        /// <param name="p_DomainToParse">The domain that we must stay in for the parsing.</param>
        /// <param name="p_FoundUrls">The list of founds URLs to keep track of already parsed URLs.</param>
        /// <param name="p_DeleteOnInvalidUrl">If we delete on codes different then 200/ok.</param>
        /// <returns>The list of founds URLs.</returns>
        private static List<GsaFeedRecord> ExtractRecordsFromUrl(string p_UrlToParse,
            string p_DomainToParse,
            List<string> p_FoundUrls,
            bool p_DeleteOnInvalidUrl)

        {
            //We add the page to the list of FoundsUrls
            p_FoundUrls.Add(p_UrlToParse);

            GsaFeedRecord urlRecord = ConstrucRecordFromDocument(p_UrlToParse);
            IHttpRestResponse<Stream, string> webResponse = urlRecord.GetWebContent();
            HtmlDocument doc = new HtmlDocument();
            //We need to make sure that we have content.
            if (webResponse.ResponseContent != null) {
                doc.LoadHtml(webResponse.ResponseContent);
            }

            List<GsaFeedRecord> allRecords = new List<GsaFeedRecord>();
            if (webResponse.HttpStatusCode == HttpStatusCode.OK) {
                allRecords.Add(urlRecord);
                if (doc.DocumentNode != null && doc.DocumentNode.HasChildNodes) {
                    //We find all href for the URLs
                    HtmlNodeCollection allLinks = doc.DocumentNode.SelectNodes("//a[@href]");
                    if (allLinks != null) {
                        foreach (HtmlNode link in allLinks) {
                            string urlInPage = BuildAbsoluteUrl(p_UrlToParse, link.GetAttributeValue("href", ""));
                            //If the url is in the domain and we did not already crawled it, we can crawl it.
                            if (urlInPage.StartsWith(p_DomainToParse) && !p_FoundUrls.Contains(urlInPage)) {
                                allRecords.AddRange(ExtractRecordsFromUrl(urlInPage, p_DomainToParse, p_FoundUrls, p_DeleteOnInvalidUrl));
                            }
                        }
                    }
                }
            } else {
                if (p_DeleteOnInvalidUrl) {
                    //We construct a record for the delete.
                    GsaFeedRecord recordToDelete = new GsaFeedRecord {
                        Action = GsaFeedRecordAction.Delete,
                        Url = p_UrlToParse
                    };
                    allRecords.Add(recordToDelete);
                }
                //We inform the user that we hit a invalid page.
                Console.WriteLine("Invalid URL ({0}). Code :{1}", p_UrlToParse, webResponse.HttpStatusCode);
            }
            return allRecords;
        }

        /// <summary>
        /// Construct a record from the given URL.
        /// </summary>
        /// <param name="p_Url">The URL used to construct the record.</param>
        /// <returns>The constructed record.</returns>
        private static GsaFeedRecord ConstrucRecordFromDocument(string p_Url)
        {
            GsaFeedRecord record = new GsaFeedRecord {
                Url = p_Url,
                //If we construct a Record from a document, that means that we need to add it.
                Action = GsaFeedRecordAction.Add
            };

            //We need to check if the header contain an ACL
            //Then we check for the record values (ex: lock, scoring, crawl_once, display_url, etc)
            //Finally we check for the extra metadata
            HttpResponseHeaders headers = record.GetWebContent().HttpHeaders;
            List<GsaFeedMeta> metadata = new List<GsaFeedMeta>();
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers) {
                //We define the correct action to take for each type of header value.
                Action<string> actionOnValues;
                switch (header.Key) {
                    case "X-gsa-doc-controls":
                        //can contain ACL and other normal value of a record.
                        //From the examples that we got, the values of doc controls contain only one element
                        actionOnValues = value => {
                            string[] keyAndTheValue = value.Split('=');
                            if (keyAndTheValue[0].ToLower() == "acl") {
                                record.Acl = ConstructAclFromEncodedJson(keyAndTheValue[1], p_Url);
                            } else if (!record.SetPropertyByName(keyAndTheValue[0], HttpUtility.UrlDecode(keyAndTheValue[1]))) {
                                ConsoleUtilities.WriteLine("Cannon find property in a record for: {0}", ConsoleColor.Red, keyAndTheValue[0]);
                            }
                        };
                        break;
                    case "X-gsa-external-metadata":
                        //ex: google%3Aobjecttype=Site,sharepoint%3Aparentwebtitle=mycollection
                        actionOnValues = value => {
                            string[] allValues = value.Split(',');
                            foreach (string metadatapair in allValues) {
                                string[] keyAndTheValue = metadatapair.Split('=');
                                metadata.Add(new GsaFeedMeta() { Content = HttpUtility.UrlDecode(keyAndTheValue[1]), Name = HttpUtility.UrlDecode(keyAndTheValue[0]) });
                            }
                        };
                        break;
                    case "Last-modified":
                        //ex: Fri, 20 Jan 2017 21:07:26 GMT
                        actionOnValues = value => record.LastModifiedString = value;
                        break;
                    case "X-gsa-serve-security":
                    //ex: secure
                    case "X-robots-tag":
                    //ex: noindex
                    case "Content-type":
                    //ex: application/x-msdownload, text/html; charset=UTF-8
                    default:
                        actionOnValues = value => {
                            metadata.Add(new GsaFeedMeta() {
                                Content = HttpUtility.UrlDecode(value),
                                Name = HttpUtility.UrlDecode(header.Key)
                            });
                        };
                        break;
                }
                //We execute the given action for each value of each header parameter.
                header.Value.ForEach(actionOnValues);
            }
            //We set the read metadata in the record.
            record.Metadata = new GsaFeedMetadata { Values = metadata };

            return record;
        }

        /// <summary>
        /// Transform the given URL in an absolute URL without any parameters.
        /// </summary>
        /// <param name="p_BaseUrl">The base URL, the page that contain the given URL.</param>
        /// <param name="p_ExtractLink">The URL to transform.</param>
        /// <returns>The resulting absolute URL.</returns>
        private static string BuildAbsoluteUrl(string p_BaseUrl,
            string p_ExtractLink)
        {
            Uri absoluteUri = p_ExtractLink.StartsWith(p_BaseUrl) ?
                new Uri(p_ExtractLink) : new Uri(new Uri(p_BaseUrl, UriKind.Absolute), p_ExtractLink);

            return absoluteUri.GetLeftPart(UriPartial.Path);
        }

        /// <summary>
        /// Construct the ACL from the given encoded Json with URL decode.
        /// </summary>
        /// <param name="p_Acl">The ACL in Json encoded with URL encode.</param>
        /// <param name="p_DocUrl">The URL of the associated document.</param>
        /// <returns>The resulting ACL.</returns>
        private static GsaFeedAcl ConstructAclFromEncodedJson(string p_Acl,
            string p_DocUrl)
        {
            p_Acl = HttpUtility.UrlDecode(p_Acl);
            GsaFeedAcl acl = (GsaFeedAcl) JsonConvert.DeserializeObject(p_Acl, typeof(GsaFeedAcl));
            acl.DocumentUrl = p_DocUrl;
            return acl;
        }
    }
}
