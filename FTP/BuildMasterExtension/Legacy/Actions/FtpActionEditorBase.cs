using System;
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
        private SimpleCheckBox chkRecursive;
        private SimpleCheckBox chkLogIndividualFiles;
        private SimpleCheckBox chkForceActiveMode;
        private SimpleCheckBox chkBinaryMode;
        private ValidatingTextBox txtInitializationCommands;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpActionEditorBase{TAction}"/> class.
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
            this.txtInitializationCommands.Text = string.Join(Environment.NewLine, action.InitializationCommands ?? new string[0]);

            this.BindActionToForm(action);
        }
        public sealed override ActionBase CreateFromForm()
        {
            var action = this.CreateActionFromForm();
            action.FtpServer = this.txtServer.Text;
            action.ServerPath = this.txtServerPath.Text;
            action.UserName = this.txtUserName.Text;
            action.Password = this.txtPassword.Text;
            action.FileMask = string.IsNullOrEmpty(this.txtFileMask.Text) ? "*" : this.txtFileMask.Text;
            action.Recursive = this.chkRecursive.Checked;
            action.LogIndividualFiles = this.chkLogIndividualFiles.Checked;
            action.ForceActiveMode = this.chkForceActiveMode.Checked;
            action.BinaryMode = this.chkBinaryMode.Checked;
            action.InitializationCommands = this.txtInitializationCommands.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

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
            this.chkRecursive = new SimpleCheckBox { Text = "Recursive" };
            this.chkLogIndividualFiles = new SimpleCheckBox { Text = "Log individual file transfers" };
            this.chkForceActiveMode = new SimpleCheckBox { Text = "Force Active FTP connection" };
            this.chkBinaryMode = new SimpleCheckBox { Text = "Use Binary Mode for file transmission" };
            this.txtInitializationCommands = new ValidatingTextBox { Required = false, Rows = 5, TextMode = TextBoxMode.MultiLine };

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
                ),
                new SlimFormField("Initialization commands:", this.txtInitializationCommands)
                {
                    HelpText = "These FTP commands, separated by newlines, will be executed on the FTP server before this action is executed. "
                     + "In most cases, this field should be left empty."
                }
            );
        }
    }
}
