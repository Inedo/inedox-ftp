using System.Web.UI.WebControls;
using Inedo.BuildMaster.Web.Controls;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Custom editor for the FTP PUT action.
    /// </summary>
    internal sealed class PutActionEditor : FtpActionEditorBase<PutAction>
    {
        private CheckBox chkSyncFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="PutActionEditor"/> class.
        /// </summary>
        public PutActionEditor()
        {
        }

        /// <summary>
        /// Gets a value indicating whether a textbox to edit the source directory should be displayed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a textbox to edit the source directory should be displayed; otherwise, <c>false</c>.
        /// </value>
        public override bool DisplaySourceDirectory { get { return true; } }

        /// <summary>
        /// Gets the server path description.
        /// </summary>
        protected override string ServerPathDescription
        {
            get { return "The path relative to the server root to send files to."; }
        }
        /// <summary>
        /// Gets the file mask description.
        /// </summary>
        protected override string FileMaskDescription
        {
            get { return "The files to send to the server; may include a path and wildcards."; }
        }

        /// <summary>
        /// Binds the action to the form.
        /// </summary>
        /// <param name="action">The action.</param>
        protected override void BindActionToForm(PutAction action)
        {
            this.chkSyncFiles.Checked = action.SyncFiles;
        }
        /// <summary>
        /// Creates the action from the form.
        /// </summary>
        /// <returns>New action.</returns>
        protected override PutAction CreateActionFromForm()
        {
            return new PutAction
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

            this.chkSyncFiles = new CheckBox
            {
                Text = "Synchronize Files"
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Synchronize Files",
                    "If checked, only files with a newer timestamp will be transferred.",
                    true,
                    new StandardFormField(
                        "",
                        this.chkSyncFiles
                    )
                )
            );
        }
    }
}
