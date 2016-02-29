using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace BuildTools
{
    public partial class BuildTools : Form {
        // Delegates
        private delegate void AppendTextDelegate(string text);

        private readonly AppendTextDelegate _appendDelegate;

        private delegate void AppendRawTextDelegat(string text);

        private readonly AppendRawTextDelegat _appendRawDelegate;

        private delegate void DisableDelegate();

        private readonly DisableDelegate _disableDelegate;

        private delegate void EnableDelegate();

        private readonly EnableDelegate _enableDelegate;

        private delegate void ShowProgressDelegate();

        private readonly ShowProgressDelegate _showProgressDelegate;

        private delegate void HideProgressDelegate();

        private readonly HideProgressDelegate _hideProgressDelegate;

        private delegate void IndeterminateProgressDelegate();

        private readonly IndeterminateProgressDelegate _indeterminateProgressDelegate;

        private delegate void ProgressPercentDelegate(int place, int total);

        private readonly ProgressPercentDelegate _progressPercentDelegate;

        // Stuff
        private readonly Runner _runner;
        private string _lastLog = "";

        /// <summary>
        /// Constructor for the form
        /// </summary>
        public BuildTools() {
            InitializeComponent();
            _runner = new Runner(this);
            undoBT.Visible = false;
            progress.Visible = false;

            // delegates
            _appendDelegate = AppendText;
            _appendRawDelegate = AppendRawText;
            _disableDelegate = Disable;
            _enableDelegate = Enable;
            _showProgressDelegate = ProgressShow;
            _hideProgressDelegate = ProgressHide;
            _indeterminateProgressDelegate = ProgressIndeterminate;
            _progressPercentDelegate = Progress;
        }

        // Run BuildTools Button Clicked
        private void runBT_Click(object sender, EventArgs e) {
            bool update = autoUpdateCB.Checked;
            Thread thread = new Thread(delegate() {
                _runner.RunBuildTools(update);
                Enable();
                ProgressHide();
            });
            Disable();
            thread.Start();
        }

        // Update BuildTools Button Clicked
        private void updateBT_Click(object sender, EventArgs e) {
            Thread thread = new Thread(delegate() {
                _runner.UpdateJar();
                Enable();
                ProgressHide();
            });
            Disable();
            thread.Start();
        }

        // Clear Log Button Clicked
        private void clearBT_Click(object sender, EventArgs e) {
            _lastLog += outputTB.Text;
            outputTB.Text = "";
            undoBT.Visible = true;
        }

        // Undo Button Clicked
        private void undoBT_Click(object sender, EventArgs e) {
            outputTB.Text = _lastLog + outputTB.Text;
            _lastLog = "";
            undoBT.Visible = false;
        }

        /// <summary>
        /// Append text to the output textbox. This method will ensure thread safety on the AppendText method call.
        /// This will also prefix a "--- " before the given text and automatically append the given text with a new-line character.
        /// </summary>
        /// <param name="text">Text to append to the output textbox.</param>
        public void AppendText(string text) {
            if (InvokeRequired) {
                Invoke(_appendDelegate, text);
            }
            else {
                outputTB.AppendText("--- " + text + "\n");
                outputTB.ScrollToCaret();
            }
        }

        /// <summary>
        /// Send one line of text to the output textbox, with no "---" prefix. A newline will be added to the end of the line.
        /// </summary>
        /// <param name="text">Text to append to the output textbox.</param>
        public void AppendRawText(string text) {
            if (InvokeRequired) {
                Invoke(_appendRawDelegate, text);
            }
            else {
                outputTB.AppendText(text + "\n");
                outputTB.ScrollToCaret();
            }
        }

        /// <summary>
        /// Disable the run buttons so they can't be pressed while work is being done.
        /// </summary>
        private void Disable() {
            if (InvokeRequired) {
                Invoke(_disableDelegate);
            }
            else {
                updateBT.Enabled = false;
                runBT.Enabled = false;
            }

        }

        /// <summary>
        /// Enable the run buttons so they will work after work has completed
        /// </summary>
        private void Enable() {
            if (InvokeRequired) {
                Invoke(_enableDelegate);
            }
            else {
                updateBT.Enabled = true;
                runBT.Enabled = true;
            }
        }

        /// <summary>
        /// Show the progress bar.
        /// This needs to be done before other calls are made to it.
        /// </summary>
        public void ProgressShow() {
            if (InvokeRequired) {
                Invoke(_showProgressDelegate);
            }
            else {
                progress.Visible = true;
            }
        }

        /// <summary>
        /// Hide the progress bar so it won't be visible
        /// </summary>
        public void ProgressHide() {
            if (InvokeRequired) {
                Invoke(_hideProgressDelegate);
            }
            else {
                progress.Visible = false;
            }
        }

        /// <summary>
        /// Set the progress bar's state to indeterminate.
        /// </summary>
        public void ProgressIndeterminate() {
            if (InvokeRequired) {
                Invoke(_indeterminateProgressDelegate);
            }
            else {
                progress.Style = ProgressBarStyle.Marquee;
            }
        }

        /// <summary>
        /// Set the progress bar to the specified place.
        /// </summary>
        /// <param name="place">Current progress to this point</param>
        /// <param name="total">Total progress to completion</param>
        public void Progress(int place, int total) {
            if (InvokeRequired) {
                Invoke(_progressPercentDelegate, place, total);
            }
            else {
                progress.Style = ProgressBarStyle.Continuous;
                progress.Minimum = 0;
                progress.Maximum = total;
                progress.Value = place;
            }
        }

        // Exit button pressed
        private void BuildTools_FormClosed(object sender, FormClosedEventArgs e) {
            _runner.CleanUp();
            Application.Exit();
            Environment.Exit(0);
        }
    }
}
