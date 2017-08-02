// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "header")]
    public class GsaFeedHeader
    {
        [XmlElement("datasource")]
        public string DataSource { get; set; }

        [XmlElement("feedtype")]
        public GsaFeedType FeedType { get; set; }
    }
}
