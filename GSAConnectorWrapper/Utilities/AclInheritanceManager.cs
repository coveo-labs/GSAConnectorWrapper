// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System.Collections.Concurrent;
using System.Collections.Generic;
using GSAFeedPushConverter.Model;

namespace GSAFeedPushConverter.Utilities
{
    /// <summary>
    /// Manager of the construction of the ACL inheritance tree and manager of the ready documents.
    /// </summary>
    public class AclInheritanceManager
    {
        private readonly ConcurrentDictionary<string, AclNode> m_Nodes = new ConcurrentDictionary<string, AclNode>();

        private readonly List<GsaFeedRecord> m_ReadyRecords;
        private bool m_PushRecordsWithoutAcl;

        /// <summary>
        /// Constructor with the default value for PushRecordsWithoutAcl
        /// </summary>
        public AclInheritanceManager() : this(false)
        {
        }

        /// <summary>
        /// Constructor specifying if the manager push the documents without ACL.
        /// </summary>
        /// <param name="p_PushRecordsWithoutAcl">If true, the document without ACL will be directly push.</param>
        public AclInheritanceManager(bool p_PushRecordsWithoutAcl)
        {
            m_PushRecordsWithoutAcl = p_PushRecordsWithoutAcl;
            m_ReadyRecords = new List<GsaFeedRecord>();
        }

        /// <summary>
        /// Property for the PushRecordsWithoutAcl option.
        /// </summary>
        public bool PushRecordsWithoutAcl
        {
            get
            {
                return m_PushRecordsWithoutAcl;
            }
            set
            {
                m_PushRecordsWithoutAcl = value;
            }
        }

        /// <summary>
        /// Delete all the records attach to the nodes. Useful for full refresh.
        /// </summary>
        public void CleanRecords()
        {
            /* Explanation of the method use to delete documents on full refresh.
             * We could push all the records that we have with the action set to delete, but
             * we already have the delete older than with the ordering id in the push SDK.
             * The idea of deleting only the records that we have in the manager is not safe because 
             * we lose everything when the tool is close.
             */

            //We delete all records because we have a full refresh
            foreach (AclNode nodesValue in m_Nodes.Values) {
                nodesValue.WaitingRecord = null;
            }
        }

        /// <summary>
        /// Get the list of ready records and empty the list.
        /// </summary>
        /// <returns>The list of ready records.</returns>
        public List<GsaFeedRecord> GetReadyToPushRecords()
        {
            List<GsaFeedRecord> rdyRecords = new List<GsaFeedRecord>(m_ReadyRecords);
            m_ReadyRecords.Clear();
            return rdyRecords;
        }

        /// <summary>
        /// Receiving the records from the connector here. They will be verify and when they will be ready, they will be push.
        /// </summary>
        /// <param name="p_Record">The record to push.</param>
        public void AddRecord(GsaFeedRecord p_Record)
        {
            if (p_Record.Action != GsaFeedRecordAction.Add) {
                //If the node no longer exist, we want to delete it
                if (m_Nodes.ContainsKey(p_Record.Url)) {
                    AclNode aclNode = m_Nodes[p_Record.Url];
                    //We could have receive an url that is a 404 but we receive a related ACL in another feed.
                    //So we only delete the record.
                    aclNode.WaitingRecord = null;
                }
                m_ReadyRecords.Add(p_Record);
            } else {
                AclNode aclNode;
                if (p_Record.Acl != null) {
                    //We add the ACL to the dictionary.
                    AddAcl(p_Record.Acl);
                    aclNode = m_Nodes[p_Record.Url];
                } else if (m_Nodes.ContainsKey(p_Record.Url)) {
                    //We get the node of the corresponding URL.
                    aclNode = m_Nodes[p_Record.Url];
                } else {
                    //We need to create a new node.
                    aclNode = new AclNode(p_Record);
                    m_Nodes.AddOrUpdate(p_Record.Url, aclNode, (url,
                        node) => {
                        node.WaitingRecord = p_Record;
                        return node;
                    });
                }
                aclNode.WaitingRecord = p_Record;

                // If the node is already ready, we add it to the list of ready records.
                // Also, if the node does not have an ACL and we do not wait for them, we push it.
                if (aclNode.IsReady || (m_PushRecordsWithoutAcl && aclNode.Acl == null)) {
                    m_ReadyRecords.Add(p_Record);
                }
                CheckNodeReady(aclNode);
            }
        }

