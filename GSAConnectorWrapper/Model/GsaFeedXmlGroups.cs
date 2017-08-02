// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "xmlgroups")]
    public class GsaFeedXmlGroups
    {
        [XmlElement("membership")]
        public List<GsaFeedMembership> Membership { get; set; }
    }
}
