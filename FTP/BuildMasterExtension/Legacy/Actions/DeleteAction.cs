using System.ComponentModel;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Implements the FTP DELETE command.
    /// </summary>
    [DisplayName("Delete Files")]
    [Description("Deletes files on an FTP server.")]
    [CustomEditor(typeof(DeleteActionEditor))]
    [Tag("FTP")]
    [Tag(Tags.Files)]
    public sealed class DeleteAction : FtpActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteAction"/> class.
        /// </summary>
        public DeleteAction()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether empty directories should be removed.
        /// </summary>
        [Persistent]
        public bool RemoveEmptyDirectories { get; set; }

        protected override void Execute()
        {
            ExecuteRemoteCommand("delete");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            this.LogInformation("Deleting files on {0} using FTP...", this.FtpServer);

            using (var ftp = CreateClient())
            using (var actualEvent = new ManualResetEvent(false))
            {
                if (this.LogIndividualFiles)
                    ftp.Preview += (s, e) => this.LogDebug("Deleting {0}...", e.FileName);

                ftp.EndDelete += (s, e) =>
                    {
                        if (e.Exception != null)
                            this.LogError(e.Exception.Message);
                        else if (e.Files != null && e.Files.Length > 0)
                            this.LogInformation("{0} file(s) deleted.", e.Files.Length);
                        else
                            this.LogInformation("No files to delete.");

                        actualEvent.Set();
                    };

                ftp.Passive = !this.ForceActiveMode;

                ftp.BeginDelete(this.FileMask, this.Recursive, this.RemoveEmptyDirectories, null);
                actualEvent.WaitOne();
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns a description of the current configuration of the action.
        /// </summary>
        /// <returns>
        /// Description of the action's current configuration.
        /// </returns>
        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription("FTP DELETE Files"),
                new RichDescription(
                    "matching ", new Hilite(this.FileMask),
                    " from ", new Hilite(this.ServerNameAndPath)
                )
            );
        }
    }
}
