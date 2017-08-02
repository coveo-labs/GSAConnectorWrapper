// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Coveo.Connectors.Utilities;
using Coveo.Connectors.Utilities.PushApiSdk;
using Coveo.Connectors.Utilities.PushApiSdk.Config;
using Coveo.Connectors.Utilities.PushApiSdk.Helpers;
using Coveo.Connectors.Utilities.PushApiSdk.Manager;
using Coveo.Connectors.Utilities.PushApiSdk.Model;
using Coveo.Connectors.Utilities.PushApiSdk.Model.Document;
using Coveo.Connectors.Utilities.PushApiSdk.Model.Permission;
using GSAFeedPushConverter.Model;
using GSAFeedPushConverter.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GSAFeedPushConverter
{
    /// <summary>
    /// Create servers to receive content from a GSA connector, then push it in the Coveo Cloud.
    /// </summary>
    public class ConnectorWrapper
    {
        private const string DATASOURCE = "datasource";
        private const string START_OF_XML = "<?xml";
        private const string PATH_TO_TEMP = @"c:\temp\";
        private const string SUCCESS_REPONSE = "Success";
        private const string ALLOW_GROUP = "-allowed";
        private const string DISALLOW_GROUP = "-disallowed";

        private Configuration m_Config = new Configuration();
        private AclInheritanceManager m_AclManager;
        private readonly string m_ConfigFilePath;
        private DateTime m_LastModifOnConfig;
        private readonly List<string> m_AllRootsUrl = new List<string>();
        private WebServer m_WebServer;
        private WebServer m_WebServerGroups;
        private WebServer m_GsaMockAuth;


        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="p_ConfigFilePath"></param>
        public ConnectorWrapper(string p_ConfigFilePath)
        {
            m_LastModifOnConfig = DateTime.MinValue;
            m_ConfigFilePath = p_ConfigFilePath;
        }

        public void Start()
        {
            if (!UpdateAndValidateConfig()) {
                Console.WriteLine("The given configuration file is invalid, closing now.");
                throw new ArgumentException();
            }

            //We start the different listening servers.
            Startup.Start();
            m_AclManager = new AclInheritanceManager();

            m_WebServer = new WebServer(ProcessRequest, ProcessFeed, m_Config.ListeningUrlFeed);
            m_WebServer.Run();
            m_WebServerGroups = new WebServer(ProcessRequest, ProcessGroups, m_Config.ListeningUrlGroups);
            m_WebServerGroups.Run();
            m_GsaMockAuth = new WebServer(ProcessGsaAuthentication, null, m_Config.GsaMockAuth);
            m_GsaMockAuth.Run();
        }

        public void Stop()
        {
            GsaFeedRecord.Dispose();
            m_WebServer.Stop();
            m_WebServerGroups.Stop();
            m_GsaMockAuth.Stop();
        }

        /// <summary>
        /// Validate the request is a POST request and give the response.
        /// </summary>
        /// <param name="p_HttpRequest">The request to answer.</param>
        /// <returns>The response to the request.</returns>
        private FeedRequestResponse ProcessRequest(HttpListenerRequest p_HttpRequest)
        {
            FeedRequestResponse response = new FeedRequestResponse {
                HttpStatusCode = HttpStatusCode.BadRequest,
                Message = String.Format("Bad request '{0}'.", p_HttpRequest.Url)
            };

            if (p_HttpRequest.HttpMethod == HttpMethod.Post.Method.ToUpperInvariant()) {
                //If the connector does not received "success" as a response, it will retry.
                response.Message = SUCCESS_REPONSE;
                response.HttpStatusCode = HttpStatusCode.OK;
            }

            return response;
        }

        /// <summary>
        /// Parse the give request to extract the groups feed then push them to the push API.
        /// </summary>
        /// <param name="p_HttpRequest">The request containing the groups feed.</param>
        private void ProcessGroups(HttpListenerRequest p_HttpRequest)
        {
            if (p_HttpRequest != null) {
                ConsoleUtilities.WriteLine("Processing the GSA XML Groups.", ConsoleColor.Green);
                string datasource = p_HttpRequest.QueryString.Get(DATASOURCE);
                string filename = String.Format("{0}_{1}_Groups.xml", datasource, Guid.NewGuid());
                string feedFilePath = CopyRequestToFile(p_HttpRequest, filename);

                if (p_HttpRequest.HttpMethod == HttpMethod.Post.Method.ToUpperInvariant()) {
                    //We need a valid configuration to push the groups
                    if (!UpdateAndValidateConfig()) {
                        throw new ArgumentException();
                    }
                    GsaFeedParser parser = new GsaFeedParser(feedFilePath);
                    ICoveoPushApiConfig clientConfig = new CoveoPushApiConfig(m_Config.PushApiEndpointUrl, m_Config.PlatformApiEndpointUrl, m_Config.ApiKey, m_Config.OrganizationId);
                    ICoveoPushApiClient client = new CoveoPushApiClient(clientConfig);

                    Console.WriteLine();
                    Console.WriteLine("Organization: {0}", m_Config.OrganizationId);
                    Console.WriteLine("Security provider: {0}", m_Config.ProviderId);
                    Console.WriteLine("Push source: {0}", datasource);
                    Console.WriteLine("Feed File: '{0}'", feedFilePath);
                    Console.WriteLine("Groups:");
                    Console.WriteLine("--------------------------------------------------------");

                    int nbOfGroups = 0;
                    foreach (GsaFeedMembership gsaFeedMembership in parser.ParseFeedGroups()) {
                        nbOfGroups++;
                        PushMemberOfGroup(client.PermissionManager, m_Config.ProviderId, gsaFeedMembership);
                    }

                    Console.WriteLine();
                    ConsoleUtilities.WriteLine("The XML Groups was processed.", ConsoleColor.Green);
                    Console.WriteLine();
                    Console.WriteLine("Statistics:");
                    ConsoleUtilities.WriteLine("> Added/Updated groups: {0}", ConsoleColor.Cyan, nbOfGroups);
                    File.Delete(feedFilePath);
                } else {
                    ConsoleUtilities.WriteError("Invalid received request: {1} - {0}.", p_HttpRequest.HttpMethod, p_HttpRequest.Url);
                }
            } else {
                ConsoleUtilities.WriteError("No HTTP request to process.");
            }
        }

        /// <summary>
        /// Process the feed in the given request.
        /// </summary>
        /// <param name="p_HttpRequest">The request to extract the feed.</param>
        private void ProcessFeed(HttpListenerRequest p_HttpRequest)
        {
            if (p_HttpRequest != null) {
                ConsoleUtilities.WriteLine("Processing the GSA Feed.", ConsoleColor.Green);

                if (p_HttpRequest.HttpMethod == HttpMethod.Post.Method.ToUpperInvariant()) {
                    //We need a valid configuration to push the groups
                    if (!UpdateAndValidateConfig()) {
                        throw new ArgumentException();
                    }
                    //We update the values from the configuration
                    m_AclManager.PushRecordsWithoutAcl = m_Config.PushRecordsWithoutAcl;

                    string datasource = m_Config.PushSourceId;
                    string providerId = m_Config.ProviderId;

                    string filename = String.Format("Feed{0}_{1}.xml", datasource, Guid.NewGuid());
                    string feedFilePath = CopyRequestToFile(p_HttpRequest, filename);

                    GsaFeedParser parser = new GsaFeedParser(feedFilePath);
                    GsaFeedHeader header = parser.ParseFeedHeader();

                    if (m_Config.DataSourceToSourceId.ContainsKey(header.DataSource)) {
                        datasource = m_Config.DataSourceToSourceId[header.DataSource];
                    }

                    Console.WriteLine();
                    Console.WriteLine("Organization: {0}", m_Config.OrganizationId);
                    Console.WriteLine("Security provider: {0}", providerId);
                    Console.WriteLine("Push source: {0}", datasource);
                    Console.WriteLine("Datasource: {0}, Type: {1}", header.DataSource, header.FeedType);
                    Console.WriteLine("Feed File: '{0}'", feedFilePath);
                    Console.WriteLine("--------------------------------------------------------");

                    ICoveoPushApiConfig clientConfig = new CoveoPushApiConfig(m_Config.PushApiEndpointUrl, m_Config.PlatformApiEndpointUrl, m_Config.ApiKey, m_Config.OrganizationId);
                    ICoveoPushApiClient client = new CoveoPushApiClient(clientConfig);
                    client.ActivityManager.UpdateSourceStatus(datasource, header.FeedType == GsaFeedType.Full ? SourceStatusType.Refresh : SourceStatusType.Incremental);

                    //Process the ACL in the request
                    foreach (GsaFeedAcl acl in parser.ParseFeedAcl()) {
                        ConsoleUtilities.WriteLine("The ACL for \"{0}\" has been parse.", ConsoleColor.Cyan, acl.DocumentUrl);
                        //For the this POC, it is fine to push the ACL Groups that we received in the feed because they will not be attach to a record.
                        PushGroupFromAcl(client.PermissionManager, providerId, acl);
                        m_AclManager.AddAcl(acl);
                    }

                    //Find the root URLs of the feed
                    List<string> feedRoot = new List<string>();
                    foreach (GsaFeedRecord gsaFeedRecord in parser.ParseFeedRecords()) {
                        //First we look if it is a root URL or the result of an incremental
                        //The crawl immediately does not work because the fileshare always push the root url with a crawl immediately
                        //We might want to keep the root URLs and if it is not a root url it is a incremental
                        //If the url do not start with one of the root url, it is a new root URL

                        //We extract the root URL (the "mimetype" should start with text, but we will not verify it for now)
                        feedRoot.Add(gsaFeedRecord.Url);
                    }

                    //Now we go through each root url to find all the records. We are also updating the list of all root URLs.
                    List<GsaFeedRecord> allRecords = new List<GsaFeedRecord>();
                    foreach (string rootUrl in feedRoot) {
                        allRecords.AddRange(GsaWebParser.ExtractRecordsFromUrl(rootUrl, m_Config.DeleteOnInvalidUrl));

                        bool isIncremental = false;
                        string overrideRoot = null;
                        //If a root element is a sub item of another root element, then that root element is incremental.
                        foreach (string mainRoot in m_AllRootsUrl) {
                            if (rootUrl.StartsWith(mainRoot)) {
                                isIncremental = true;
                            } else if (mainRoot.StartsWith(rootUrl)) {
                                overrideRoot = mainRoot;
                            }
                        }
                        //If we received an incremental before a real root, we need to delete it and add the real root.
                        if (overrideRoot != null) {
                            m_AllRootsUrl.Remove(overrideRoot);
                        }
                        if (!isIncremental && !m_AllRootsUrl.Contains(rootUrl)) {
                            m_AllRootsUrl.Add(rootUrl);
                        }
                    }

                    //If all root URLs are part of the feed, this is a full refresh. The feed could contain more URL (i.e. sub-root).
                    //If we have a full-refresh, we will delete the old documents.
                    bool fullRefresh = m_AllRootsUrl.All(feedRoot.Contains);
                    if (fullRefresh) {
                        m_AclManager.CleanRecords();
                    }
                    //We push all the records to the ACL manager and after this, we ask for the ready records.
                    foreach (GsaFeedRecord gsaFeedRecord in allRecords) {
                        m_AclManager.AddRecord(gsaFeedRecord);
                    }

                    //Variable to keep track of the actions with the records.
                    ulong firstOrderingIdRef = 0;
                    ulong orderingIdRef = 0;
                    int addedDocs = 0;
                    int deletedDocs = 0;
                    int ignoredDocs = 0;

                    //Now we can push all the ready records.
                    foreach (GsaFeedRecord record in m_AclManager.GetReadyToPushRecords()) {
                        ConsoleUtilities.WriteLine("{4}>>> {0}|{1}|{2}|{3}",
                            record.Action == GsaFeedRecordAction.Delete ? ConsoleColor.Yellow : ConsoleColor.Cyan,
                            record.Action.ToString().ToUpperInvariant(),
                            record.Url, record.MimeType ?? "None",
                            record.LastModified?.ToUniversalTime().ToString(CultureInfo.InvariantCulture) ?? "Unspecified",
                            DateTime.Now);

                        if (record.Action == GsaFeedRecordAction.Add) {
                            //We need to push the acl virtual groups, if the ACL was already there, we just updating it.
                            if (record.Acl == null) {
                                record.Acl = new GsaFeedAcl { AllowAnonymous = m_Config.PushRecordsWithoutAcl };
                            }
                            PushGroupFromAcl(client.PermissionManager, providerId, record.Acl);

                            //If the record does not have a display URL, that means it is a root URL of GSA.
                            if (string.IsNullOrEmpty(record.DisplayUrl) && m_Config.RequireDisplayUrl) {
                                ConsoleUtilities.WriteWarning("A document without display URL will be ignore.({0})", record.Url);
                            } else {
                                orderingIdRef = client.DocumentManager.AddOrUpdateDocument(datasource,
                                    CreateDocumentFromRecord(record, header.FeedType == GsaFeedType.MetadataAndUrl),
                                    null);
                                addedDocs++;
                            }
                        } else if (record.Action == GsaFeedRecordAction.Delete) {
                            orderingIdRef = client.DocumentManager.DeleteDocument(datasource, record.Url, null);
                            deletedDocs++;
                        } else {
                            ConsoleUtilities.WriteError("No action was specified for the record '{0}'.", record.Url);
                            ignoredDocs++;
                        }
                        //We want to keep the first action ordering id to delete the old documents in a full-refresh.
                        if (firstOrderingIdRef == 0) {
                            firstOrderingIdRef = orderingIdRef;
                        }
                    }

                    //If this is a full-refresh, we need to delete the old documents.
                    if (header.FeedType == GsaFeedType.Full || fullRefresh) {
                        ConsoleUtilities.WriteWarning("Full feed detected - Deleting old documents. Reference ordering Id: {0}.", firstOrderingIdRef);
                        client.DocumentManager.DeleteDocumentsOlderThan(datasource, firstOrderingIdRef, 5);
                    }

                    client.ActivityManager.UpdateSourceStatus(datasource, SourceStatusType.Idle);

                    Console.WriteLine();
                    ConsoleUtilities.WriteLine("The feed was processed.", ConsoleColor.Green);
                    Console.WriteLine();
                    Console.WriteLine("Statistics:");
                    ConsoleUtilities.WriteLine("> Added documents: {0}", ConsoleColor.Cyan, addedDocs);
                    ConsoleUtilities.WriteLine("> Deleted documents: {0}", ConsoleColor.Yellow, deletedDocs);
                    ConsoleUtilities.WriteLine("> Ignored documents: {0}", ConsoleColor.Red, ignoredDocs);

                    File.Delete(feedFilePath);
                } else {
                    ConsoleUtilities.WriteError("Invalid received request: {1} - {0}.", p_HttpRequest.HttpMethod, p_HttpRequest.Url);
                }
            } else {
                ConsoleUtilities.WriteError("No HTTP request to process.");
            }
        }

        /// <summary>
        /// Create a document in the Coveo format from a record.
        /// </summary>
        /// <param name="p_Record">The record to transform in a document.</param>
        /// <param name="p_DownloadContent">Specify if we need to get the content from the URL of the record.</param>
        /// <returns>The document to push.</returns>
        private PushDocument CreateDocumentFromRecord(GsaFeedRecord p_Record,
            bool p_DownloadContent)
        {
            IDictionary<string, JToken> metadata = p_Record.ConvertMetadata();

            //We add the standard field of a record as metadata.
            metadata.Add("clickableuri", p_Record.DisplayUrl);
            metadata.Add(nameof(p_Record.DisplayUrl), p_Record.DisplayUrl);
            metadata.Add(nameof(p_Record.Lock), p_Record.Lock);
            metadata.Add(nameof(p_Record.MimeType), p_Record.MimeType);
            metadata.Add(nameof(p_Record.PageRank), p_Record.PageRank);
            metadata.Add(nameof(p_Record.Scoring), p_Record.Scoring);
            metadata.Add(nameof(p_Record.Url), p_Record.Url);
            metadata.Add(nameof(p_Record.AuthMethod), p_Record.AuthMethod.ToString());
            metadata.Add(nameof(p_Record.CrawlImmediately), p_Record.CrawlImmediately);
            metadata.Add(nameof(p_Record.CrawlOnce), p_Record.CrawlOnce);

            PushDocument document = new PushDocument(p_Record.Url) {
                ModifiedDate = p_Record.LastModified ?? DateTime.MinValue,
                Metadata = metadata
            };

            //If the record have ACL we construct the permissions.
            if (p_Record.Acl != null) {
                DocumentPermissionSet currentDocSet = new DocumentPermissionSet();
                if (p_Record.Acl.AllowAnonymous) {
                    currentDocSet.AllowAnonymous = true;
                }

                //We construct virtual groups for each elements
                PermissionIdentity denyGroup = new PermissionIdentity(p_Record.Url + DISALLOW_GROUP, PermissionIdentityType.VirtualGroup);
                PermissionIdentity allowGroup = new PermissionIdentity(p_Record.Url + ALLOW_GROUP, PermissionIdentityType.VirtualGroup);
                currentDocSet.DeniedPermissions.Add(denyGroup);
                currentDocSet.AllowedPermissions.Add(allowGroup);
                DocumentPermissionLevel currentDocLevel = new DocumentPermissionLevel();
                currentDocLevel.PermissionSets.Add(currentDocSet);


                if (p_Record.Acl.ParentAcl != null) {
                    GsaFeedAcl currentAcl = p_Record.Acl;
                    List<DocumentPermissionLevel> allLevels = new List<DocumentPermissionLevel> { currentDocLevel };
                    int currentLevelIndex = 0;

                    while (currentAcl.ParentAcl != null) {
                        GsaFeedAcl curParentAcl = currentAcl.ParentAcl;
                        DocumentPermissionSet curParentDocSet = new DocumentPermissionSet();
                        PermissionIdentity parentDenyGroup = new PermissionIdentity(curParentAcl.DocumentUrl + DISALLOW_GROUP, PermissionIdentityType.VirtualGroup);
                        PermissionIdentity parentAllowGroup = new PermissionIdentity(curParentAcl.DocumentUrl + ALLOW_GROUP, PermissionIdentityType.VirtualGroup);


                        //We sill always need the parents in a different set
                        curParentDocSet.DeniedPermissions.Add(parentDenyGroup);
                        curParentDocSet.AllowedPermissions.Add(parentAllowGroup);
                        switch (curParentAcl.InheritanceType) {
                            case GsaFeedAclInheritance.BothPermit:
                                //The parent and the document are in two different sets
                                allLevels.ElementAt(currentLevelIndex).PermissionSets.Add(curParentDocSet);
                                break;
                            case GsaFeedAclInheritance.ChildOverrides:
                                //The parent is in a lower level than the current document
                                DocumentPermissionLevel parentLowerDocLevel = new DocumentPermissionLevel();
                                parentLowerDocLevel.PermissionSets.Add(curParentDocSet);
                                //We are adding our self after the children
                                currentLevelIndex++;
                                allLevels.Insert(currentLevelIndex, parentLowerDocLevel);
                                break;
                            case GsaFeedAclInheritance.ParentOverrides:
                                //The parent is in a higher level than the current document
                                DocumentPermissionLevel parentHigherDocLevel = new DocumentPermissionLevel();
                                parentHigherDocLevel.PermissionSets.Add(curParentDocSet);
                                allLevels.Insert(currentLevelIndex, parentHigherDocLevel);
                                break;
                            case GsaFeedAclInheritance.LeafNode:
                                //The document is not suppose to have inheritance from a leaf node
                                ConsoleUtilities.WriteLine("> Warning: You are trying to have inheritance on a LeafNode. Document in error: {0}", ConsoleColor.Yellow, p_Record.Url);
                                curParentAcl.ParentAcl = null;
                                break;
                        }
                        currentAcl = curParentAcl;
                    }
                    //Now we add the permissions to the document
                    foreach (DocumentPermissionLevel documentPermissionLevel in allLevels) {
                        document.Permissions.Add(documentPermissionLevel);
                    }
                } else {
                    //We might need to add the parent level before, so we will not default this action.
                    document.Permissions.Add(currentDocLevel);
                }
            }

            //We set the content of the document.
            if (p_DownloadContent) {
                PushDocumentHelper.SetCompressedEncodedContent(document, Compression.GetCompressedBinaryData(p_Record.GetWebContent().ResponseObject));
            } else {
                if (p_Record.Content.Encoding == GsaFeedContentEncoding.Base64Compressed) {
                    PushDocumentHelper.SetCompressedEncodedContent(document, p_Record.Content.Value.Trim(Convert.ToChar("\n")));
                } else {
                    PushDocumentHelper.SetContent(document, p_Record.Content.GetDecodedValue());
                }
            }

            return document;
        }

        /// <summary>
        /// Push to the cloud the given ACL. Push the groups of that ACL.
        /// </summary>
        /// <param name="p_PermissionPushManager">The permission manager to use to push the groups.</param>
        /// <param name="p_ProviderId">The provider to push the groups.</param>
        /// <param name="p_Acl">The ACL to push.</param>
        private void PushGroupFromAcl(IPermissionServiceManager p_PermissionPushManager,
            string p_ProviderId,
            GsaFeedAcl p_Acl)
        {
            PermissionIdentity denyIdentity = new PermissionIdentity(p_Acl.DocumentUrl + DISALLOW_GROUP, PermissionIdentityType.VirtualGroup);
            PermissionIdentity allowIdentity = new PermissionIdentity(p_Acl.DocumentUrl + ALLOW_GROUP, PermissionIdentityType.VirtualGroup);


            PermissionIdentityBody denyBody = new PermissionIdentityBody(denyIdentity);
            PermissionIdentityBody allowBody = new PermissionIdentityBody(allowIdentity);

            if (p_Acl.Principals != null) {
                foreach (GsaFeedPrincipal principal in p_Acl.Principals) {
                    //We create the groups of the document based on the principals elements
                    PermissionIdentity permission = new PermissionIdentity(principal.Value,
                        principal.AclScope == GsaFeedAclScope.Group ? PermissionIdentityType.Group : PermissionIdentityType.User);
                    if (principal.Access == GsaFeedAclAccess.Permit) {
                        allowBody.Mappings.Add(permission);
                    } else {
                        denyBody.Mappings.Add(permission);
                    }
                }
            }

            p_PermissionPushManager.AddOrUpdateIdentity(p_ProviderId, null, allowBody);
            p_PermissionPushManager.AddOrUpdateIdentity(p_ProviderId, null, denyBody);
        }

        /// <summary>
        /// Push the members of a group.
        /// </summary>
        /// <param name="p_PermissionPushManager">The permission manager to use to push the group.</param>
        /// <param name="p_ProviderId">The provider to push the groups.</param>
        /// <param name="p_Membership">The membership containing the group.</param>
        private void PushMemberOfGroup(IPermissionServiceManager p_PermissionPushManager,
            string p_ProviderId,
            GsaFeedMembership p_Membership)
        {
            if (p_Membership == null) {
                throw new ArgumentNullException(nameof(p_Membership));
            }
            if (p_Membership.Principal == null) {
                throw new ArgumentNullException(nameof(p_Membership.Principal));
            }
            PermissionIdentity group = new PermissionIdentity(p_Membership.Principal.Value,
                p_Membership.Principal.AclScope == GsaFeedAclScope.Group ? PermissionIdentityType.Group : PermissionIdentityType.User);

            PermissionIdentityBody groupBody = new PermissionIdentityBody(group);
            foreach (GsaFeedPrincipal memberPrincipal in p_Membership.Members.Principals) {
                //We create the group based on the principals elements
                PermissionIdentity permission = new PermissionIdentity(memberPrincipal.Value,
                    memberPrincipal.AclScope == GsaFeedAclScope.Group ? PermissionIdentityType.Group : PermissionIdentityType.User);

                groupBody.Mappings.Add(permission);
            }
            p_PermissionPushManager.AddOrUpdateIdentity(p_ProviderId, null, groupBody);
        }

        /// <summary>
        /// Construct a response for a successful connection to GSA. (Used to access the dashboard of the adaptors)
        /// </summary>
        /// <param name="p_HttpRequest">The request to process.</param>
        /// <returns>The answer to the request.</returns>
        private FeedRequestResponse ProcessGsaAuthentication(HttpListenerRequest p_HttpRequest)
        {
            FeedRequestResponse response = new FeedRequestResponse {
                HttpStatusCode = HttpStatusCode.BadRequest,
                Message = String.Format("Bad request '{0}'.", p_HttpRequest.Url)
            };

            if (p_HttpRequest.HttpMethod == HttpMethod.Post.Method.ToUpperInvariant()) {
                //The adaptor is only expecting an Auth token.
                response.Message = "Authentication Success!\nAuth=737db7e7e42aac47e75223fb85dd3c03";
                response.HttpStatusCode = HttpStatusCode.OK;
            }

            return response;
        }

        /// <summary>
        /// Copy the stream of the request in a file of the given name
        /// </summary>
        /// <param name="p_HttpRequest">The request to get the stream</param>
        /// <param name="p_FileName">The name of the file that will be created.</param>
        /// <returns>The complete path of the file.</returns>
        private string CopyRequestToFile(HttpListenerRequest p_HttpRequest,
            string p_FileName)
        {
            string feedFilePath = Path.Combine(PATH_TO_TEMP, p_FileName);

            using (FileStream output = File.OpenWrite(feedFilePath)) {
                RemoveGsaExtraContent(p_HttpRequest).CopyTo(output);
            }
            return feedFilePath;
        }

        /// <summary>
        /// Clear the stream from content that is not part of the XML.
        /// </summary>
        /// <param name="p_HttpRequest">The request containing the source stream and content encoding.</param>
        /// <returns>The clean stream.</returns>
        private Stream RemoveGsaExtraContent(HttpListenerRequest p_HttpRequest)
        {
            if (p_HttpRequest != null) {
                string content;
                using (StreamReader reader = new StreamReader(p_HttpRequest.InputStream, p_HttpRequest.ContentEncoding)) {
                    content = reader.ReadToEnd();
                }
                string[] contentArray = content.Split(new string[] { START_OF_XML }, StringSplitOptions.None);
                if (contentArray.Length > 1) {
                    //If we do not have content before the xml, the array will still contain 2 elements, an empty one and one with the xml.
                    content = START_OF_XML + contentArray[1];
                    //if we do not have content after the xml, the array will contain one element, the xml.
                    contentArray = content.Split(new string[] { "--<<--" }, StringSplitOptions.None);
                    content = contentArray[0];

                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(content);
                    writer.Flush();
                    stream.Position = 0;
                    return stream;
                } else {
                    throw new ArgumentException("The request does not contain a XML.");
                }
            }
            return null;
        }

        /// <summary>
        /// Reload the configuration from the file and validate that configuration is complete.
        /// </summary>
        /// <returns>Return if the configuration file is valid.</returns>
        private bool UpdateAndValidateConfig()
        {
            bool isValid = true;
            DateTime newConfigDate = File.GetLastWriteTime(m_ConfigFilePath);
            if (m_LastModifOnConfig < newConfigDate) {
                m_LastModifOnConfig = newConfigDate;
                string stringConfig = File.ReadAllText(m_ConfigFilePath);
                m_Config = (Configuration) JsonConvert.DeserializeObject(stringConfig, typeof(Configuration));
                List<string> missingConfig = m_Config.ValidateConfig();
                if (missingConfig.Count > 0) {
                    isValid = false;
                    foreach (string configName in missingConfig) {
                        ConsoleUtilities.WriteError("The parameter {0} is missing in the configuration file. Current path:{1}", configName, m_ConfigFilePath);
                    }
                }
            }
            return isValid;
        }
    }
}
