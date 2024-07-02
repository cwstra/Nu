let
  pkgs = import<nixpkgs> {};
in
  pkgs.mkShell rec {
    name = "dotnet-env";
    packages = with pkgs; [
      dotnetCorePackages.sdk_8_0_1xx
      steam-run
      jetbrains.rider
    ];
  }
