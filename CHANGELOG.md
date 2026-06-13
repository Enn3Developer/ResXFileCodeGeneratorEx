# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2026-06-13
- Fix a Rider UI freeze (~20s, sometimes killing the IDE) when renaming a resource key in a `.resx` handled by this generator; the generated `.Designer.cs` is now written through Rider's virtual file system so it no longer deadlocks the Rename Resource refactoring

## [1.0.2] - 2026-06-13
- Rename the plugin ID to `com.enn3developer.resxfilecodegeneratorex` so the plugin can be published on the JetBrains Marketplace (no behavior change)
- Add an MIT LICENSE and point the project/vendor URL at the Enn3Developer repository

## [1.0.1] - 2026-06-13
- Fix the release build by disabling the flaky `buildSearchableOptions` CI task (no plugin behavior change)

## [1.0.0] - 2026-06-13
- Initial version
