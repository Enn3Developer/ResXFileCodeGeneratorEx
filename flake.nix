{
  description = "Development environment for the ResXFileCodeGeneratorEx Rider/ReSharper plugin";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    { nixpkgs, flake-utils, ... }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
        # Toolchain mirrors the GitHub Actions workflows:
        #   - .NET 8 SDK  (C# plugin sources + unit tests)
        #   - JDK 17      (Gradle / IntelliJ Platform Gradle Plugin)
        # The Gradle wrapper (./gradlew, currently 8.13) is the canonical
        # Gradle and is downloaded by the wrapper itself, so it is not pinned
        # here. The plugin's real build still fetches the Rider SDK and NuGet
        # packages from the network, so `nix build` is intentionally not
        # provided -- this flake only supplies a reproducible dev shell.
        jdk = pkgs.jdk17;
        dotnet = pkgs.dotnet-sdk_8;
      in
      {
        devShells.default = pkgs.mkShell {
          packages = [
            dotnet
            jdk
          ];

          JAVA_HOME = "${jdk}/lib/openjdk";
          DOTNET_ROOT = "${dotnet}";
          # Telemetry off by default for reproducible, offline-friendly runs.
          DOTNET_CLI_TELEMETRY_OPTOUT = "1";
          DOTNET_NOLOGO = "1";

          shellHook = ''
            echo "ResXFileCodeGeneratorEx dev shell"
            echo "  dotnet : $(dotnet --version 2>/dev/null)"
            echo "  java   : $(java -version 2>&1 | head -n1)"
            echo "  gradle : use ./gradlew (wrapper)"
          '';
        };
      }
    );
}