        /// <summary>
        /// Adding an ACL to the collection
        /// </summary>
        /// <param name="p_Acl">The <see cref="GsaFeedAcl" /> that will be added in the collection.</param>
        public void AddAcl(GsaFeedAcl p_Acl)
        {
            AclNode aclNode;
            if (m_Nodes.ContainsKey(p_Acl.DocumentUrl)) {
                aclNode = m_Nodes[p_Acl.DocumentUrl];
                //We need to update the ACL parent of the ACL of the children.
                foreach (AclNode aclNodeChild in aclNode.Children) {
                    if (aclNodeChild.Acl != null) {
                        aclNodeChild.Acl.ParentAcl = p_Acl;
                    }
                }
            } else {
                aclNode = new AclNode(p_Acl);

                m_Nodes.AddOrUpdate(p_Acl.DocumentUrl, aclNode, (key,
                    node) => {
                    node.Acl = p_Acl;
                    return node;
                });
            }


            if (aclNode.Acl != null) {
                if (aclNode.Acl.InheritanceType != p_Acl.InheritanceType) {
                    //We want the document to be re-pushed if his relation with his parent has changed.
                    UnreadyNode(aclNode);
                }
                if (aclNode.Acl.InheritFrom != p_Acl.InheritFrom) {
                    //We need to update the old parent and update the new parent.
                    aclNode.Parent.Children.Remove(aclNode);
                    aclNode.Parent = null;
                    //We want the document to be re-pushed if his parent has changed.
                    UnreadyNode(aclNode);
                }
            }


            // We did not have a parent and the ACL have a parent, so we add one.
            if (aclNode.Parent == null && !string.IsNullOrEmpty(p_Acl.InheritFrom)) {
                //We need to update the link to the parent
                AclNode parent;
                if (m_Nodes.ContainsKey(p_Acl.InheritFrom)) {
                    parent = m_Nodes[p_Acl.InheritFrom];
                    parent.Children.Add(aclNode);
                    //We set the parent of the ACL with the parent node Acl.
                    p_Acl.ParentAcl = parent.Acl;
                } else {
                    //We create the parent with the current node as a child.
                    parent = new AclNode(p_Acl.InheritFrom, aclNode);
                    m_Nodes.AddOrUpdate(parent.Url, parent, (parenturl,
                        node) => {
                        node.Children.Add(aclNode);
                        return node;
                    });
                }
                aclNode.Parent = parent;
            }

            aclNode.Acl = p_Acl;
            //If we do not have a parent or the parent is ready, the node is ready.
            CheckNodeReady(aclNode);
        }

        /// <summary>
        /// Verify if the node is ready to be send. If the node become ready, we add it to the ready list.
        /// </summary>
        /// <param name="p_Node">The <see cref="AclNode" /> to verify.</param>
        /// <returns>Return if the node is ready.</returns>
        private bool CheckNodeReady(AclNode p_Node)
        {
            /* GSA Logic on the broken inheritance chain.
             * Note: If a per-URL ACL inherits from a non-existent URL, or inherits from a URL that does not have a per-URL ACL,
             * the authorization decision is always INDETERMINATE because of the broken inheritance chain.
             * https://www.google.com/support/enterprise/static/gsa/docs/admin/72/gsa_doc_set/feedsguide/feedsguide.html#1084377
             */

            // If we are ready, we do not need check any further.
            // We need to have our ACL correctly setup and the parent need to be ready or we do not have a parent.
            if (!p_Node.IsReady && p_Node.Acl != null && (p_Node.Parent == null || CheckNodeReady(p_Node.Parent))) {
                p_Node.IsReady = true;
                if (p_Node.WaitingRecord != null) {
                    //We are changing to the ready status so we add the record to the ready records.
                    m_ReadyRecords.Add(p_Node.WaitingRecord);
                }
                // We push the waiting record and notify the children.
                foreach (AclNode child in p_Node.Children) {
                    CheckNodeReady(child);
                }
            }
            return p_Node.IsReady;
        }

        /// <summary>
        /// Set the node to unready and all of it is children.
        /// </summary>
        /// <param name="p_Node">Node to unready</param>
        private void UnreadyNode(AclNode p_Node)
        {
            p_Node.IsReady = false;
            foreach (AclNode nodeChild in p_Node.Children) {
                UnreadyNode(nodeChild);
            }
        }
    }
}
