let
  pkgs = import<nixpkgs> {};
  selected-dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0_1xx;
in
  pkgs.mkShell rec {
    name = "dotnet-env";
    packages = with pkgs; [
      selected-dotnet-sdk
      steam-run
      jetbrains.rider
      mono
    ];
    shellHook = ''
      export MSBuildSDKsPath=${selected-dotnet-sdk}/share/dotnet/sdk/${selected-dotnet-sdk.version}/Sdks
    '';
  }
