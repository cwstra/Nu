﻿namespace BlazeVector
open System
open System.Numerics
open Prime
open Nu
open BlazeVector

type GameplayState =
    | Playing
    | Quit

// this extends the Screen API to expose the Gameplay model as well as the Quit event.
[<AutoOpen>]
module GameplayExtensions =
    type Screen with
        member this.GetGameplayState world : GameplayState = this.Get (nameof Screen.GameplayState) world
        member this.SetGameplayState (value : GameplayState) world = this.Set (nameof Screen.GameplayState) value world
        member this.GameplayState = lens (nameof Screen.GameplayState) this this.GetGameplayState this.SetGameplayState
        member this.GetScore world : int = this.Get (nameof Screen.Score) world
        member this.SetScore (value : int) world = this.Set (nameof Screen.Score) value world
        member this.Score = lens (nameof Screen.Score) this this.GetScore this.SetScore

// this is the dispatcher that defines the behavior of the screen where gameplay takes place.
type GameplayDispatcher () =
    inherit ScreenDispatcherImNui ()

    // here we define default property values
    static member Properties =
        [define Screen.GameplayState Quit
         define Screen.Score 0]

    // here we define the behavior of our gameplay
    override this.Process (gameplay, world) =

        // declare scene group
        let world = World.beginGroup Simulants.Gameplay.Name [] world

        // declare player
        let world = World.doEntity<PlayerDispatcher> "Player" [Entity.Position .= v3 -390.0f -50.0f 0.0f; Entity.Elevation .= 1.0f] world
        let player = World.getRecentEntity world

        // process scoring
        let world =
            Seq.fold (fun world section ->
                let (deaths, world) = World.doSubscription ("Die" + string section) (Events.DieEvent --> Simulants.GameplaySection section --> Address.Wildcard) world
                gameplay.Score.Map (fun score -> score + deaths.Length * 100) world)
                world [0 .. dec Constants.Gameplay.SectionCount]

        // process player death
        let world =
            let (deaths, world) = World.doSubscription "Die" player.DieEvent world
            if gameplay.GetGameplayState world = Playing && FQueue.notEmpty deaths then
                let world = gameplay.SetGameplayState Quit world
                World.playSound Constants.Audio.SoundVolumeDefault Assets.Gameplay.DeathSound world
                world
            else world
        
        // process eye look
        let world =
            if world.Advancing then
                let playerPosition = player.GetPosition world
                let playerSize = player.GetSize world
                let eyeCenter = World.getEye2dCenter world
                let eyeSize = World.getEye2dSize world
                let eyeCenter = v2 (playerPosition.X + playerSize.X * 0.5f + eyeSize.X * 0.33f) eyeCenter.Y
                World.setEye2dCenter eyeCenter world
            else world

        // finish declaring scene group
        let world = World.endGroup world

        // declare gui group
        let world = World.beginGroup "Gui" [] world
        let world = World.doText "Score" [Entity.Position .= v3 260.0f 155.0f 0.0f; Entity.Elevation .= 10.0f; Entity.Text @= "Score: " + string (gameplay.GetScore world)] world
        let (clicked, world) = World.doButton "Quit" [Entity.Position .= v3 232.0f -144.0f 0.0f; Entity.Elevation .= 10.0f; Entity.Text .= "Quit"] world
        let world = if clicked then gameplay.SetGameplayState Quit world else world
        let world = World.endGroup world
        world

    // this is a semantic fix-up that allows the editor to avoid creating an unused group. This is specific to the
    // ImNui API that is needed to patch a little semantic hole inherent in the immediate-mode programming idiom.
    override this.CreateDefaultGroup (screen, world) = World.createGroup (Some "Gui") screen world