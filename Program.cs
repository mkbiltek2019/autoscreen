﻿//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Gavin Kendall">
//     Copyright (c) 2020 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>The main entry for Auto Screen Capture.</summary>
//-----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace AutoScreenCapture
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 1 && !string.IsNullOrEmpty(args[0]) && args[0].Equals("-kill"))
            {
                // Find all instances of autoscreen and kill them.
                foreach (var process in Process.GetProcessesByName("autoscreen"))
                {
                    process.Kill();
                }
            }
            else
            {
                // Parse any command line arguments before we start a new instance so we can issue commands externally
                // such as -debug, -log, -capture, -start, -stop, and -exit to the instance which is already running.
                if (args.Length > 0)
                {
                    ParseCommandLineArguments(args);
                }
                else
                {
                    // Normally we could use the -config command to specify the configuration file to use, but if we
                    // have no commands to parse then we'll load the settings from the default configuration file.
                    Config.Load();

                    Settings.Initialize();
                }

                // This block of code figures out if we're already running an instance of the application.
                using (new Mutex(false, ((GuidAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(GuidAttribute), false)).Value, out bool createdNew))
                {
                    if (createdNew)
                    {
                        // If we're not already running then start a new instance of the application.
                        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new FormMain());
                    }
                    else
                    {
                        if (args.Length == 0 && Convert.ToBoolean(Settings.Application.GetByKey("ShowStartupError", defaultValue: true).Value))
                        {
                            // We've determined that an existing instance is already running. We should write out an error message informing the user.
                            string appVersion = "[(v" + Settings.ApplicationVersion + ") ";

                            FileSystem.AppendToFile(FileSystem.StartupErrorFile, appVersion + DateTime.Now.ToString(MacroParser.DateFormat + " " + MacroParser.TimeFormat) + "] Cannot start " + Settings.ApplicationName + " because an existing instance of the application is already running. To disable this error message set \"ShowStartupError\" to \"False\" in \"" + FileSystem.ApplicationSettingsFile + "\"");
                        }
                    }
                }
            }
        }

        private static void ParseCommandLineArguments(string[] args)
        {
            // Because ordering is important I want to make sure that we pick up the configuration file first.
            // This will avoid scenarios like "autoscreen.exe -debug -config" creating all the default folders
            // and files (thanks to -debug being the first argument) before -config is parsed.

            foreach (string arg in args)
            {
                if (Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_CONFIG))
                {
                    string configFile = Regex.Match(arg, CommandLineRegex.REGEX_COMMAND_LINE_CONFIG).Groups["ConfigFile"].Value;

                    if (configFile.Length > 0)
                    {
                        FileSystem.ConfigFile = configFile;

                        Config.Load();

                        Settings.Initialize();
                    }
                }
            }

            // We didn't get a -config command line argument so just load the default config.
            if (Settings.Application == null)
            {
                Config.Load();

                Settings.Initialize();
            }

            // Load user settings.
            if (string.IsNullOrEmpty(FileSystem.UserSettingsFile))
            {
                Config.Load();

                Settings.Initialize();
            }

            // All of these commands can be externally issued to an already running instance.
            // The current running instance monitors the command file for the commands in the file.
            foreach (string arg in args)
            {
                if (Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_DEBUG) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_DEBUG_ON) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_DEBUG_OFF) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_LOG) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_LOG_ON) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_LOG_OFF) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_CAPTURE) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_START) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_STOP) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_EXIT) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_SHOW_SYSTEM_TRAY_ICON) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_HIDE_SYSTEM_TRAY_ICON) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_INITIAL) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_INITIAL_ON) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_INITIAL_OFF) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_LIMIT) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_INTERVAL) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_PASSPHRASE) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_STARTAT) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_STOPAT) ||
                    Regex.IsMatch(arg, CommandLineRegex.REGEX_COMMAND_LINE_CAPTUREAT))
                {
                    FileSystem.AppendToFile(FileSystem.CommandFile, arg);
                }
            }
        }
    }
}