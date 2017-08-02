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
    public enum GsaFeedAclInheritance
    {
        //We put the child in a higher level than the parent.
        [EnumMember(Value = "CHILD_OVERRIDES")]
        [XmlEnum("child-overrides")]
        ChildOverrides,

        [EnumMember(Value = "PARENT_OVERRIDES")]
        //We put the parent in a higher level than the child.
        [XmlEnum("parent-overrides")]
        ParentOverrides,

        //Child and parent are on the same level.
        [EnumMember(Value = "AND_BOTH_PERMIT")]
        [XmlEnum("and-both-permit")]
        BothPermit,

        //This ACL will not be inherit.
        [EnumMember(Value = "LEAF_NODE")]
        [XmlEnum("leaf-node")]
        LeafNode
        /* GSA official definition (https://www.google.com/support/enterprise/static/gsa/docs/admin/72/gsa_doc_set/feedsguide/feedsguide.html#1084066):
         * Valid values are:
        • parent-overrides--The permission of the parent ACL dominates the child ACL, except when the parent permission is INDETERMINATE. In this case, the child permission dominates. If both parent and child are INDETERMINATE, then the permission is INDETERMINATE.
        • child-overrides--The permission of the child ACL dominates the parent ACL, except when the child permission is INDETERMINATE. In this case, the parent permission dominates. If both parent and child are INDETERMINATE, then the permission is INDETERMINATE.
        • and-both-permit--The permission is PERMIT only if both the parent ACL and child ACL permissions are PERMIT. Otherwise, the permission is DENY.
        • leaf-node--ACL that terminates the chain.
        */
    }
}
