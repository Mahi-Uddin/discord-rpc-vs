﻿//------------------------------------------------------------------------------
// <copyright file="DiscordRPCVSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE;
using System.IO;
using System.Collections.Generic;

namespace discord_rpc_vs
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoadAttribute("{F1536EF8-92EC-443C-9ED7-FDADF150DA82}")]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(DiscordRPCVSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class DiscordRPCVSPackage : Package
    {
        /// <summary>
        /// DiscordRPCVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ea99cd90-97ea-40a5-be3c-2f3377242800";

        private DiscordController DiscordController { get; set; } = new DiscordController();

        private DTE _dte;
        private Events _dteEvents;
        private DocumentEvents _documentEvents;

        private Dictionary<string, string> Languages = new Dictionary<string, string>()
        {
            { ".cs", "c-sharp"},
            { ".cpp", "cpp" },
            { ".py" , "python"},
            { ".js", "javascript" },
            { ".html", "html" },
            { ".css", "css"},
            { ".java", "java" },
            { ".go", "go" },
            { ".php", "php" },
            { ".c", "c" },
            { ".class", "java" },
        };
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscordRPCVS"/> class.
        /// </summary>
        public DiscordRPCVSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            DiscordRPCVS.Initialize(this);
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            _dte = (DTE)GetService(typeof(SDTE));
            _dteEvents = _dte.Events;
            _dteEvents.WindowEvents.WindowActivated += OnWindowSwitch;

            DiscordController.Initialize();
            DiscordController.presence = new DiscordRPC.RichPresence()
            {
                details = "Idle",
                state = "Looking for a project",
                largeImageKey = "visualstudio",
                largeImageText = "Visual Studio",
                smallImageKey = "smallvs",

                startTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
            };

            DiscordRPC.UpdatePresence(ref DiscordController.presence);
            base.Initialize();
        }

        /// <summary>
        ///     When switching between windows
        /// </summary>
        /// <param name="windowActivated"></param>
        /// <param name="lastWindow"></param>
        private void OnWindowSwitch(Window windowActivated, Window lastWindow)
        {
            // Get Extension
            var ext = Path.GetExtension(windowActivated.Document.FullName);

            // Update the RichPresence
            DiscordController.presence = new DiscordRPC.RichPresence()
            {
                details = Path.GetFileName(GetExactPathName(windowActivated.Document.FullName)),
                state = "Developing " + Path.GetFileNameWithoutExtension(_dte.Solution.FileName),
                largeImageKey = "visualstudio",
                largeImageText = "Visual Studio",
                smallImageKey =  (Languages.ContainsKey(ext)) ? Languages[ext] : "smallvs",
                smallImageText = (Languages.ContainsKey(ext)) ? Languages[ext] : "",
                startTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds
            };

            DiscordRPC.UpdatePresence(ref DiscordController.presence);
        }

        /// <summary>
        ///     Gets path name with correct casing
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public static string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }

        #endregion
    }
}
