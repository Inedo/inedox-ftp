using System.Web.UI.WebControls;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Custom editor for the FTP GET action.
    /// </summary>
    internal sealed class GetActionEditor : FtpActionEditorBase<GetAction>
    {
        private CheckBox chkSyncFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetActionEditor"/> class.
        /// </summary>
        public GetActionEditor()
        {
        }

        /// <summary>
        /// Gets a value indicating whether a textbox to edit the target directory should be displayed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a textbox to edit the target directory should be displayed; otherwise, <c>false</c>.
        /// </value>
        public override bool DisplayTargetDirectory { get { return true; } }

        /// <summary>
        /// Gets the server path description.
        /// </summary>
        protected override string ServerPathDescription
        {
            get { return "The path relative to the server root to get files from."; }
        }
        /// <summary>
        /// Gets the file mask description.
        /// </summary>
        protected override string FileMaskDescription
        {
            get { return "The files to get from the server; may include a path and wildcards."; }
        }

        public override string ServerLabel { get { return "To server:"; } }

        public override string TargetDirectoryLabel { get { return "To directory:"; } }

        /// <summary>
        /// Binds the action to the form.
        /// </summary>
        /// <param name="action">The action.</param>
        protected override void BindActionToForm(GetAction action)
        {
            this.chkSyncFiles.Checked = action.SyncFiles;
        }
        /// <summary>
        /// Creates the action from the form.
        /// </summary>
        /// <returns>New action.</returns>
        protected override GetAction CreateActionFromForm()
        {
            return new GetAction
            {
                SyncFiles = this.chkSyncFiles.Checked
            };
        }
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation
        /// to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.chkSyncFiles = new CheckBox { Text = "Only transfer files with newer timestamp" };

            this.Controls.Add(
                new SlimFormField("File synchronization:", this.chkSyncFiles)
            );
        }
    }
}
