﻿//-----------------------------------------------------------------------
// <copyright file="FormMain.cs" company="Gavin Kendall">
//     Copyright (c) 2020 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>The main interface form for setting up sub-forms, showing the interface, hiding the interface, displaying dates in the calendar, and searching for screenshots.</summary>
//-----------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.Generic;

namespace AutoScreenCapture
{
    /// <summary>
    /// The application's main window.
    /// </summary>
    public partial class FormMain : Form
    {
        // The various forms that are used for modules.
        private FormEditor formEditor = new FormEditor();
        private FormTrigger formTrigger = new FormTrigger();
        private FormRegion formRegion = new FormRegion();
        private FormScreen formScreen = new FormScreen();
        private FormTag formTag = new FormTag();
        private FormEnterPassphrase formEnterPassphrase = new FormEnterPassphrase();
        private FormSchedule formSchedule = new FormSchedule();
        private HotKeyMap hotKeyMap = new HotKeyMap();

        private ScreenCapture _screenCapture;
        private ImageFormatCollection _imageFormatCollection;
        private ScreenshotCollection _screenshotCollection;

        /// <summary>
        /// Threads for background operations.
        /// </summary>
        private BackgroundWorker runScreenshotSearchThread = null;
        private BackgroundWorker runDateSearchThread = null;
        private BackgroundWorker runDeleteSlidesThread = null;
        private BackgroundWorker runFilterSearchThread = null;
        private BackgroundWorker runSaveScreenshotsThread = null;

        /// <summary>
        /// Delegates for the threads.
        /// </summary>
        private delegate void RunSlideSearchDelegate(DoWorkEventArgs e);
        private delegate void RunDateSearchDelegate(DoWorkEventArgs e);
        private delegate void RunTitleSearchDelegate(DoWorkEventArgs e);

        /// <summary>
        /// Default settings used by the command line parser.
        /// </summary>
        private const int CAPTURE_LIMIT_MIN = 0;
        private const int CAPTURE_LIMIT_MAX = 9999;

        /// <summary>
        /// Constructor for the main form.
        /// </summary>
        public FormMain()
        {
            InitializeComponent();

            hotKeyMap.KeyPressed +=  new EventHandler<KeyPressedEventArgs>(hotKey_KeyPressed);
            hotKeyMap.RegisterHotKey(AutoScreenCapture.ModifierKeys.Control | AutoScreenCapture.ModifierKeys.Alt, Keys.Z);
            hotKeyMap.RegisterHotKey(AutoScreenCapture.ModifierKeys.Control | AutoScreenCapture.ModifierKeys.Alt, Keys.X);
            hotKeyMap.RegisterHotKey(AutoScreenCapture.ModifierKeys.Control | AutoScreenCapture.ModifierKeys.Alt, Keys.A);
            hotKeyMap.RegisterHotKey(AutoScreenCapture.ModifierKeys.Control | AutoScreenCapture.ModifierKeys.Alt, Keys.E);

            LoadSettings();

            Text = (string)Settings.Application.GetByKey("Name", defaultValue: Settings.ApplicationName).Value;

            // Get rid of the old "slides" directory that may still remain from an old version of the application.
            DeleteSlides();
        }

        /// <summary>
        /// When this form loads we'll need to delete slides and then search for dates and slides.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormMain_Load(object sender, EventArgs e)
        {
            HelpMessage("Welcome to " +
                Settings.Application.GetByKey("Name", defaultValue: Settings.ApplicationName).Value + " " +
                Settings.Application.GetByKey("Version", defaultValue: Settings.ApplicationVersion).Value);

            // Start the scheduled capture timer.
            timerScheduledCapture.Interval = 1000;
            timerScheduledCapture.Enabled = true;
            timerScheduledCapture.Start();

            LoadHelpTips();

            ShowInfo();

            SearchFilterValues();
            SearchDates();
            SearchScreenshots();

            Log.WriteDebugMessage("Running triggers of condition type ApplicationStartup");
            RunTriggersOfConditionType(TriggerConditionType.ApplicationStartup);
        }

        /// <summary>
        /// When this form is closing we can either exit the application or just close this window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormViewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                DisableStopCapture();
                EnableStartCapture();

                _screenCapture.Count = 0;
                _screenCapture.Running = false;

                SystemTrayIconStatusNormal();
                HideSystemTrayIcon();

                Log.WriteDebugMessage("Hiding interface on forced application exit because Windows is shutting down");
                HideInterface();

                Log.WriteDebugMessage("Saving screenshots on forced application exit because Windows is shutting down");
                _screenshotCollection.SaveToXmlFile((int)numericUpDownKeepScreenshotsForDays.Value);

                if (runDateSearchThread != null && runDateSearchThread.IsBusy)
                {
                    runDateSearchThread.CancelAsync();
                }

                if (runScreenshotSearchThread != null && runScreenshotSearchThread.IsBusy)
                {
                    runScreenshotSearchThread.CancelAsync();
                }

                Log.WriteMessage("Bye!");

