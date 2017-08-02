// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Coveo.Connectors.Utilities.Rest.Response;
using GSAFeedPushConverter.Utilities;
using Newtonsoft.Json.Linq;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "record")]
    public class GsaFeedRecord
    {
        [XmlIgnore]
        private static readonly HttpDownloader s_HttpDownloader = new HttpDownloader();

        [XmlIgnore]
        private IHttpRestResponse<Stream, string> m_WebContent;

        public GsaFeedRecord()
        {
            CrawlImmediately = false;
            CrawlOnce = false;
        }

        [XmlAttribute("last-modified")]
        public string LastModifiedString { get; set; }

        [XmlIgnore]
        public DateTime? LastModified
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(LastModifiedString)) {
                    return DateTime.Parse(LastModifiedString);
                }

                return null;
            }
        }

        [XmlAttribute("mimetype")]
        public string MimeType { get; set; }

        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("displayurl")]
        public string DisplayUrl { get; set; }

        [XmlAttribute("action")]
        public GsaFeedRecordAction Action { get; set; }

        [XmlAttribute("authmethod")]
        public GsaFeedRecordAuthMethod AuthMethod { get; set; }

        [XmlAttribute("lock")]
        public bool Lock { get; set; }

        [XmlAttribute("pagerank")]
        public string PageRank { get; set; }

        [XmlAttribute("crawl-immediately"), DefaultValue(false)]
        public bool CrawlImmediately { get; set; }

        [XmlAttribute("crawl-once"), DefaultValue(false)]
        public bool CrawlOnce { get; set; }

        [XmlAttribute("scoring")]
        public string Scoring { get; set; }

        [XmlElement("content")]
        public GsaFeedContent Content { get; set; }

        [XmlElement("metadata")]
        public GsaFeedMetadata Metadata { get; set; }

        [XmlElement("acl")]
        public GsaFeedAcl Acl { get; set; }

        public IDictionary<string, JToken> ConvertMetadata()
        {
            IDictionary<string, JToken> metatada = new Dictionary<string, JToken>();

            if (Metadata != null) {
                foreach (GsaFeedMeta meta in Metadata.Values) {
                    if (metatada.ContainsKey(meta.Name)) {
                        metatada[meta.Name] = String.Join(";", metatada[meta.Name], meta.GetDecodedContent());
                    } else {
                        metatada.Add(meta.Name, meta.GetDecodedContent());
                    }
                }
            }

            return metatada;
        }

        public IHttpRestResponse<Stream, string> GetWebContent()
        {
            if (m_WebContent == null) {
                if (!string.IsNullOrEmpty(Url)) {
                    m_WebContent = s_HttpDownloader.CompleteDownload(Url);
                    //With a 404, the response object is null as an example.
                    if (m_WebContent.ResponseObject != null) {
                        //We keep the original content and we get the content in a string to see if it has text value.
                        //Stream copyForReading = new Stream());
                        //m_WebContent.ResponseObject.CopyTo(copyForReading);
                        //(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize
                        using (StreamReader reader = new StreamReader(m_WebContent.ResponseObject, Encoding.UTF8, true, 1024, true)) {
                            m_WebContent.ResponseContent = reader.ReadToEnd();
                        }
                    }
                }
            }
            return m_WebContent;
        }

        public bool SetPropertyByName(string p_PropertyName,
            string p_Value)
        {
            bool found = true;
            if (Regex.IsMatch(p_PropertyName, "crawl(_|-)once", RegexOptions.IgnoreCase)) {
                CrawlOnce = Convert.ToBoolean(p_Value);
            } else if (p_PropertyName.ToLower() == "lock") {
                Lock = Convert.ToBoolean(p_Value);
            } else if (p_PropertyName.ToLower() == "scoring") {
                Scoring = p_Value;
            } else if (Regex.IsMatch(p_PropertyName, "display(_|-|)url", RegexOptions.IgnoreCase)) {
                DisplayUrl = p_Value;
            } else if (p_PropertyName.ToLower() == "pagerank") {
                PageRank = p_Value;
            } else if (p_PropertyName.ToLower() == "mimetype") {
                MimeType = p_Value;
            } else {
                found = false;
            }
            return found;
        }

        public static void Dispose()
        {
            s_HttpDownloader?.Dispose();
        }
    }
}
