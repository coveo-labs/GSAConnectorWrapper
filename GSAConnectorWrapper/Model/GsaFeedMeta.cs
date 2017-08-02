// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    [Serializable]
    [XmlRoot(ElementName = "meta")]
    public class GsaFeedMeta
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("encoding")]
        public GsaFeedMetaEncoding Encoding { get; set; }

        [XmlAttribute("content")]
        public string Content { get; set; }

        public string GetDecodedContent()
        {
            if (Encoding == GsaFeedMetaEncoding.Base64Binary) {
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Content));
            }

            return Content;
        }
    }
}
