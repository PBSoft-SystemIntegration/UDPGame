using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPGame
{
    internal class Button : GameObject
    {
        private Rectangle bounds;
        private Action onButtonClicked;

        public Button(string contentPath, ContentManager Content, Vector2 startPos, Action onButtonClicked) : base(contentPath, Content)
        {
            this.position = startPos;
            this.onButtonClicked = onButtonClicked;
            this.texture = texture;
            bounds = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);

        }
        MouseState previousMouseState;
        public override void Update(GameTime gameTime)
        {
            previousMouseState = Mouse.GetState();
            if (IsClicked(previousMouseState))
            {
                onButtonClicked.Invoke();
            }
        }

        public bool IsClicked(MouseState previousMouseState)
        {
            var mouseState = Mouse.GetState();
            var mousePosition = new Point(mouseState.X, mouseState.Y);

            bool isMouseClick = mouseState.LeftButton == ButtonState.Released &&
                                previousMouseState.LeftButton == ButtonState.Pressed &&
                                bounds.Contains(mousePosition);

            return isMouseClick;
        }

        public override void Draw(GameTime gameTime, SpriteBatch sb)
        {
            sb.Draw(texture, bounds, Color.White);

        }
    }
}
