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

      #packages.x86_64-linux.hello = nixpkgs.legacyPackages.x86_64-linux.hello;
      #packages.x86_64-linux.default = self.packages.x86_64-linux.hello;
      devShells = forAllSystems ({pkgs}: {
        default = pkgs.mkShell {
          packages = with pkgs; [
            dotnetCorePackages.sdk_8_0_1xx
            steam-run
            jetbrains.rider
          ];
        };
      });
    };
}
