#if RIDER
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.RdBackend.Common.Features.ProjectModel.CustomTools;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace ReSharperPlugin.ResXFileCodeGeneratorEx
{
    // ------------------------------------------------------------------
    // Shared base — handles Execute() and all ISingleFileCustomTool props
    // that don't vary between public and internal variants.
    // ------------------------------------------------------------------

    public abstract class ResXFileCodeGeneratorExToolBase : ISingleFileCustomTool
    {
        // Subclasses supply these two values
        protected abstract bool   IsInternal   { get; }
        public    abstract string[] CustomTools { get; }

        public string   Name       => IsInternal ? "ResX File Code Generator Ex (internal)" : "ResX File Code Generator Ex";
        public string   ActionName => IsInternal ? "Run ResXFileCodeGeneratorExInternal"     : "Run ResXFileCodeGeneratorEx";
        public IconId   Icon       => null!;
        public string[] Extensions => new[] { ".resx" };
        public string[] Keywords   => new[] { "resx", "resource", "generator", "code" };
        public bool     IsEnabled  => true;

        public bool IsApplicable(IProjectFile projectFile) =>
            ".resx".Equals(
                projectFile.Location.ExtensionWithDot,
                StringComparison.OrdinalIgnoreCase);

        public ISingleFileCustomToolExecutionResult Execute(IProjectFile projectFile)
        {
            var errors = new List<string>();

            try
            {
                var resxPath    = projectFile.Location;
                var resxContent = File.ReadAllText(resxPath.FullPath);

                // Class name: strip ".resx", also strip trailing ".Designer" if present.
                var className = resxPath.NameWithoutExtension;
                if (className.EndsWith(".Designer", StringComparison.OrdinalIgnoreCase))
                    className = className.Substring(0, className.Length - ".Designer".Length);

                var project       = projectFile.GetProject();
                var namespaceName = BuildNamespace(project, resxPath);

                var code = ResXCodeGenerator.Generate(
                    resxContent, namespaceName, className, IsInternal);

                var outputPath = resxPath.Directory.Combine(className + ".Designer.cs");

                // Write the Designer through Rider's VFS inside an EnsureWritable modification
                // cookie (not raw System.IO.File) so the write participates in the VFS/model.
                SingleFileCustomToolExtensions.WriteToDisk(
                    projectFile.GetSolution(), new List<VirtualFileSystemPath>(), errors,
                    (outputPath, code));
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
            }

            // Deliberately report NO affected files. If the Designer path were returned, the
            // custom-tool manager's PostProcess invalidates the SymbolCache under a
            // WriteLockCookie. When Rider invokes this tool synchronously inside the
            // Rename-Resource refactoring (which holds a non-interruptible read lock for its
            // conflict search), that write lock can never be acquired and the EDT freezes ~20s
            // in acquireWriteIntentPermit. The VFS write above already propagates the change;
            // the daemon reparses the regenerated Designer through the normal file-change path.
            return new SingleFileCustomToolExecutionResult(
                new List<VirtualFileSystemPath>(), errors);
        }

        // ------------------------------------------------------------------
        // Namespace resolution
        // ------------------------------------------------------------------

        private static string BuildNamespace(IProject? project, VirtualFileSystemPath resxPath)
        {
            if (project is null) return string.Empty;

            var defaultNs =
                (project.ProjectProperties.BuildSettings as IManagedProjectBuildSettings)
                    ?.DefaultNamespace ?? string.Empty;

            var projectDir = project.ProjectFileLocation.Directory.FullPath
                .TrimEnd(Path.DirectorySeparatorChar, '/');
            var fileDir    = resxPath.Directory.FullPath
                .TrimEnd(Path.DirectorySeparatorChar, '/');

            string relativeNs;
            if (fileDir.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase)
                && fileDir.Length > projectDir.Length)
            {
                var relative = fileDir.Substring(projectDir.Length)
                    .TrimStart(Path.DirectorySeparatorChar, '/');
                relativeNs = relative
                    .Replace(Path.DirectorySeparatorChar, '.')
                    .Replace('/', '.');
            }
            else
            {
                relativeNs = string.Empty;
            }

            if (string.IsNullOrEmpty(relativeNs)) return defaultNs;
            if (string.IsNullOrEmpty(defaultNs))  return relativeNs;
            return $"{defaultNs}.{relativeNs}";
        }
    }

    // ------------------------------------------------------------------
    // Public-access generator — matches <Generator>ResXFileCodeGeneratorEx</Generator>
    // ------------------------------------------------------------------

    [ShellComponent]
    public sealed class ResXFileCodeGeneratorExTool : ResXFileCodeGeneratorExToolBase
    {
        public override string[] CustomTools => new[] { "ResXFileCodeGeneratorEx" };
        protected override bool  IsInternal  => false;
    }

    // ------------------------------------------------------------------
    // Internal-access generator — matches <Generator>ResXFileCodeGeneratorExInternal</Generator>
    // ------------------------------------------------------------------

    [ShellComponent]
    public sealed class ResXFileCodeGeneratorExInternalTool : ResXFileCodeGeneratorExToolBase
    {
        public override string[] CustomTools => new[] { "ResXFileCodeGeneratorExInternal" };
        protected override bool  IsInternal  => true;
    }
}
#endif
