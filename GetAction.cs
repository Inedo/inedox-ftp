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
        "Gets files from an FTP server.")]
    [CustomEditor(typeof(GetActionEditor))]
    [Tag("FTP")]
    [Tag(Tags.Files)]
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
            this.LogInformation("Getting files from {0} using FTP...", this.FtpServer);

            using (var ftp = CreateClient())
            using (var actualEvent = new ManualResetEvent(false))
            {
                if (this.LogIndividualFiles)
                    ftp.Preview += (s, e) => this.LogDebug("Getting {0}...", e.FileName);

                ftp.EndGet += (s, e) =>
                {
                    if (e.Exception != null)
                        this.LogError(e.Exception.Message);
                    else if (e.Files != null && e.Files.Length > 0)
                        this.LogInformation("{0} file(s) received.", e.Files.Length);
                    else
                        this.LogInformation("No files to transfer.");

                    actualEvent.Set();
                };

                ftp.Passive = !this.ForceActiveMode;

                ftp.BeginGet(this.ServerPath, this.FileMask, this.Context.TargetDirectory, this.Recursive, this.SyncFiles, null);
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
                new ShortActionDescription("FTP GET Files"),
                new LongActionDescription(
                    "matching ", new Hilite(this.FileMask), 
                    " from ", new Hilite(this.ServerNameAndPath), 
                    " to ", new DirectoryHilite(this.OverriddenTargetDirectory)
                )
            );
        }
    }
}
