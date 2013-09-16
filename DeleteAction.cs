using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Implements the FTP DELETE command.
    /// </summary>
    [ActionProperties(
        "Delete Files",
        "Deletes files on an FTP server.",
        "FTP")]
    [CustomEditor(typeof(DeleteActionEditor))]
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
            LogInformation("Deleting files on " + this.FtpServer + " using FTP...");

            using (var ftp = CreateClient())
            using (var actualEvent = new ManualResetEvent(false))
            {
                if (this.LogIndividualFiles)
                    ftp.Preview += (s, e) => LogInformation("Deleting " + e.FileName);

                ftp.EndDelete += (s, e) =>
                    {
                        if (e.Exception != null)
                            LogError(e.Exception.Message);
                        else if (e.Files != null && e.Files.Length > 0)
                            LogInformation(string.Format("{0} file(s) deleted.", e.Files.Length));
                        else
                            LogInformation("No files to delete.");

                        actualEvent.Set();
                    };

                ftp.Passive = !this.ForceActiveMode;

                ftp.BeginDelete(this.FileMask, this.Recursive, this.RemoveEmptyDirectories, null);
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
                "Delete files matching {0} on FTP server at {1}",
                this.FileMask,
                this.ServerNameAndPath
            );
        }
    }
}
