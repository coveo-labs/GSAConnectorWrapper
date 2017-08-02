// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "metadata")]
    public class GsaFeedMetadata
    {
        [XmlElement("meta")]
        public List<GsaFeedMeta> Values { get; set; }
    }
}
