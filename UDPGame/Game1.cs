using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace UDPGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        List<GameObject> gamebjects = new List<GameObject>();
        public static bool USE_INTERPOLATION = true;
        public static bool USE_RECONCILITION = true;
        public static bool USE_PREDICTION = true;
        public static int LATENCY = 250;
        private SpriteFont font;
        Ball ball;
        UDPGameClient client;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }


        protected override void Initialize()
        {
            //force FPS, makes it easier to create functional network code, otherwise time is a factor...
            this.IsFixedTimeStep = true;//false;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d); //60);
            base.Initialize();
            client = new UDPGameClient(onDataRecieved);
            client.SendDataToServer(new JoinMessage());
            StartGame();
        }

        private void onDataRecieved(byte[] receivedData)
        {
            MessageType messageType = (MessageType)receivedData[0];
            byte[] dataToDeserialize = receivedData.Skip(1).ToArray();
            switch (messageType)
            {
                case MessageType.SnapShot:
                    SnapShot snap = MessagePackSerializer.Deserialize<SnapShot>(dataToDeserialize);
                    ball.HandleSnapShot(snap);
                    break;
                case MessageType.JoinAnswer:
                    ball.Owner = MessagePackSerializer.Deserialize<JoinAnswer>(dataToDeserialize).BallOwner;
                    break;
                    case MessageType.UpdateTickRate:
                    ball.SetTickRate(MessagePackSerializer.Deserialize<UpdateTickRate>(dataToDeserialize).TickRate);
                    break;
                default:
                    break;
            }
        }

        void StartGame()
        {
            ball = new Ball("ball", Content, new Vector2(GraphicsDevice.Viewport.Bounds.Width / 2, GraphicsDevice.Viewport.Bounds.Height / 2), client);
            gamebjects.Add(ball);
            gamebjects.ForEach(x => x.Init());
        }


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font");
            gamebjects.ForEach(x => x.LoadContent());
            // TODO: use this.Content to load your game content here
        }
       

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            GetState();
            if (HasBeenPressed(Keys.Add))
            {
                Debug.WriteLine("hello");
                LATENCY += 50;
            }
            if (HasBeenPressed(Keys.OemMinus))
            {
                LATENCY -= 50;
            }
            if (HasBeenPressed(Keys.P))
            {
                Debug.WriteLine("hello");
                USE_PREDICTION = !USE_PREDICTION;
            }
            if (HasBeenPressed(Keys.I))
            {
                USE_INTERPOLATION = !USE_INTERPOLATION;
            }
            if (HasBeenPressed(Keys.R))
            {
                USE_RECONCILITION = !USE_RECONCILITION;
            }
            var kstate = Keyboard.GetState();
           
            gamebjects.ForEach(x => x.Update(gameTime));
            base.Update(gameTime);
        }
        static KeyboardState currentKeyState;
        static KeyboardState previousKeyState;

        public  KeyboardState GetState()
        {
            previousKeyState = currentKeyState;
            currentKeyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            return currentKeyState;
        }

        public bool HasBeenPressed(Keys key)
        {
            return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();
            gamebjects.ForEach(x => x.Draw(gameTime, _spriteBatch));
            DrawStats(_spriteBatch);
            _spriteBatch.End();

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
        void DrawStats(SpriteBatch _spriteBatch)
        {
            string stringToDraw01 = ball.Owner ? "You own the ball\n controls A to move left, D to move right\n" : "you dont own the ball\n";
            string stringToDraw0 = $"latency: {LATENCY} + to add 50 - to decrease 50\n";
            string stringToDraw1 = $"prediction on: {USE_PREDICTION} p to toglle\n";
            string stringToDraw2 = $"Reconciliation on: {USE_RECONCILITION} r to toggle\n";
            string stringToDraw3 = $"Interpolation on: {USE_INTERPOLATION} i to toggle\n";

            string finalstring = stringToDraw01+ stringToDraw0 + stringToDraw1 + stringToDraw2 + stringToDraw3;
            _spriteBatch.DrawString(font, finalstring, Vector2.Zero, Color.Black);
        }
    }
}