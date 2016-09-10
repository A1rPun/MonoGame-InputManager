﻿/*
 *** TODO ***
 * GamePadCapabilities capabilities = GamePad.GetCapabilities(PlayerIndex.One);
 * // If there a controller attached, handle it
 * if (capabilities.IsConnected) {
 *     // Get the current state of Controller1
 *     GamePadState state = GamePad.GetState(PlayerIndex.One);
 *     // You can check explicitly if a gamepad has support for a certain feature
 *     if (capabilities.HasLeftXThumbStick) {
 *         // Check teh direction in X axis of left analog stick
 *         if (state.ThumbSticks.Left.X < -0.5f) 
 * 
 * 1) keyboard input als vector altijd normalizen want een vector van (1,1) of (1,-1) etc. is impossible op gamepad
 * 2) gamepad input als vector alleen normalizen als je de "unfair" pressure advantage weg wil hebben
 *
 * een 2e enum, enum AnalogAction { LeftTrigger, RightTrigger, LeftStickX, LeftStickY, RightStickX, RightStickY }
 * en dan 2 implementaties van IsDown maken, die 2e accepteert dan zo'n AnalogAction en die gebruikt dan thresholds óf returnt een float ipv bool
 *
 * Bind an action to multiple buttons
 * Key ghost warnings
 * Keys configurable
 * MousePlayer ?? mouse support
 * thumbstick configurable, 360 degrees or 8 directions
 * More deadzone options http://www.third-helix.com/2013/04/12/doing-thumbstick-dead-zones-right.html
 * XINPUT VS DIRECTINPUT ??
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace A1r.Input
{
    // Input enum used to identify which action has been pressed
    enum Input
    {
        // Center
        Home,
        Start,
        Back,
        // D-Pad
        Up,
        Left,
        Down,
        Right,
        // Face buttons
        FaceButtonUp,
        FaceButtonLeft,
        FaceButtonDown,
        FaceButtonRight,
        // Shoulder buttons
        LeftShoulder,
        RightShoulder,
        // Triggers
        LeftTrigger,
        RightTrigger,
        // Left Stick
        LeftStick,
        LeftStickUp,
        LeftStickLeft,
        LeftStickDown,
        LeftStickRight,
        // Right Stick
        RightStick,
        RightStickUp,
        RightStickLeft,
        RightStickDown,
        RightStickRight
    }

    struct InputToKey
    {
        public Input Input;
        public Keys Key;
        public InputToKey(Input input, Keys key)
        {
            this.Input = input;
            this.Key = key;
        }
    }

    class InputManager : GameComponent
    {
        // Types of player input
        private class Player { }
        private class KeyboardPlayer : Player
        {
            public List<InputToKey> Map { get; set; }
            public KeyboardState CurrentState { get; set; }
            public KeyboardState PreviousState { get; set; }
        }
        private class GamePadPlayer : Player
        {
            public int index { get; set; }
            public GamePadState CurrentState { get; set; }
            public GamePadState PreviousState { get; set; }
        }
        private List<Player> players;
        private KeyboardPlayer keyboardPlayer;
        // Default - Return normalized float for isDown functions
        // TODO: Implement `private bool KeyboardDisadvantage = true;`
        // Deadzones
        private GamePadDeadZone gamePadDeadZone = GamePadDeadZone.IndependentAxes;
        private Array inputValues;
        public float DeadzoneSticks = 0.25f;
        public float DeadzoneTriggers = 0.25f;
        // How many states need to be checked?
        public int MaxGamePads = 2;
        public int AmountOfPlayers = 1;

        public InputManager(Game game, List<InputToKey> map = null)
            : base(game)
        {
            inputValues = Enum.GetValues(typeof(Input));
            players = new List<Player>();
            keyboardPlayer = new KeyboardPlayer();
            if (map != null)
            {
                keyboardPlayer.Map = map;
                players.Add(keyboardPlayer);
            }
        }

        public override void Update(GameTime gameTime)
        {
            var indices = new List<int>();
            keyboardPlayer.PreviousState = keyboardPlayer.CurrentState;
            keyboardPlayer.CurrentState = Keyboard.GetState();

            for (int i = players.Count; --i == 0; )
            {
                var player = players[i];
                if (player is GamePadPlayer)
                {
                    var gpp = (GamePadPlayer)player;
                    gpp.PreviousState = gpp.CurrentState;
                    gpp.CurrentState = GamePad.GetState(gpp.index, gamePadDeadZone);

                    if (gpp.CurrentState.IsConnected)
                    {
                        indices.Add(gpp.index);
                        players[i] = gpp;
                    }
                    else
                    {
                        //Remove disconnected players
                        players.RemoveAt(i);
                    }
                }
            }
            //Checking for new gamepads
            for (int j = 0; j < MaxGamePads; j++)
            {
                if (indices.Contains(j)) continue;
                var state = GamePad.GetState(j, gamePadDeadZone);
                if (state.IsConnected)
                {
                    players.Add(new GamePadPlayer()
                    {
                        index = j,
                        CurrentState = state
                    });
                }
            }
            base.Update(gameTime);
        }

        private Keys getKey(List<InputToKey> map, Input input)
        {
            return map.Find(m => m.Input == input).Key;
        }

        public bool IsDown(Keys key)
        {
            return keyboardPlayer.CurrentState.IsKeyDown(key);
        }

        public bool IsDown(Input input, PlayerIndex index = 0)
        {
            var p = players[(int)index];//out of bounds?
            if (p is KeyboardPlayer)
            {
                var player = (KeyboardPlayer)p;
                var key = getKey(player.Map, input);
                return player.CurrentState.IsKeyDown(key);
            }
            else
            {
                return IsDown(((GamePadPlayer)p).CurrentState, input);
            }
        }

        private bool IsDown(GamePadState state, Input input)
        {
            switch (input)
            {
                case Input.Home:
                    return state.Buttons.BigButton == ButtonState.Pressed;
                case Input.Start:
                    return state.Buttons.Start == ButtonState.Pressed;
                case Input.Back:
                    return state.Buttons.Back == ButtonState.Pressed;
                case Input.Up:
                    return state.DPad.Up == ButtonState.Pressed;
                case Input.Left:
                    return state.DPad.Left == ButtonState.Pressed;
                case Input.Down:
                    return state.DPad.Down == ButtonState.Pressed;
                case Input.Right:
                    return state.DPad.Right == ButtonState.Pressed;
                case Input.FaceButtonUp:
                    return state.Buttons.Y == ButtonState.Pressed;
                case Input.FaceButtonLeft:
                    return state.Buttons.X == ButtonState.Pressed;
                case Input.FaceButtonDown:
                    return state.Buttons.A == ButtonState.Pressed;
                case Input.FaceButtonRight:
                    return state.Buttons.B == ButtonState.Pressed;
                case Input.LeftShoulder:
                    return state.Buttons.LeftShoulder == ButtonState.Pressed;
                case Input.RightShoulder:
                    return state.Buttons.RightShoulder == ButtonState.Pressed;
                case Input.LeftTrigger:
                    return state.Triggers.Left > DeadzoneTriggers;
                case Input.RightTrigger:
                    return state.Triggers.Right > DeadzoneTriggers;
                case Input.LeftStick:
                    return state.Buttons.LeftStick == ButtonState.Pressed;
                case Input.LeftStickUp:
                    return state.ThumbSticks.Left.Y > DeadzoneSticks;
                case Input.LeftStickLeft:
                    return state.ThumbSticks.Left.X < DeadzoneSticks;
                case Input.LeftStickDown:
                    return state.ThumbSticks.Left.Y < DeadzoneSticks;
                case Input.LeftStickRight:
                    return state.ThumbSticks.Left.X > DeadzoneSticks;
                case Input.RightStick:
                    return state.Buttons.RightStick == ButtonState.Pressed;
                case Input.RightStickUp:
                    return state.ThumbSticks.Right.Y > DeadzoneSticks;
                case Input.RightStickLeft:
                    return state.ThumbSticks.Right.X < DeadzoneSticks;
                case Input.RightStickDown:
                    return state.ThumbSticks.Right.Y < DeadzoneSticks;
                case Input.RightStickRight:
                    return state.ThumbSticks.Right.X > DeadzoneSticks;
            }
            return false;
        }

        public bool IsHeld(Keys key)
        {
            return keyboardPlayer.CurrentState.IsKeyDown(key) && keyboardPlayer.PreviousState.IsKeyDown(key);
        }

        public bool IsHeld(Input input, PlayerIndex index = 0)
        {
            var p = players[(int)index];//out of bounds?
            if (p is KeyboardPlayer)
            {
                var player = (KeyboardPlayer)p;
                var key = getKey(player.Map, input);
                return player.CurrentState.IsKeyDown(key) && player.PreviousState.IsKeyDown(key);
            }
            else
            {
                var player = (GamePadPlayer)p;
                return IsDown(player.CurrentState, input) && IsDown(player.PreviousState, input);
            }
        }

        public bool JustPressed(Keys key)
        {
            return keyboardPlayer.CurrentState.IsKeyDown(key) && !keyboardPlayer.PreviousState.IsKeyDown(key);
        }

        public bool JustPressed(Input input, PlayerIndex index = 0)
        {
            var p = players[(int)index];//out of bounds?
            if (p is KeyboardPlayer)
            {
                var player = (KeyboardPlayer)p;
                var key = getKey(player.Map, input);
                return player.CurrentState.IsKeyDown(key) && !player.PreviousState.IsKeyDown(key);
            }
            else
            {
                var player = (GamePadPlayer)p;
                return IsDown(player.CurrentState, input) && !IsDown(player.PreviousState, input);
            }
        }

        public bool JustReleased(Keys key)
        {
            return !keyboardPlayer.CurrentState.IsKeyDown(key) && keyboardPlayer.PreviousState.IsKeyDown(key);
        }

        public bool JustReleased(Input input, PlayerIndex index = 0)
        {
            var p = players[(int)index];//out of bounds?
            if (p is KeyboardPlayer)
            {
                var player = (KeyboardPlayer)p;
                var key = getKey(player.Map, input);
                return !player.CurrentState.IsKeyDown(key) && player.PreviousState.IsKeyDown(key);
            }
            else
            {
                var player = (GamePadPlayer)p;
                return !IsDown(player.CurrentState, input) && IsDown(player.PreviousState, input);
            }
        }

        public bool SomethingDown(PlayerIndex index = 0)
        {
            var p = players[(int)index];//out of bounds?
            if (p is KeyboardPlayer)
            {
                return keyboardPlayer.CurrentState.GetPressedKeys().Length > 0;
            }
            else
            {
                var player = (GamePadPlayer)p;
                foreach (Input key in inputValues)
                {
                    if (IsDown(player.CurrentState, key))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int GetPlayerCount()
        {
            return players.Count;
        }
    }
}
