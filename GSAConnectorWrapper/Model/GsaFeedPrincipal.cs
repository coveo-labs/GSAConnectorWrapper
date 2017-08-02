// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "principal", IsNullable = false)]
    public class GsaFeedPrincipal
    {
        [XmlIgnore]
        [JsonProperty(PropertyName = "scope")]
        [JsonConverter(typeof(StringEnumConverter))]
        public GsaFeedAclScope AclScope { get; set; }

        [JsonProperty(PropertyName = "access")]
        [JsonConverter(typeof(StringEnumConverter))]
        [XmlAttribute("access")]
        public GsaFeedAclAccess Access { get; set; }

        [DefaultValue("Default")]
        [JsonProperty(PropertyName = "namespace", DefaultValueHandling = DefaultValueHandling.Populate)]
        [XmlAttribute("namespace")]
        public string Namespace { get; set; }
        
        [DefaultValue(GsaFeedAclCaseSensitivity.CaseSensitive)]
        [JsonProperty(PropertyName = "case_sensitivity_type", DefaultValueHandling = DefaultValueHandling.Populate)]
        [JsonConverter(typeof(StringEnumConverter))]
        [XmlIgnore]
        public GsaFeedAclCaseSensitivity CaseSensitivityType { get; set; }

        [XmlAttribute("pridncipal-type")]
        public string PrincipalType { get; set; }

        [JsonProperty(PropertyName = "name")]
        [XmlText()]
        public string Value { get; set; }

        
        [XmlAttribute("case-sensitivity-type")]
        [DefaultValue("case-sensitivity-type")]
        public string CaseSensitivityTypeXml
        {
            get
            {
                return CaseSensitivityType.ToString().ToUpper();
            }
            set
            {
                CaseSensitivityType = Regex.IsMatch(value, "case(_|-)sensitivity(_|-)type", RegexOptions.IgnoreCase) ? GsaFeedAclCaseSensitivity.CaseSensitive : GsaFeedAclCaseSensitivity.NotCaseSensitive;
            }
        }

        [XmlAttribute("scope")]
        public string AclScopeXml
        {
            get
            {
                return AclScope.ToString().ToUpper();
            }
            set
            {
                AclScope = (GsaFeedAclScope) Enum.Parse(typeof(GsaFeedAclScope), value, true);
            }
        }
    }
}
