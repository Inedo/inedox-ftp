using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Implements the FTP GET command.
    /// </summary>
    [ActionProperties(
        "Get Files",
        "Gets files from an FTP server.",
        "FTP")]
    [CustomEditor(typeof(GetActionEditor))]
    public sealed class GetAction : FtpActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAction"/> class.
        /// </summary>
        public GetAction()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether to perform a synch.
        /// </summary>
        [Persistent]
        public bool SyncFiles { get; set; }

        protected override void Execute()
        {
            ExecuteRemoteCommand("get");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            LogInformation("Getting files from " + this.FtpServer + " using FTP...");

            using (var ftp = CreateClient())
            using (var actualEvent = new ManualResetEvent(false))
            {
                if (this.LogIndividualFiles)
                    ftp.Preview += (s, e) => LogInformation("Getting " + e.FileName);

                ftp.EndGet += (s, e) =>
                {
                    if (e.Exception != null)
                        LogError(e.Exception.Message);
                    else if (e.Files != null && e.Files.Length > 0)
                        LogInformation(string.Format("{0} file(s) received.", e.Files.Length));
                    else
                        LogInformation("No files to transfer.");

                    actualEvent.Set();
                };

                ftp.Passive = !this.ForceActiveMode;

                ftp.BeginGet(this.ServerPath, this.FileMask, this.Context.TargetDirectory, this.Recursive, this.SyncFiles, null);
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
                "Get files matching {0} from FTP server at {1} to {2}",
                this.FileMask,
                this.ServerNameAndPath,
                Util.CoalesceStr(this.OverriddenTargetDirectory, "(default directory)")
            );
        }
    }
}