                // Exit.
                Environment.Exit(0);
            }
            else
            {
                Log.WriteDebugMessage("Running triggers of condition type InterfaceClosing");
                RunTriggersOfConditionType(TriggerConditionType.InterfaceClosing);

                // If there isn't a Trigger for "InterfaceClosing" that performs an action
                // then make sure we cancel this event so that nothing happens. We want the user
                // to use a Trigger, and decide what they want to do, when closing the interface window.
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Searches for dates. They should be in the format yyyy-mm-dd.
        /// </summary>
        private void SearchDates()
        {
            Log.WriteDebugMessage("Searching for dates");

            if (runDateSearchThread == null)
            {
                runDateSearchThread = new BackgroundWorker
                {
                    WorkerReportsProgress = false,
                    WorkerSupportsCancellation = true
                };

                runDateSearchThread.DoWork += new DoWorkEventHandler(DoWork_runDateSearchThread);
            }
            else
            {
                if (!runDateSearchThread.IsBusy)
                {
                    runDateSearchThread.RunWorkerAsync();
                }
            }
        }

        private void DeleteSlides()
        {
            Log.WriteDebugMessage("Deleting slides directory from old version of application (if needed)");

            if (runDeleteSlidesThread == null)
            {
                runDeleteSlidesThread = new BackgroundWorker
                {
                    WorkerReportsProgress = false,
                    WorkerSupportsCancellation = true
                };

                runDeleteSlidesThread.DoWork += new DoWorkEventHandler(DoWork_runDeleteSlidesThread);
            }
            else
            {
                if (!runDeleteSlidesThread.IsBusy)
                {
                    runDeleteSlidesThread.RunWorkerAsync();
                }
            }
        }

        /// <summary>
        /// This thread is responsible for figuring out what days screenshots were taken.
        /// </summary>
        /// <param name="e"></param>
        private void RunDateSearch(DoWorkEventArgs e)
        {
            if (monthCalendar.InvokeRequired)
            {
                monthCalendar.Invoke(new RunDateSearchDelegate(RunDateSearch), new object[] {e});
            }
            else
            {
                if (_screenshotCollection != null)
                {
                    List<string> dates = new List<string>();
                    dates = _screenshotCollection.GetDatesByFilter(comboBoxFilterType.Text, comboBoxFilterValue.Text);

                    DateTime[] boldedDates = new DateTime[dates.Count];

                    for (int i = 0; i < dates.Count; i++)
                    {
                        boldedDates.SetValue(ConvertDateStringToDateTime(dates[i].ToString()), i);
                    }

                    monthCalendar.BoldedDates = boldedDates;
                }
            }
        }

        /// <summary>
        /// This thread is responsible for deleting all the slides remaining from an old version of the application
        /// since we no longer use slides or support the Slideshow module going forward.
        /// </summary>
        /// <param name="e"></param>
        private void RunDeleteSlides(DoWorkEventArgs e)
        {
            FileSystem.DeleteFilesInDirectory(FileSystem.SlidesFolder);
        }

        /// <summary>
        /// Shows the interface.
        /// </summary>
        private void ShowInterface()
        {
            try
            {
                Log.WriteDebugMessage("Showing interface");

                if (ScreenCapture.LockScreenCaptureSession && !formEnterPassphrase.Visible)
                {
                    Log.WriteDebugMessage("Screen capture session is locked. Challenging user to enter correct passphrase to unlock");
                    formEnterPassphrase.ShowDialog(this);
                }

                // This is intentional. Do not rewrite these statements as an if/else
                // because as soon as lockScreenCaptureSession is set to false we want
                // to continue with normal functionality.
                if (!ScreenCapture.LockScreenCaptureSession)
                {
                    Settings.User.GetByKey("StringPassphrase", defaultValue: false).Value = string.Empty;
                    SaveSettings();

                    Opacity = 100;

                    SearchDates();
                    SearchScreenshots();

                    PopulateLabelList();

                    Show();

                    Visible = true;
                    ShowInTaskbar = true;

                    // If the window is mimimized then show it when the user wants to open the window.
                    if (WindowState == FormWindowState.Minimized)
                    {
                        WindowState = FormWindowState.Normal;
                    }

                    Focus();

                    Log.WriteDebugMessage("Running triggers of condition type InterfaceShowing");
                    RunTriggersOfConditionType(TriggerConditionType.InterfaceShowing);
                }
            }
            catch (Exception ex)
            {
                Log.WriteExceptionMessage("FormMain::ShowInterface", ex);
            }
        }

        /// <summary>
        /// Hides the interface.
        /// </summary>
        private void HideInterface()
        {
            try
            {
                Log.WriteDebugMessage("Hiding interface");

                Opacity = 0;

                Hide();
                Visible = false;
                ShowInTaskbar = false;

                // Show a balloon tip (if necessary) when the interface is being hidden but the current screen capture session isn't running.
                if (!_screenCapture.Running)
                {
                    SystemTrayBalloonMessage("The application is available in your system tray. To exit, right-click its system tray icon and select Exit");
                }

                Log.WriteDebugMessage("Running triggers of condition type InterfaceHiding");
                RunTriggersOfConditionType(TriggerConditionType.InterfaceHiding);
            }
            catch (Exception ex)
            {
                Log.WriteExceptionMessage("FormMain::HideInterface", ex);
            }
        }

        /// <summary>
        /// Runs the date search thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork_runDateSearchThread(object sender, DoWorkEventArgs e)
        {
            RunDateSearch(e);
        }

        /// <summary>
        /// Runs the delete slides thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoWork_runDeleteSlidesThread(object sender, DoWorkEventArgs e)
        {
            RunDeleteSlides(e);
        }

        /// <summary>
        /// Shows the interface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemShowInterface_Click(object sender, EventArgs e)
        {
            ShowInterface();
        }

        /// <summary>
        /// Hides the interface.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemHideInterface_Click(object sender, EventArgs e)
        {
            HideInterface();
        }

        /// <summary>
        /// Shows the "About" window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItemAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                Settings.Application.GetByKey("Name", defaultValue: Settings.ApplicationName).Value + " " +
                Settings.Application.GetByKey("Version", defaultValue: Settings.ApplicationVersion).Value +
                " (\"" + Settings.ApplicationCodename + "\")\nDeveloped by Gavin Kendall (2008 - 2020)", "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}