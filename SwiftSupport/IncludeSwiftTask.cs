﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SwiftSupport.Shared;

using Parallel = System.Threading.Tasks.Parallel;

namespace SwiftSupport
{
    public class IncludeSwiftTask : BaseIncludeSwiftTask
    {
        [Required]
        public ITaskItem[] Resources { get; set; }

        private string[] AvailableArchs()
        {
            var firstItemPath = Resources?.FirstOrDefault()?.ItemSpec;
            if (string.IsNullOrWhiteSpace(firstItemPath))
            {
                return new string[0];
            }

            var archs = RunLipo($"'{Path.Combine(GetRuntimePath(), firstItemPath)}' -archs");

            return archs.Split(' ').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();
        }

        public override bool Execute()
        {
            var arcs = MtouchArch.Split(',').Select(c => c.Trim().ToLower()).ToList(); // lipo uses lower case
            var availableArchs = AvailableArchs();

            Log.LogMessage(MessageImportance.Normal, $"Copying: {string.Join(", ", Resources.Select(c => c.ItemSpec))}");
            Log.LogMessage(MessageImportance.Normal, $"Swift Arcs Needed: {MtouchArch}");
            Log.LogMessage(MessageImportance.Normal, $"Swift Arcs Available: {string.Join(", ", availableArchs)}");

            var args = GetLipoArgs(availableArchs);
            var xcodePath = GetRuntimePath();

            Parallel.ForEach(Resources.Select(c => c.ItemSpec), (dylib) =>
            {
                Log.LogMessage(MessageImportance.Normal, $"Copying: {dylib}");
                RunLipo($"'{Path.Combine(xcodePath, dylib)}' {args} '{GetOutputPath(dylib)}'");
            });

            return true;
        }
    }
}