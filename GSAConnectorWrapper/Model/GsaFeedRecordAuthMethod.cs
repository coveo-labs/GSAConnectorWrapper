// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    public enum GsaFeedRecordAuthMethod
    {
        [XmlEnum("none")]
        None,

        [XmlEnum("httpbasic")]
        HttpBasic,

        [XmlEnum("ntlm")]
        Ntlm,

        [XmlEnum("httpsso")]
        HttpSso,

        [XmlEnum("negotiate")]
        Negotiate
    }
}
