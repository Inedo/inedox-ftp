using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Implements the FTP PUT command.
    /// </summary>
    [ActionProperties(
        "Send Files",
        "Sends files to an FTP server.",
        "FTP")]
    [CustomEditor(typeof(PutActionEditor))]
    public sealed class PutAction : FtpActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PutAction"/> class.
        /// </summary>
        public PutAction()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a synch.
        /// </summary>
        [Persistent]
        public bool SyncFiles { get; set; }

        protected override void Execute()
        {
            ExecuteRemoteCommand("put");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            LogInformation("Sending files to " + this.FtpServer + " using FTP...");

            using (var ftp = CreateClient())
            using (var actualEvent = new ManualResetEvent(false))
            {
                if (this.LogIndividualFiles)
                    ftp.Preview += (s, e) => LogInformation("Sending " + e.FileName);

                ftp.EndPut += (s, e) =>
                {
                    if (e.Exception != null)
                        LogError(e.Exception.Message);
                    else if (e.Files != null && e.Files.Length > 0)
                        LogInformation(string.Format("{0} file(s) sent.", e.Files.Length));
                    else
                        LogInformation("No files to transfer.");

                    actualEvent.Set();
                };

                ftp.Passive = !this.ForceActiveMode;

                ftp.BeginPut(this.Context.SourceDirectory, this.FileMask, this.ServerPath, this.Recursive, this.SyncFiles, null);
                actualEvent.WaitOne();
            }

            return string.Empty;
        }
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Send files matching {0} from {1} to FTP server at {2}",
                this.FileMask,
                Util.CoalesceStr(this.OverriddenSourceDirectory, "(default directory)"),
                this.ServerNameAndPath
            );
        }
    }
}
