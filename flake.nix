{
  description = "nu dev environment";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
  };

  outputs = { self, nixpkgs }: 
    let 
      allSystems = [
        "x86_64-linux"
      ];

      forAllSystems = f: nixpkgs.lib.genAttrs allSystems (system: f {
        pkgs = import nixpkgs {inherit system; config.allowUnfree = true;};
      });
    in {

      devShells = forAllSystems ({pkgs}: 
      let
        selected-dotnet-sdk = pkgs.dotnet-sdk_9;
        ld_pkgs = with pkgs; [
          SDL2
          SDL2_ttf
          SDL2_mixer
          SDL2_image
        ];
        ld_path = nixpkgs.lib.strings.concatStringsSep ":" (map (pkg: pkg + "/lib") ld_pkgs);
      in 
      {
        default = pkgs.mkShell {
          packages = with pkgs; [
            selected-dotnet-sdk
            steam-run
            jetbrains.rider
          ] ++ ld_pkgs;
          shellHook = 
            if selected-dotnet-sdk.version < "9.0.0" 
            then
              ''
                export MSBuildSDKsPath=${selected-dotnet-sdk}/sdk/${selected-dotnet-sdk.version}/Sdks
              ''
            else 
              ''
                export MSBuildSDKsPath=${selected-dotnet-sdk}/share/dotnet/sdk/${selected-dotnet-sdk.version}/Sdks
                export LD_LIBRARY_PATH=${ld_path}:/run/opengl-driver/lib:/run/opengl-driver-32/lib:$LD_LIBRARY_PATH
              '';
        };
      });
    };
}
