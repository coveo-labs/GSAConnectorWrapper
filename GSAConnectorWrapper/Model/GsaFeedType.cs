// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    public enum GsaFeedType
    {
        [XmlEnum("incremental")]
        Incremental,

        [XmlEnum("full")]
        Full,

        [XmlEnum("metadata-and-url")]
        MetadataAndUrl
    }
}
