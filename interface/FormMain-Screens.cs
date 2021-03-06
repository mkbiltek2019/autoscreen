﻿//-----------------------------------------------------------------------
// <copyright file="FormMain-Screens.cs" company="Gavin Kendall">
//     Copyright (c) 2020 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>All the methods for when screens are added, removed, or changed.</summary>
//-----------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoScreenCapture
{
    public partial class FormMain : Form
    {
        /// <summary>
        /// Shows the "Add Screen" window to enable the user to add a chosen Screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addScreen_Click(object sender, EventArgs e)
        {
            formScreen.ScreenObject = null;
            formScreen.ImageFormatCollection = _imageFormatCollection;
            formScreen.ScreenCapture = _screenCapture;
            formScreen.TagCollection = formTag.TagCollection;

            formScreen.ShowDialog(this);

            if (formScreen.DialogResult == DialogResult.OK)
            {
                BuildScreensModule();
                BuildViewTabPages();

                formScreen.ScreenCollection.SaveToXmlFile();
            }
        }

        /// <summary>
        /// Removes the selected Screens from the Screens tab page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeSelectedScreens_Click(object sender, EventArgs e)
        {
            int countBeforeRemoval = formScreen.ScreenCollection.Count;

            foreach (Control control in tabPageScreens.Controls)
            {
                if (control.GetType().Equals(typeof(CheckBox)))
                {
                    CheckBox checkBox = (CheckBox)control;

                    if (checkBox.Checked)
                    {
                        Screen screen = formScreen.ScreenCollection.Get((Screen)checkBox.Tag);
                        formScreen.ScreenCollection.Remove(screen);
                    }
                }
            }

            if (countBeforeRemoval > formScreen.ScreenCollection.Count)
            {
                BuildScreensModule();
                BuildViewTabPages();

                formScreen.ScreenCollection.SaveToXmlFile();
            }
        }

        /// <summary>
        /// Shows the "Change Screen" window to enable the user to edit a chosen Screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeScreen_Click(object sender, EventArgs e)
        {
            Screen screen = new Screen();

            if (sender is Button)
            {
                Button buttonSelected = (Button)sender;
                screen = (Screen)buttonSelected.Tag;
            }

            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem toolStripMenuItemSelected = (ToolStripMenuItem)sender;
                screen = (Screen)toolStripMenuItemSelected.Tag;
            }

            formScreen.ScreenObject = screen;
            formScreen.ImageFormatCollection = _imageFormatCollection;
            formScreen.ScreenCapture = _screenCapture;
            formScreen.TagCollection = formTag.TagCollection;

            formScreen.ShowDialog(this);

            if (formScreen.DialogResult == DialogResult.OK)
            {
                BuildScreensModule();
                BuildViewTabPages();

                formScreen.ScreenCollection.SaveToXmlFile();
            }
        }

        private void removeScreen_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem toolStripMenuItemSelected = (ToolStripMenuItem)sender;
                Screen screenSelected = (Screen)toolStripMenuItemSelected.Tag;

                DialogResult dialogResult = MessageBox.Show("Do you want to remove the screen named \"" + screenSelected.Name + "\"?", "Remove Screen", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    Screen screen = formScreen.ScreenCollection.Get(screenSelected);
                    formScreen.ScreenCollection.Remove(screen);

                    BuildScreensModule();
                    BuildViewTabPages();

                    formScreen.ScreenCollection.SaveToXmlFile();
                }
            }
        }

        private void RunScreenCaptures()
        {
            try
            {
                Log.WriteDebugMessage(":: RunScreenCaptures Start ::");

                foreach (Screen screen in formScreen.ScreenCollection)
                {
                    if (screen.Active)
                    {
                        if (screen.Component == 0)
                        {
                            MacroParser.screenCapture = _screenCapture;

                            // Active Window
                            if (!string.IsNullOrEmpty(_screenCapture.ActiveWindowTitle))
                            {
                                // Do not contiune if the active window title needs to be checked and the active window title
                                // does not contain the text defined in "Active Window Title Capture Text".
                                if (screen.ActiveWindowTitleCaptureCheck && !string.IsNullOrEmpty(screen.ActiveWindowTitleCaptureText) &&
                                    !_screenCapture.ActiveWindowTitle.ToLower().Contains(screen.ActiveWindowTitleCaptureText.ToLower()))
                                {
                                    return;
                                }

                                if (_screenCapture.GetScreenImages(screen.Component, 0, 0, 0, 0, false, screen.ResolutionRatio, out Bitmap bitmap))
                                {
                                    if (_screenCapture.SaveScreenshot(
                                        path: FileSystem.CorrectScreenshotsFolderPath(MacroParser.ParseTags(screen.Folder, formTag.TagCollection)) + MacroParser.ParseTags(preview: false, screen.Name, screen.Macro, screen.Component, screen.Format, _screenCapture.ActiveWindowTitle, formTag.TagCollection),
                                        format: screen.Format,
                                        component: screen.Component,
                                        screenshotType: ScreenshotType.ActiveWindow,
                                        jpegQuality: screen.JpegQuality,
                                        viewId: screen.ViewId,
                                        bitmap: bitmap,
                                        label: checkBoxScreenshotLabel.Checked ? comboBoxScreenshotLabel.Text : string.Empty,
                                        windowTitle: _screenCapture.ActiveWindowTitle,
                                        processName: _screenCapture.ActiveWindowProcessName,
                                        screenshotCollection: _screenshotCollection
                                    ))
                                    {
                                        ScreenshotTakenWithSuccess();
                                    }
                                    else
                                    {
                                        ScreenshotTakenWithFailure();
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (formScreen.ScreenDictionary.ContainsKey(screen.Component))
                            {
                                MacroParser.screenCapture = _screenCapture;

                                if (!string.IsNullOrEmpty(_screenCapture.ActiveWindowTitle))
                                {
                                    // Do not contiune if the active window title needs to be checked and the active window title
                                    // does not contain the text defined in "Active Window Title Capture Text".
                                    if (screen.ActiveWindowTitleCaptureCheck && !string.IsNullOrEmpty(screen.ActiveWindowTitleCaptureText) &&
                                        !_screenCapture.ActiveWindowTitle.ToLower().Contains(screen.ActiveWindowTitleCaptureText.ToLower()))
                                    {
                                        return;
                                    }

                                    // Screen X
                                    if (_screenCapture.GetScreenImages(screen.Component,
                                        formScreen.ScreenDictionary[screen.Component].Bounds.X,
                                        formScreen.ScreenDictionary[screen.Component].Bounds.Y,
                                        formScreen.ScreenDictionary[screen.Component].Bounds.Width,
                                        formScreen.ScreenDictionary[screen.Component].Bounds.Height, screen.Mouse, screen.ResolutionRatio, out Bitmap bitmap))
                                    {
                                        if (_screenCapture.SaveScreenshot(
                                            path: FileSystem.CorrectScreenshotsFolderPath(MacroParser.ParseTags(screen.Folder, formTag.TagCollection)) + MacroParser.ParseTags(preview: false, screen.Name, screen.Macro, screen.Component, screen.Format, _screenCapture.ActiveWindowTitle, formTag.TagCollection),
                                            format: screen.Format,
                                            component: screen.Component,
                                            screenshotType: ScreenshotType.Screen,
                                            jpegQuality: screen.JpegQuality,
                                            viewId: screen.ViewId,
                                            bitmap: bitmap,
                                            label: checkBoxScreenshotLabel.Checked ? comboBoxScreenshotLabel.Text : string.Empty,
                                            windowTitle: _screenCapture.ActiveWindowTitle,
                                            processName: _screenCapture.ActiveWindowProcessName,
                                            screenshotCollection: _screenshotCollection
                                        ))
                                        {
                                            ScreenshotTakenWithSuccess();
                                        }
                                        else
                                        {
                                            ScreenshotTakenWithFailure();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Log.WriteDebugMessage(":: RunScreenCaptures End ::");
            }
            catch (Exception ex)
            {
                Log.WriteExceptionMessage("FormMain-Screens::RunScreenCaptures", ex);
            }
        }
    }
}