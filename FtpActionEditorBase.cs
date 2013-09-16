using System.Web;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

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
        private TextBox txtUserName;
        private PasswordTextBox txtPassword;
        private TextBox txtServerPath;
        private ValidatingTextBox txtFileMask;
        private CheckBox chkRecursive;
        private CheckBox chkLogIndividualFiles;
        private CheckBox chkForceActiveMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="FtpActionEditorBase&lt;TAction&gt;"/> class.
        /// </summary>
        protected FtpActionEditorBase()
        {
        }

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
            this.txtServer.Text = action.FtpServer ?? "";
            this.txtServerPath.Text = action.ServerPath ?? "";
            this.txtUserName.Text = action.UserName ?? "";
            this.txtPassword.Text = action.Password ?? "";
            this.txtFileMask.Text = action.FileMask ?? "";
            this.chkRecursive.Checked = action.Recursive;
            this.chkLogIndividualFiles.Checked = action.LogIndividualFiles;
            this.chkForceActiveMode.Checked = action.ForceActiveMode;

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
            action.FileMask = this.txtFileMask.Text;
            action.Recursive = this.chkRecursive.Checked;
            action.LogIndividualFiles = this.chkLogIndividualFiles.Checked;
            action.ForceActiveMode = this.chkForceActiveMode.Checked;

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

            this.txtServer = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.txtUserName = new TextBox
            {
                Width = 300
            };

            this.txtPassword = new PasswordTextBox
            {
                Width = 250
            };

            this.txtServerPath = new TextBox
            {
                Width = 300
            };

            this.txtFileMask = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Recursive"
            };

            this.chkLogIndividualFiles = new CheckBox
            {
                Text = "Log Individual Files"
            };

            this.chkForceActiveMode = new CheckBox()
            {
                Text = "Force Active FTP Connection"
            };

            CUtil.Add(this,
                new RenderJQueryDocReadyDelegator(w => w.WriteLine("$('#" + this.txtUserName.ClientID + "').inedobm_defaulter({defaultText:'anonymous'});")),
                new FormFieldGroup(
                    "FTP Server",
                    "Provide the FTP server to connect to and log on information if it is required.",
                    false,
                    new StandardFormField(
                        "Server Name/Address:",
                        this.txtServer
                    ),
                    new StandardFormField(
                        "User Name:",
                        this.txtUserName
                    ),
                    new StandardFormField(
                        "Password:",
                        this.txtPassword
                    )
                ),
                new FormFieldGroup(
                    "Server Root Path",
                    HttpUtility.HtmlEncode(this.ServerPathDescription),
                    false,
                    new StandardFormField(
                        "Server Path:",
                        this.txtServerPath
                    )
                ),
                new FormFieldGroup(
                    "Files",
                    HttpUtility.HtmlEncode(this.FileMaskDescription),
                    false,
                    new StandardFormField(
                        "Files:",
                        this.txtFileMask
                    )
                ),
                new FormFieldGroup(
                    "Options",
                    "Provide additional configuration settings for the FTP operation.",
                    false,
                    new StandardFormField(
                        "",
                        this.chkRecursive
                    ),
                    new StandardFormField(
                        "",
                        this.chkLogIndividualFiles
                    ),
                    new StandardFormField(
                        "",
                        this.chkForceActiveMode
                    )
                )
            );
        }
    }
}
