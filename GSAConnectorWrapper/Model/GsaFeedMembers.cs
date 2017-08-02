// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "members")]
    public class GsaFeedMembers
    {
        [XmlElement("principal")]
        public List<GsaFeedPrincipal> Principals { get; set; }
    }
}