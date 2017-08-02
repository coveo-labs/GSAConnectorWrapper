// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "group")]
    public class GsaFeedGroup
    {
        [XmlAttribute("action"), DefaultValue(GsaFeedRecordAction.Add)]
        public GsaFeedRecordAction Action { get; set; }
    }
}
