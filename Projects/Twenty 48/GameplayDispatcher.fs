namespace Twenty48
open System
open System.Numerics
open Prime
open Nu

// this is our MMCC model type representing gameplay.
type GameplayMessage =
    | StartPlaying
    | FinishQuitting
    | TimeUpdate
    | TryShift of Direction
    | StartQuittingMsg
    | Nil
    interface Message
    
// this is our gameplay MMCC command type.
type GameplayCommand =
    | NilCmd
    interface Command
    
// this extends the Screen API to expose the Gameplay model as well as the Quit event.
[<AutoOpen>]
module GameplayExtensions =
    type Screen with
        member this.GetGameplay world = this.GetModelGeneric<Gameplay> world
        member this.SetGameplay value world = this.SetModelGeneric<Gameplay> value world
        member this.Gameplay = this.ModelGeneric<Gameplay> ()
        member this.QuitEvent = Events.QuitEvent --> this
        
// this is the dispatcher that defines the behavior of the screen where gameplay takes place.
type GameplayDispatcher () =
    inherit ScreenDispatcher<Gameplay> (Gameplay.empty)
    
    override this.Run (gameplay, _, world): Gameplay * World =
        
        // Core update loop
        let gameplay =
            if world.Advancing then
                [Upward; Rightward; Downward; Leftward]
                |> List.fold (fun gameplay direction ->
                    let key = 
                        match direction with
                        | Upward -> KeyboardKey.Up
                        | Rightward -> KeyboardKey.Right
                        | Downward -> KeyboardKey.Down
                        | Leftward -> KeyboardKey.Left
                    let keyIsPressed = World.isKeyboardKeyDown key world
                    let keyWasPressed = gameplay.PressedDirections.Contains direction
                    let gameplay =
                        if keyIsPressed && not(keyWasPressed)
                        then Gameplay.doShift direction gameplay
                        else gameplay
                    let pressedDirections =
                        if keyIsPressed
                        then Set.add direction gameplay.PressedDirections
                        else Set.remove direction gameplay.PressedDirections
                    { gameplay with PressedDirections = pressedDirections }
                    ) gameplay
            else gameplay
            
        // The gui group 
        let world = World.beginGroup Simulants.GameplayGui.Name [] world
        let gameplay, world =
            match World.doButton
                      Simulants.GameplayQuit.Name
                      [Entity.Position .= v3 232.0f -144.0f 0.0f
                       Entity.Elevation .= 10.0f
                       Entity.Text .= "Quit"] world with
            | true, world -> {gameplay with GameplayState = Quit }, world
            | false, world -> gameplay, world
        let world = World.endGroup world
        
        // the scene group
        let world = World.beginGroup Simulants.GameplayScene.Name [] world
        let world =
            World.doText "Score"
              [Entity.Position .= v3 232.0f 155.0f 0.0f
               Entity.Elevation .= 10.0f
               Entity.Text @= "Score: " + string gameplay.Score]
              world
        let world =
            match gameplay.GameplayState with
            | Playing true ->
                World.doText "GameOver"
                    [Entity.Position .= v3 0.0f 155.0f 0.0f
                     Entity.Elevation .= 10.0f
                     Entity.Text .= "Game Over!"
                     Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)]
                    world
            | Playing false | Quit -> world
        
        // board panel
        let world =
            World.beginPanel Simulants.GameplayBoard.Name
              [Entity.Size .= v3 148.0f 148.0f 0.0f
               Entity.Elevation .= 1.0f
               Entity.BackdropImageOpt .= Some Assets.Gameplay.BoardImage]
              world
        let gutter = v3 4.0f 4.0f 0.0f
        let tileSize = v3 32.0f 32.0f 0.0f
        let tileOffset = (gameplay.BoardSize.V3 * tileSize + gutter * (gameplay.BoardSize - v2iOne).V3) * -0.5f
        let world =
            gameplay.Tiles
            |> List.fold (fun world tile ->
                let world =
                    World.doText ("Tile+" + string tile.TileId)
                      [Entity.PositionLocal @= tile.Position.V3 * (tileSize + gutter) + tileSize * 0.5f + tileOffset
                       Entity.Size .= tileSize
                       Entity.ElevationLocal .= 1.0f
                       Entity.Text @= string tile.Value
                       Entity.Justification .= Justified (JustifyCenter, JustifyMiddle)
                       Entity.Font .= Assets.Gui.ClearSansFont
                       Entity.FontSizing @= if tile.Value < 16384 then Some 12 else Some 8
                       Entity.TextColor .= Color.GhostWhite
                       Entity.BackdropImageOpt @= Some (Assets.Gameplay.TileImage tile.Value)]
                      world
                world
                ) world
        let world = World.endPanel world
        let world = World.endGroup world
            
        gameplay, world
