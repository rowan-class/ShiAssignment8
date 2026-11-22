using Microsoft.Xna.Framework;
using System;

namespace GameBase;

public class PlayerInputComponent : Component
{
    public event Action<Vector2> MoveRequested;

    public PlayerInputComponent(GameObject owner) : base(owner)
    {
    }

    public override void Connect()
    {
        base.Connect();
        Services.Input.ActionPressed += OnActionPressed;
    }

    private void OnActionPressed(GameAction action)
    {
        if (!activeSelf)
        {
            return;
        }

        switch (action)
        {
            case GameAction.MoveUp:
                MoveRequested?.Invoke(Directions.Up);
                break;

            case GameAction.MoveDown:
                MoveRequested?.Invoke(Directions.Down);
                break;

            case GameAction.MoveLeft:
                MoveRequested?.Invoke(Directions.Left);
                break;

            case GameAction.MoveRight:
                MoveRequested?.Invoke(Directions.Right);
                break;
        }
    }

    public override void OnDestroy()
    {
        Services.Input.ActionPressed -= OnActionPressed;
        base.OnDestroy();
    }
}