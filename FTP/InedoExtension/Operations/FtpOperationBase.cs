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
using Inedo.Extensions.FTP.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.FTP.Operations
{
    [Tag("FTP")]
    [ScriptNamespace("FTP", PreferUnqualified = false)]
    public abstract class FtpOperationBase : ExecuteOperation, IHasCredentials<FtpCredentials>
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

        public abstract string CredentialName { get; set; }

        [DisplayName("FTP server")]
        [ScriptAlias("Host")]
        [MappedCredential(nameof(FtpCredentials.HostName))]
        [Category("Authentication")]
        public string HostName { get; set; }

        [ScriptAlias("Port")]
        [MappedCredential(nameof(FtpCredentials.Port))]
        [DefaultValue(21)]
        [Category("Authentication")]
        public int Port { get; set; } = 21;

        [DisplayName("Username")]
        [ScriptAlias("User")]
        [MappedCredential(nameof(FtpCredentials.UserName))]
        [Category("Authentication")]
        public string UserName { get; set; }

        [DisplayName("Password")]
        [ScriptAlias("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        [MappedCredential(nameof(FtpCredentials.Password))]
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
        internal FtpWebRequest CreateRequest(string path)
        {
            if (string.IsNullOrEmpty(this.HostName))
                throw new InvalidOperationException("FTP server not specified.");

            var userName = AH.CoalesceString(this.UserName, "anonymous");

            if (this.VerboseLogging)
            {
                this.LogDebug($"Requesting \"{path}\" from {this.HostName} in {this.TransferMode} mode as user \"{userName}\"...");
            }

            var uri = new UriBuilder(Uri.UriSchemeFtp, this.HostName, this.Port, path).Uri;

            var ftp = (FtpWebRequest)WebRequest.Create(uri);
            ftp.Credentials = new NetworkCredential(this.UserName, this.Password);
            ftp.KeepAlive = true;
            ftp.UseBinary = this.TransferMode == DataTransferMode.Binary;
            ftp.UsePassive = this.TransferBehavior == DataTransferBehavior.Passive;

            return ftp;
        }
    }
}
