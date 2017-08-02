// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "membership")]
    public class GsaFeedMembership
    {
        [XmlElement("members")]
        public GsaFeedMembers Members { get; set; }

        [XmlElement("principal")]
        public GsaFeedPrincipal Principal { get; set; }
    }
}