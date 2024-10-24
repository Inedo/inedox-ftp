using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Security;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.FTP.Credentials;
using Inedo.Web;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.FTP.Operations
{
    [Tag("FTP")]
    [ScriptNamespace("FTP", PreferUnqualified = false)]
    public abstract class FtpOperationBase : ExecuteOperation
    {
        public enum DataTransferMode
        {
            Binary,
            ASCII
        }

        public enum DataTransferBehavior
        {
            Passive,
            Active
        }

        private string serverPath = "/";

        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        [SuggestableValue(typeof(SecureCredentialsSuggestionProvider<UsernamePasswordCredentials>))]
        public string CredentialName { get; set; }

        [DisplayName("FTP Resource")]
        [ScriptAlias("ResourceName")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<FtpSecureResource>))]
        public string ResourceName { get; set; }

        [DisplayName("FTP server")]
        [ScriptAlias("Host")]
        [Category("Authentication")]
        public string HostName { get; set; }

        [ScriptAlias("Port")]
        [DefaultValue(21)]
        [Category("Authentication")]
        public int Port { get; set; } = 21;

        [DisplayName("Username")]
        [ScriptAlias("User")]
        [Category("Authentication")]
        public string UserName { get; set; }

        [DisplayName("Password")]
        [ScriptAlias("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        [Category("Authentication")]
        public SecureString Password { get; set; }

        [DisplayName("Server path")]
        [ScriptAlias("ServerPath")]
        public string ServerPath
        {
            get { return this.serverPath; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    this.serverPath = "/";
                else if (!value.StartsWith("/"))
                    this.serverPath = "/" + value;
                else
                    this.serverPath = value;
            }
        }

        [ScriptAlias("Include")]
        [MaskingDescription]
        public IEnumerable<string> Includes { get; set; }

        [ScriptAlias("Exclude")]
        [MaskingDescription]
        public IEnumerable<string> Excludes { get; set; }

        [DisplayName("Verbose logging")]
        [ScriptAlias("Verbose")]
        [Category("Advanced")]
        public bool VerboseLogging { get; set; }

        [DisplayName("Transfer behavior")]
        [ScriptAlias("Behavior")]
        [DefaultValue(DataTransferBehavior.Passive)]
        [Category("Advanced")]
        public DataTransferBehavior TransferBehavior { get; set; } = DataTransferBehavior.Passive;

        [DisplayName("Transfer mode")]
        [ScriptAlias("Mode")]
        [DefaultValue(DataTransferMode.Binary)]
        [Category("Advanced")]
        public DataTransferMode TransferMode { get; set; } = DataTransferMode.Binary;

        [Category("Advanced")]
        [DisplayName("Use current date on error")]
        [Description("Set the file modified date and time as the current date and time if it cannot be parsed from the FTP server.")]
        [ScriptAlias("UseCurrentDateOnDateParseError")]
        public bool UseCurrentDateOnDateParseError { get; set; } = false;

        /// <summary>
        /// Creates an FTP client to connect to the remote server.
        /// </summary>
        /// <returns>FTP client object used to access the server.</returns>
        internal FtpWebRequest CreateRequest(UsernamePasswordCredentials credentials, FtpSecureResource resource, string path)
        {
            if (string.IsNullOrEmpty(resource?.HostName))
                throw new InvalidOperationException("FTP server not specified.");

            if (this.VerboseLogging)
            {
                this.LogDebug($"Requesting \"{path}\" from {resource?.HostName} in {this.TransferMode} mode as user \"{credentials?.UserName}\"...");
            }

            var uri = new UriBuilder(Uri.UriSchemeFtp, resource.HostName, resource.Port, path).Uri;

#pragma warning disable SYSLIB0014 // Type or member is obsolete
            var ftp = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            ftp.Credentials = new NetworkCredential(credentials?.UserName, credentials?.Password);
            ftp.KeepAlive = true;
            ftp.UseBinary = this.TransferMode == DataTransferMode.Binary;
            ftp.UsePassive = this.TransferBehavior == DataTransferBehavior.Passive;

            return ftp;
        }

        protected (UsernamePasswordCredentials, FtpSecureResource) GetCredentialsAndResource(ICredentialResolutionContext context)
        {
            if (context == null)
                throw new InvalidOperationException("Credential resolution context is null.");

            UsernamePasswordCredentials credentials; FtpSecureResource resource = null;
            if (string.IsNullOrEmpty(this.CredentialName))
            {
                credentials = string.IsNullOrWhiteSpace(this.UserName) ? null : new UsernamePasswordCredentials();
            }
            else
            {
                credentials = (UsernamePasswordCredentials)SecureCredentials.TryCreate(this.CredentialName, context);
            }

            if (resource == null && string.IsNullOrWhiteSpace(this.ResourceName))
            {
                resource = new FtpSecureResource();
            }
            else if (resource == null)
            {
                resource = (FtpSecureResource)SecureResource.TryCreate(SecureResourceType.General, this.ResourceName, context);
            }

            if (credentials != null)
            {
                credentials.UserName = AH.CoalesceString(this.UserName, credentials.UserName);
                credentials.Password = this.Password ?? credentials.Password;
                credentials.UserName = AH.CoalesceString(credentials.UserName, "anonymous");
            }

            if (resource != null)
            {
                resource.HostName = AH.CoalesceString(this.HostName, resource.HostName);
                resource.Port = this.Port != 21 ? this.Port : resource.Port;

            }

            return (credentials, resource);
        }
    }
}
