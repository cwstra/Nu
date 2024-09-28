namespace Twenty48
open System
open System.Numerics
open Prime
open Nu

// this is our top-level MMCC model type. It determines what state the game is in. To learn about MMCC in Nu, see -
// https://github.com/bryanedds/Nu/wiki/Model-View-Update-for-Games-via-MMCC
type Twenty48 =
    | Splash
    | Title
    | Credits
    | Gameplay

// this extends the Game API to expose the above MMCC model as a property.
[<AutoOpen>]
module Twenty48Extensions =
    type Game with
        member this.GetTwenty48 world = this.GetModelGeneric<Twenty48> world
        member this.SetTwenty48 value world = this.SetModelGeneric<Twenty48> value world
        member this.Twenty48 = this.ModelGeneric<Twenty48> ()

// this is the dispatcher that customizes the top-level behavior of our game. In here, we create screens as content and
// bind them up with events and properties.
type Twenty48Dispatcher () =
    inherit GameDispatcher<Twenty48> (Splash)

    override this.Run (twenty48, _, world): Twenty48 * World =
        // declare splash screen
        let result, world =
            World.beginScreen
                Simulants.Splash.Name
                (twenty48 = Splash)
                (Slide (Constants.Dissolve.Default, Constants.Slide.Default, None, Simulants.Title))
                []
                world
        let twenty48 =
           match result |> Seq.filter (function Deselecting -> true | _ -> false) |> Seq.tryHead with
           | Some _ -> Title
           | None -> twenty48
        let world = World.endScreen world
           
        // declare title screen
        let _, world =
            World.beginScreenWithGroupFromFile
              Simulants.Title.Name
              (twenty48 = Title)
              (Dissolve (Constants.Dissolve.Default, None))
              "Assets/Gui/Title.nugroup"
              []
              world
        let world = World.beginGroup "Gui" [] world
        let twenty48, world =
            match World.doButton "Play" [] world with
            | true, world -> (Gameplay, world)
            | false, world -> (twenty48, world)
        let twenty48, world =
            match World.doButton "Credits" [] world with
            | true, world -> (Credits, world)
            | false, world -> (twenty48, world)
        let world =
            match World.doButton "Exit" [] world with
            | true, world -> World.exit world
            | false, world -> world
        let world = World.endGroup world
        let world = World.endScreen world
        
        // Declare gameplay screen
        let result, world =
            World.beginScreen<GameplayDispatcher>
                Simulants.Gameplay.Name
                (twenty48 = Gameplay)
                (Dissolve (Constants.Dissolve.Default, None))
                []
                world
        let gameplayScreen = world.ContextScreen
        let world =
            if FQueue.contains Select result
            then gameplayScreen.SetGameplay Gameplay.initial world
            else world
        let twenty48 =
            if gameplayScreen.GetSelected world && (gameplayScreen.GetGameplay world).GameplayState = Quit
            then Title
            else twenty48
        let world = World.endScreen world
            
        // handle Alt+F4
        let world =
            if World.isKeyboardAltDown world && World.isKeyboardKeyDown KeyboardKey.F4 world
            then World.exit world
            else world
        
        twenty48, world