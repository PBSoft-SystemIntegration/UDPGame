using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace UDPGame;

// Ball contains only ball-specific input. All networking techniques live in
// NetworkGameObject and can therefore be reused by another player or an NPC.
public sealed class Ball : NetworkGameObject
{
    public const string NetworkId = "ball";

    public Ball(Vector2 startPosition, UDPGameClient client, DemoSettings settings)
        : base(NetworkId, "ball", startPosition, 4f, client, settings)
    {
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
    {
        // Check ownership before reading input or constructing a network message.
        if (!IsOwnedByThisClient)
        {
            UpdateRemote();
            return;
        }

        var direction = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.A)) direction.X--;
        if (keyboard.IsKeyDown(Keys.D)) direction.X++;
        if (keyboard.IsKeyDown(Keys.W)) direction.Y--;
        if (keyboard.IsKeyDown(Keys.S)) direction.Y++;

        // Without normalization, diagonal input (for example W+D) has a length
        // of sqrt(2) and would move the ball about 41% faster.
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }

        UpdateOwned(gameTime, direction);
    }
}
