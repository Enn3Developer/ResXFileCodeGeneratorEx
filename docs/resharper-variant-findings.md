# ReSharper variant — feasibility findings

**Question:** Can the ReSharper (non-Rider) `.nupkg` register a `ResXFileCodeGeneratorEx`
single-file generator, the way the Rider variant does?

**Answer: No — not through the ReSharper SDK plugin model.** Giving Visual Studio users this
custom tool would require a *separate* Visual Studio extension (VSIX), which is out of scope of this
ReSharper plugin and not currently planned.

## Evidence

The Rider variant's tool (`src/dotnet/.../ResXFileCodeGeneratorExTool.cs`, entirely under `#if RIDER`)
implements `JetBrains.RdBackend.Common.Features.ProjectModel.CustomTools.ISingleFileCustomTool`.

Scanning the local SDK NuGet caches (`~/.nuget/packages`, SDK `2025.3.0.1`):

1. **`ISingleFileCustomTool` and the whole `RdBackend.Common.Features.ProjectModel.CustomTools`
   namespace exist only in `JetBrains.RdBackend.Common.dll`**, shipped by the Rider-only
   `jetbrains.rider.rdbackend.common` package. It is not referenced by the ReSharper SDK
   (`JetBrains.ReSharper.SDK` → `JetBrains.ReSharper.SDK.Internal`, which does not depend on
   `rdbackend.common`).

2. **The ReSharper SDK public-API catalog contains no single-file-generator surface.** The SDK's
   public API listing (`PublicApiMetadata` inside
   `JetBrains.ReSharper.SDK.Internal.JetMetadata.sstg`) has **zero** hits for `SingleFileGenerator`,
   `CustomTool`, `ICustomTool`, `IVsSingleFileGenerator`, or `GeneratedFile`, and no `.resx`-specific
   API. The only `*Generator` types are ASP.NET code-behind generators
   (`AspCodeBehindGenerator`), which are not a user-facing custom-tool registration mechanism.
   (Known ReSharper types — `IProjectFile`, `ShellComponent`, `ZoneDefinition`, `IZone` — *do*
   appear, confirming the catalog is complete and the absence is real.)

## Why

`RdBackend` is the Rider **backend host** — Rider reimplemented Visual Studio's "Custom Tool"
(single-file generator) feature inside its own headless backend, which is why `ISingleFileCustomTool`
exists there. Standalone ReSharper runs *inside Visual Studio*, which already owns that feature
natively: a single-file generator is a VS extensibility component (`IVsSingleFileGenerator`,
COM/registry-registered, typically via a VSIX with `CodeGeneratorRegistration`). That is a **Visual
Studio SDK** concern, not a ReSharper SDK one. A ReSharper plugin delivered as a `.nupkg` through the
ReSharper extension manager cannot register a VS single-file generator.

## Recommendation

- **Keep shipping Rider only.** The ReSharper `.nupkg` cannot host the generator, so its publish
  (`publishDotNet` in `build.gradle.kts`) stays deferred / unwired from `release.yml`.
- VS-user support, if ever wanted, is a **separate VSIX sub-project** implementing
  `IVsSingleFileGenerator`. It could reuse the shared, platform-agnostic, unit-tested
  `src/dotnet/.../ResXCodeGenerator.cs` (BCL-only, no Rider/ReSharper dependencies) verbatim — only
  the registration/host layer would be new. Treat as a distinct future effort, not part of this repo's
  ReSharper plugin.

## How this was checked (reproducible)

```sh
cd ~/.nuget/packages
# (1) where the custom-tool API lives:
grep -rlI --include=*.dll -a ISingleFileCustomTool .
#   -> jetbrains.rider.rdbackend.common/.../JetBrains.RdBackend.Common.dll  (Rider only)

# (2) ReSharper SDK public API surface (the .sstg is a zip of metadata text):
unzip -o jetbrains.resharper.sdk.internal/*/DotFiles/*.sstg -d /tmp/sstg
grep -aoE '[A-Za-z.]*(CustomTool|SingleFile|Generat)[A-Za-z]*' \
  /tmp/sstg/PublicApiMetadata/ArtifactValue.txt | sort -u
#   -> only Generate/Generated/AspCodeBehindGenerator; no CustomTool/SingleFile
```
