namespace Caretaker

open System.IO
open Prime
open Nu

[<RequireQualifiedAccess>]
module Data =
    let Overworld =
        match Metadata.tryGetTileMapMetadata (asset "Gameplay" "EmptyField") with
        | Some tileMapMetadata -> Some tileMapMetadata.TileMap
        | None -> None