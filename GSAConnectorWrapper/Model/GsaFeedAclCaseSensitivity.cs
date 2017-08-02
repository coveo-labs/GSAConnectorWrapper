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
    public enum GsaFeedAclCaseSensitivity
    {
        [EnumMember(Value = "everything_case_sensitive")]
        [XmlEnum("everything-case-sensitive")]
        CaseSensitive,
        
        [EnumMember(Value = "everything_case_insensitive")]
        [XmlEnum("everything-case-insensitive")]
        NotCaseSensitive
    }
}
