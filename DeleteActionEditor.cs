using System.Web.UI.WebControls;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Custom editor for the FTP DELETE action.
    /// </summary>
    internal sealed class DeleteActionEditor : FtpActionEditorBase<DeleteAction>
    {
        private CheckBox chkRemoveEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteActionEditor"/> class.
        /// </summary>
        public DeleteActionEditor()
        {
        }

        /// <summary>
        /// Gets the server path description.
        /// </summary>
        protected override string ServerPathDescription
        {
            get { return "The path relative to the server root to delete files from."; }
        }
        /// <summary>
        /// Gets the file mask description.
        /// </summary>
        protected override string FileMaskDescription
        {
            get { return "The files to delete on the server; may include a path and wildcards."; }
        }

        public override string ServerLabel { get { return "Run on server:"; } }

        /// <summary>
        /// Binds the action to the form.
        /// </summary>
        /// <param name="action">The action.</param>
        protected override void BindActionToForm(DeleteAction action)
        {
            this.chkRemoveEmpty.Checked = action.RemoveEmptyDirectories;
        }
        /// <summary>
        /// Creates the action from the form.
        /// </summary>
        /// <returns>New action.</returns>
        protected override DeleteAction CreateActionFromForm()
        {
            return new DeleteAction
            {
                RemoveEmptyDirectories = this.chkRemoveEmpty.Checked
            };
        }
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation
        /// to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.chkRemoveEmpty = new CheckBox { Text = "Remove empty directories" };

            this.Controls.Add(
                new SlimFormField("Directory handling:", this.chkRemoveEmpty)
            );
        }
    }
}
