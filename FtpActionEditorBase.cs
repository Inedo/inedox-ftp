using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.FTP
{
    /// <summary>
    /// Abstract base class for FTP action editors.
    /// </summary>
    /// <typeparam name="TAction">Type of the FTP action.</typeparam>
    internal abstract class FtpActionEditorBase<TAction> : ActionEditorBase
        where TAction : FtpActionBase
    {
        private ValidatingTextBox txtServer;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtServerPath;
        private ValidatingTextBox txtFileMask;
        private CheckBox chkRecursive;
        private CheckBox chkLogIndividualFiles;
        private CheckBox chkForceActiveMode;
        private CheckBox chkBinaryMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpActionEditorBase&lt;TAction&gt;"/> class.
        /// </summary>
        protected FtpActionEditorBase()
        {
        }

        public abstract override string ServerLabel { get; }

        /// <summary>
        /// Gets the server path description.
        /// </summary>
        protected abstract string ServerPathDescription { get; }
        /// <summary>
        /// Gets the file mask description.
        /// </summary>
        protected abstract string FileMaskDescription { get; }

        public sealed override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var action = (TAction)extension;
            this.txtServer.Text = action.FtpServer;
            this.txtServerPath.Text = action.ServerPath;
            this.txtUserName.Text = action.UserName;
            this.txtPassword.Text = action.Password;
            this.txtFileMask.Text = action.FileMask;
            this.chkRecursive.Checked = action.Recursive;
            this.chkLogIndividualFiles.Checked = action.LogIndividualFiles;
            this.chkForceActiveMode.Checked = action.ForceActiveMode;
            this.chkBinaryMode.Checked = action.BinaryMode;

            BindActionToForm(action);
        }
        public sealed override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            var action = CreateActionFromForm();
            action.FtpServer = this.txtServer.Text;
            action.ServerPath = this.txtServerPath.Text;
            action.UserName = this.txtUserName.Text;
            action.Password = this.txtPassword.Text;
            action.FileMask = string.IsNullOrEmpty(this.txtFileMask.Text) ? "*" : this.txtFileMask.Text;
            action.Recursive = this.chkRecursive.Checked;
            action.LogIndividualFiles = this.chkLogIndividualFiles.Checked;
            action.ForceActiveMode = this.chkForceActiveMode.Checked;
            action.BinaryMode = this.chkBinaryMode.Checked;

            return action;
        }

        /// <summary>
        /// Binds the action to the form.
        /// </summary>
        /// <param name="action">The action.</param>
        protected abstract void BindActionToForm(TAction action);
        /// <summary>
        /// Creates the action from the form.
        /// </summary>
        /// <returns>New action.</returns>
        protected abstract TAction CreateActionFromForm();
        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation
        /// to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtServer = new ValidatingTextBox { Required = true };
            this.txtUserName = new ValidatingTextBox { DefaultText = "anonymous" };
            this.txtPassword = new PasswordTextBox();
            this.txtServerPath = new ValidatingTextBox();
            this.txtFileMask = new ValidatingTextBox { DefaultText = "*" };
            this.chkRecursive = new CheckBox { Text = "Recursive" };
            this.chkLogIndividualFiles = new CheckBox { Text = "Log individual file transfers" };
            this.chkForceActiveMode = new CheckBox { Text = "Force Active FTP connection" };
            this.chkBinaryMode = new CheckBox { Text = "Use Binary Mode for file transmission" };

            this.Controls.Add(
                new SlimFormField("FTP server host:", this.txtServer),
                new SlimFormField("User name:", this.txtUserName),
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("FTP server root path:", this.txtServerPath)
                {
                    HelpText = this.ServerPathDescription
                },
                new SlimFormField("File mask:", this.txtFileMask)
                {
                    HelpText = this.FileMaskDescription
                },
                new SlimFormField("File transmission mode:", this.chkBinaryMode)
                {
                    HelpText = "If checked, any files transferred will use Binary Mode (i.e. byte-for-byte); "
                    + "otherwise any transferred files will be transferred in ASCII Mode and line endings will be converted "
                    + "if the source and target platforms are different."
                },
                new SlimFormField(
                    "Additional options:",
                    new Div(this.chkRecursive),
                    new Div(this.chkLogIndividualFiles),
                    new Div(this.chkForceActiveMode)
                )
            );
        }
    }
}
