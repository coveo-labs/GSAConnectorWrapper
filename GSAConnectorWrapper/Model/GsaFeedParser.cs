// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace GSAFeedPushConverter.Model
{
    /// <summary>
    /// Parser of the XML feed.
    /// </summary>
    public class GsaFeedParser
    {
        private const string HEADER_ELEMENT = "header";
        private const string GROUP_ELEMENT = "group";
        private const string RECORD_ELEMENT = "record";
        private const string ACTION_ATTRIBUTE = "action";
        private const string ACL_ELEMENT = "acl";
        private const string XML_GROUP_ELEMENT = "xmlgroups";
        private const string MEMBERSHIP_ELEMENT = "membership";

        private readonly string m_FeedFilePath;

        /// <summary>
        /// Constructor. Construct records from XML feed.
        /// </summary>
        /// <param name="p_FeedFilePath">Path to the XML feed.</param>
        public GsaFeedParser(string p_FeedFilePath)
        {
            m_FeedFilePath = p_FeedFilePath;
        }

        /// <summary>
        /// Go through the XML Feed and find all the records.
        /// </summary>
        /// <returns>The list of all records in the document.</returns>
        public IEnumerable<GsaFeedRecord> ParseFeedRecords()
        {
            CheckFileExists(m_FeedFilePath);

            using (XmlReader reader = CreateXmlReader(m_FeedFilePath)) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element) {
                        if (reader.LocalName == GROUP_ELEMENT) {
                            string xmlAction = reader.GetAttribute(ACTION_ATTRIBUTE);
                            //The default value is add
                            GsaFeedRecordAction groupAction = GsaFeedRecordAction.Add;

                            if (!String.IsNullOrWhiteSpace(xmlAction)) {
                                groupAction = (GsaFeedRecordAction) Enum.Parse(typeof(GsaFeedRecordAction),
                                    xmlAction);
                            }

                            if (reader.ReadToDescendant(RECORD_ELEMENT)) {
                                do {
                                    XElement xmlRecord = XNode.ReadFrom(reader) as XElement;
                                    GsaFeedRecord record = Deserialize<GsaFeedRecord>(xmlRecord);

                                    //The record action override the group action.
                                    if (record.Action == GsaFeedRecordAction.Unspecified && groupAction != GsaFeedRecordAction.Unspecified) {
                                        record.Action = groupAction;
                                    }

                                    //We add the record to the dictionary of parents
                                    if (record.Acl != null) {
                                        //we will need the document url in the Acl to construct the permissions.
                                        record.Acl.DocumentUrl = record.Url;
                                    }

                                    yield return record;
                                } while (reader.ReadToNextSibling(RECORD_ELEMENT));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parse only the ACL of the feed.
        /// </summary>
        /// <returns>Return all the ACL of the feed.</returns>
        public IEnumerable<GsaFeedAcl> ParseFeedAcl()
        {
            CheckFileExists(m_FeedFilePath);

            using (XmlReader reader = CreateXmlReader(m_FeedFilePath)) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element) {
                        if (reader.LocalName == GROUP_ELEMENT) {
                            if (reader.ReadToDescendant(ACL_ELEMENT)) {
                                do {
                                    XElement xmlAcl = XNode.ReadFrom(reader) as XElement;
                                    GsaFeedAcl acl = Deserialize<GsaFeedAcl>(xmlAcl);
                                    yield return acl;
                                } while (reader.ReadToNextSibling(ACL_ELEMENT));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parse the header of the feed.
        /// </summary>
        /// <returns>Header of the feed.</returns>
        public GsaFeedHeader ParseFeedHeader()
        {
            CheckFileExists(m_FeedFilePath);

            GsaFeedHeader header = null;

            using (XmlReader reader = CreateXmlReader(m_FeedFilePath)) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element && reader.LocalName == HEADER_ELEMENT) {
                        XElement xmlHeader = XNode.ReadFrom(reader) as XElement;
                        header = Deserialize<GsaFeedHeader>(xmlHeader);
                    }
                }
            }

            return header;
        }

        /// <summary>
        /// Parse the groups of the XML Groups Feed.
        /// </summary>
        /// <returns>All the groups of the feed.</returns>
        public IEnumerable<GsaFeedMembership> ParseFeedGroups()
        {
            CheckFileExists(m_FeedFilePath);

            using (XmlReader reader = CreateXmlReader(m_FeedFilePath)) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Element) {
                        if (reader.LocalName == XML_GROUP_ELEMENT) {
                            if (reader.ReadToDescendant(MEMBERSHIP_ELEMENT)) {
                                do {
                                    XElement xmlgroups = XNode.ReadFrom(reader) as XElement;
                                    GsaFeedMembership membership = Deserialize<GsaFeedMembership>(xmlgroups);
                                    yield return membership;
                                } while (reader.ReadToNextSibling(MEMBERSHIP_ELEMENT));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verify the file of the feed exist.
        /// </summary>
        /// <param name="p_FeedFilePath">Path to the file.</param>
        private static void CheckFileExists(string p_FeedFilePath)
        {
            if (String.IsNullOrWhiteSpace(p_FeedFilePath) || !File.Exists(p_FeedFilePath)) {
                throw new ArgumentException(String.Format("The file '{0}' does not exists.", p_FeedFilePath));
            }
        }

        /// <summary>
        /// Create a XML reader for the given file path.
        /// </summary>
        /// <param name="p_FeedFilePath">Path to the file containing the XML.</param>
        /// <returns>The XML reader of the given file.</returns>
        private static XmlReader CreateXmlReader(string p_FeedFilePath)
        {
            XmlReader reader = XmlReader.Create(p_FeedFilePath,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            return reader;
        }

        /// <summary>
        /// Deserialize the object from the XML element.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="p_Element">The XML element to deserialize.</param>
        /// <returns>The deserialize object.</returns>
        private static T Deserialize<T>(XElement p_Element) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T) serializer.Deserialize(p_Element.CreateReader());
        }
    }
}
