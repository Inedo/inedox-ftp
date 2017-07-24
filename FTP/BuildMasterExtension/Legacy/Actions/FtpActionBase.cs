using System;
using System.Collections.Generic;
using System.Linq;
using Dart.PowerTCP.Ftp;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Provides common functionality for FTP actions.
    /// </summary>
    public abstract class FtpActionBase : RemoteActionBase, IMissingPersistentPropertyHandler
    {
        private string serverPath = "/";

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpActionBase"/> class.
        /// </summary>
        protected FtpActionBase()
        {
        }

        /// <summary>
        /// Gets or sets the FTP server to connect to.
        /// </summary>
        [Persistent]
        public string FtpServer { get; set; }
        /// <summary>
        /// Gets or sets the user name to connect with. If blank, anonymous authentication is used.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the password to connect with. This is ignored if <see cref="UserName"/> is blank.
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the root path on the server to get files from.
        /// </summary>
        [Persistent]
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
        /// <summary>
        /// Gets or sets the mask specifying files.
        /// </summary>
        [Persistent]
        public string FileMask { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether subdirectories should be transferred.
        /// </summary>
        [Persistent]
        public bool Recursive { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether individual file operations should be logged.
        /// </summary>
        [Persistent]
        public bool LogIndividualFiles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the FTP server should use an active mode connection.
        /// </summary>
        [Persistent]
        public bool ForceActiveMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether FTP actions should use Binary mode as opposed to ASCII.
        /// </summary>
        [Persistent]
        public bool BinaryMode { get; set; }

        /// <summary>
        /// Gets or sets a series of commands to run on the server before any actions are run.
        /// </summary>
        [Persistent]
        public string[] InitializationCommands { get; set; }

        /// <summary>
        /// Gets the server name with the path appended.
        /// </summary>
        protected string ServerNameAndPath
        {
            get
            {
                var server = this.FtpServer ?? "";
                var path = this.ServerPath;
                if (!server.EndsWith("/"))
                    return server + path;
                else
                    return server + path.Substring(1);
            }
        }

        /// <summary>
        /// Creates an FTP client to connect to the remote server.
        /// </summary>
        /// <returns>FTP client object used to access the server.</returns>
        internal Ftp CreateClient()
        {
            if (string.IsNullOrEmpty(this.FtpServer))
                throw new InvalidOperationException("FTP server not specified.");

            int port = 21;
            var ftpServer = this.FtpServer.TrimEnd('/');
            int index = ftpServer.LastIndexOf(':');
            if (index == 0 || index == ftpServer.Length - 1)
                throw new InvalidOperationException("FTP server name is invalid.");
            else if (index > 0)
            {
                var portText = ftpServer.Substring(index + 1);
                ftpServer = ftpServer.Substring(0, index);
                if (!int.TryParse(portText, out port))
                    throw new InvalidOperationException("Invalid port in FTP server name.");
            }

            this.LogDebug(
                "Connecting to FTP server at {0} in {1} mode as user \"{2}\"...",
                this.FtpServer,
                this.BinaryMode ? "Binary" : "ASCII",
                Util.CoalesceStr(this.UserName, "anonymous")
            );

            var ftp = new Ftp
            {
                Timeout = this.Timeout == 0 ? 30000 : this.Timeout * 1000,
                Server = ftpServer,
                ServerPort = port,
                FileType = this.BinaryMode ? FileType.Image : FileType.Ascii,
                Username = Util.CoalesceStr(this.UserName, "anonymous"),
                Password = string.Equals(this.UserName, "anonymous", StringComparison.OrdinalIgnoreCase) ? "anonymous@" : this.Password
            };

            if (this.InitializationCommands != null)
            {
                foreach (string command in this.InitializationCommands)
                {
                    this.LogDebug("Sending initialization command {0}...", command);
                    var response = ftp.SendCommand(command);
                    this.LogDebug("Responded with: " + response);
                }
            }

            return ftp;
        }

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            string value = missingProperties["AS400Compatibility"];

            if (value == null)
                return;

            if (bool.Parse(value))
                this.InitializationCommands = new[] { "SITE NAMEFMT 1" }.Concat(this.InitializationCommands ?? Enumerable.Empty<string>()).ToArray();
        }
    }
}
