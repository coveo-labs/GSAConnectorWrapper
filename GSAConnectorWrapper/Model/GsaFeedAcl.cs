// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "acl")]
    public class GsaFeedAcl
    {
        [XmlAttribute("url")]
        public string DocumentUrl { get; set; }

        [DefaultValue(GsaFeedAclInheritance.LeafNode)]
        [JsonProperty(PropertyName = "inheritance_type", DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        [XmlAttribute("inheritance-type")]
        public GsaFeedAclInheritance InheritanceType { get; set; }
        
        [JsonProperty(PropertyName = "inherit_from")]
        [XmlAttribute("inherit-from")]
        public string InheritFrom { get; set; }
        
        [JsonProperty(PropertyName = "entries")]
        [XmlElement("principal")]
        public List<GsaFeedPrincipal> Principals { get; set; }

        [XmlIgnore]
        public GsaFeedAcl ParentAcl { get; set; }

        [XmlIgnore]
        public bool AllowAnonymous { get; set; }
    }
}
