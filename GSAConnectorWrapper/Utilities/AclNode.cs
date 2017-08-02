// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System.Collections.Generic;
using GSAFeedPushConverter.Model;

namespace GSAFeedPushConverter.Utilities
{
    /// <summary>
    /// Container for constructing an ACL inheritance.
    /// </summary>
    public class AclNode
    {
        public bool IsReady;

        public readonly string Url;
        public GsaFeedAcl Acl;
        public List<AclNode> Children;
        public GsaFeedRecord WaitingRecord;
        public AclNode Parent;

        /// <summary>
        /// Constructor with only the Node URL.
        /// </summary>
        /// <param name="p_Url">Node URL</param>
        private AclNode(string p_Url)
        {
            Url = p_Url;
            Children = new List<AclNode>();
            IsReady = false;
        }

        /// <summary>
        /// Constructor based on a record. (Set the waiting record and node URL)
        /// </summary>
        /// <param name="p_Record">The record used to construct the node.</param>
        public AclNode(GsaFeedRecord p_Record) : this(p_Record.Url)
        {
            WaitingRecord = p_Record;
        }

        /// <summary>
        /// Constructor based on an ACL. (Set the ACL and node URL)
        /// </summary>
        /// <param name="p_Acl">The ACL used to construct the node.</param>
        public AclNode(GsaFeedAcl p_Acl) : this(p_Acl.DocumentUrl)
        {
            Acl = p_Acl;
        }

        /// <summary>
        /// Constructor giving the URL and a child node.
        /// </summary>
        /// <param name="p_Url">URL of the node.</param>
        /// <param name="p_Child">A Child of the node.</param>
        public AclNode(string p_Url,
            AclNode p_Child) : this(p_Url)
        {
            Children.Add(p_Child);
        }
    }
}
