
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UDPGame
{
    public class Ball : GameObject
    {
        UDPGameClient server;
        int seqNum;
        float ballMoveSpeed = 4;
        List<(int seqNum, Action action)> unProcessedInputs = new List<(int, Action)>();
        public bool Owner = false;
        //THIS IS KNOWN FROM SERVER! SERVER COULD TELL ?
        float ServerUpdateRate = 3;

        public void SetTickRate(int newTickrate)
        {
            ServerUpdateRate = newTickrate;
        }
        public Ball(string contentPath, ContentManager Content, Vector2 startPos, UDPGameClient server) : base(contentPath, Content)
        {
            this.position = startPos;
            this.server = server;
        }

        public override void Update(GameTime gameTime)
        {
            if (Owner) // acutally send messages and do movement
            {
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    if (Game1.USE_PREDICTION)
                    {
                        Move(true);
                        seqNum++;
                        unProcessedInputs.Add((seqNum, () => Move(true)));
                    }
                    server.SendDataToServer(new MovementUpdate() {Moveleft = true, SequenceNumber = seqNum });

                    Debug.WriteLine(seqNum);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    if (Game1.USE_PREDICTION)
                    {
                        Move(false);
                        seqNum++;
                        unProcessedInputs.Add((seqNum, () => Move(false)));
                    }
                    server.SendDataToServer(new MovementUpdate() { Moveleft = false, SequenceNumber = seqNum });

                }
            }
            else if(Game1.USE_INTERPOLATION)// interpolate!
            {
                //How long we are "behind" due to interpolation
                var renderTimestamp = DateTime.UtcNow - TimeSpan.FromMilliseconds(1000.0f / ServerUpdateRate);                

                // Drop older positions.
                while (positionBuffer.Count >= 2 && positionBuffer[1].timestamp <= renderTimestamp)
                {
                    positionBuffer.RemoveAt(0);
                }
                // Interpolate between the two surrounding authoritative positions.
                if (positionBuffer.Count >= 2 && positionBuffer[0].timestamp <= renderTimestamp && renderTimestamp <= positionBuffer[1].timestamp)
                {
                    double position0 = positionBuffer[0].position.X;
                    double position1 = positionBuffer[1].position.X;
                    var timestamp0 = positionBuffer[0].timestamp;
                    var timestamp1 = positionBuffer[1].timestamp;

                    position.X = (float) (position0 + (position1 - position0) * (renderTimestamp - timestamp0).TotalMilliseconds
                        / (timestamp1 - timestamp0).TotalMilliseconds);
                }

            }
            base.Update(gameTime);
        }
        void Move(bool left)
        {
            if (left)
            {
                position.X -= 1 * ballMoveSpeed;
            }
            else
            {
                position.X += 1 * ballMoveSpeed;
            }
        }
        List<(DateTime timestamp, Vector2 position)> positionBuffer = new List<(DateTime, Vector2)>();
        internal void HandleSnapShot(SnapShot snap)
        {
            if (Owner) // recon!
            {
                SetPosition(new Vector2(snap.ballPosX, snap.ballPosY));

                if (Game1.USE_RECONCILITION)
                {
                    int i = 0;
                    while (i < unProcessedInputs.Count)
                    {
                        var input = unProcessedInputs[i];
                        if (input.seqNum <= snap.SnapSeqId)
                        {
                            unProcessedInputs.Remove(input);
                        }
                        else
                        {
                            input.action.Invoke();
                            i++;
                        }
                    }

                }
            }
            else if(Game1.USE_INTERPOLATION)// interpolation
            {
                positionBuffer.Add((DateTime.UtcNow, new Vector2(snap.ballPosX, snap.ballPosY)));
               
            }
            else
            {
                SetPosition(new Vector2(snap.ballPosX, snap.ballPosY));
            }
            
        }
    }
}
