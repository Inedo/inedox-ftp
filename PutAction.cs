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
        "Sends files to an FTP server.")]
    [CustomEditor(typeof(PutActionEditor))]
    [Tag("FTP")]
    [Tag(Tags.Files)]
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
            this.LogInformation("Sending files to {0} using FTP...", this.FtpServer);

            using (var ftp = CreateClient())
            using (var actualEvent = new ManualResetEvent(false))
            {
                if (this.LogIndividualFiles)
                    ftp.Preview += (s, e) => this.LogInformation("Sending {0}...", e.FileName);

                ftp.EndPut += (s, e) =>
                {
                    if (e.Exception != null)
                        this.LogError(e.Exception.Message);
                    else if (e.Files != null && e.Files.Length > 0)
                        this.LogInformation("{0} file(s) sent.", e.Files.Length);
                    else
                        this.LogInformation("No files to transfer.");

                    actualEvent.Set();
                };

                ftp.Passive = !this.ForceActiveMode;

                ftp.BeginPut(this.Context.SourceDirectory, this.FileMask, this.ServerPath, this.Recursive, this.SyncFiles, null);
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
        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription("FTP PUT Files"),
                new LongActionDescription(
                    "matching ", new Hilite(this.FileMask),
                    " from ",  new DirectoryHilite(this.OverriddenTargetDirectory),
                    " to ", new Hilite(this.ServerNameAndPath)
                )
            );
        }
    }
}
