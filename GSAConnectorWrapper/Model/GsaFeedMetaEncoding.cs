// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    public enum GsaFeedMetaEncoding
    {
        [XmlEnum("")]
        None,

        [XmlEnum("base64binary")]
        Base64Binary
    }
}
