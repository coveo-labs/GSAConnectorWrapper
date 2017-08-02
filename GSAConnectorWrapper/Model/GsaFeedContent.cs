// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "content")]
    public class GsaFeedContent
    {
        [XmlAttribute("encoding")]
        public GsaFeedContentEncoding Encoding { get; set; }

        [XmlText]
        public string Value { get; set; }

        public string GetDecodedValue()
        {
            if (Encoding == GsaFeedContentEncoding.Base64Binary) {
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Value));
            }

            if (Encoding == GsaFeedContentEncoding.Base64Compressed) {
                throw new NotImplementedException("Unsupported decoding value.");
            }

            return Value;
        }
    }
}
