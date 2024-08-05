namespace Caretaker
open System
open System.Numerics
open Prime
open Nu

// this is our top-level MMCC model type. It determines what state the game is in. To learn about MMCC in Nu, see -
// https://github.com/bryanedds/Nu/wiki/Model-View-Update-for-Games-via-MMCC
type Caretaker =
    { MyGameTime : int64 }
    static member initial = { MyGameTime = 0L }

// this is our top-level MMCC message type.
type MyGameMessage =
    | Tick
    interface Message

// this is our top-level MMCC command type. Commands are used instead of messages when the world is to be transformed.
type MyGameCommand =
    | Update
    | Exit
    interface Command

// this extends the Game API to expose the above MMCC model as a property.
[<AutoOpen>]
module CaretakerExtensions =
    type Game with
        member this.GetMyGame world = this.GetModelGeneric<Caretaker> world
        member this.SetMyGame value world = this.SetModelGeneric<Caretaker> value world
        member this.MyGame = this.ModelGeneric<Caretaker> ()

// this is the dispatcher that customizes the top-level behavior of our game. In here, we create screens as content and
// bind them up with events and properties.
type MyGameDispatcher () =
    inherit GameDispatcher<Caretaker, MyGameMessage, MyGameCommand> (Caretaker.initial)

    // here we define the game's properties and event handling
    override this.Definitions (_, _) =
        [Game.UpdateEvent => Update]

    // here we handle the above messages
    override this.Message (myGame, message, _, _) =
        match message with
        | Tick ->
            let myGame = { myGame with MyGameTime = inc myGame.MyGameTime }
            just myGame

    // here we handle the above commands
    override this.Command (_, command, _, world) =
        match command with
        | Update ->
            let bodyId = Simulants.Caretaker.Elmario.GetBodyId world
            let motion = 
              if World.isKeyboardKeyDown KeyboardKey.Left world then
                  Some (v3 -800.0f 0.0f 0.0f)
              elif World.isKeyboardKeyDown KeyboardKey.Right world then
                  Some (v3 800.0f 0.0f 0.0f)
              elif World.isKeyboardKeyDown KeyboardKey.Down world then
                  Some (v3 0.0f -800.0f 0.0f)
              elif World.isKeyboardKeyDown KeyboardKey.Up world then
                  Some (v3 0.0f 800.0f 0.0f)
              else None
            let world =
                Option.mapOrDefaultValue
                    (fun m -> World.applyBodyForce m v3Zero bodyId world) world motion
            just world
        | Exit ->
            if world.Unaccompanied
            then just (World.exit world)
            else just world

    // here we describe the content of the game, including a screen, a group, and a couple example entities.
    override this.Content (_, _) =
        [Content.screen Simulants.Caretaker.Screen.Name Vanilla []
            [Content.group Simulants.Caretaker.Group.Name []
                [Content.character2d Simulants.Caretaker.Elmario.Name
                    [Entity.Position == v3 0.0f 32.0f 0.0f
                     Entity.Size == v3 56.0f 56.0f 0.0f
                     Entity.GravityOverride == Some v3Zero
                     Entity.Elevation == 10.0f
                     Entity.Scale == v3Dup 0.2f]
                 Content.tileMap "TileMap"
                    [Entity.Position == v3 0.0f 0.0f 0.0f
                     Entity.TileMap := asset "Gameplay" "Overworld"
                     Entity.TileIndexOffset := 0
                     Entity.TileIndexOffsetRange := (0,0)
                     Entity.Elevation == 0.0f
                     Entity.Friction == 0.1f
                     Entity.TileLayerClearance == 10.0f]
                 Content.block2d "Ground"
                    [Entity.Position == v3 0.0f -128.0f 0.0f
                     Entity.Size == v3 384.0f 32.0f 0.0f
                     Entity.StaticImage == asset "Gameplay" "TreeTop"
                     Entity.Friction == 0.1f
                     Entity.Elevation == 1.0f]
                 Content.block2d "Rock"
                    [Entity.Position == v3 176.0f -96.0f 0.0f
                     Entity.Size == v3 32.0f 32.0f 0.0f
                     Entity.StaticImage == asset "Gameplay" "Rock"
                     Entity.Friction == 0.1f
                     Entity.Elevation == 1.0f]]]]
