// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GSAFeedPushConverter.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    [Serializable]
    public enum GsaFeedAclAccess
    {
        [EnumMember(Value = "permit")]
        [XmlEnum("permit")]
        Permit,

        [EnumMember(Value = "deny")]
        [XmlEnum("deny")]
        Deny
    }
}
