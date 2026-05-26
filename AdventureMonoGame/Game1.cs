using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace AdventureMonoGame;

/// <summary>
/// Main game class for the MonoGame/FNA client.
/// This is the core game loop that manages screens, input, and rendering.
/// </summary>
public class Game1 : Game, IDisposable
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private const int WINDOW_WIDTH = 1280;
    private const int WINDOW_HEIGHT = 720;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Set window size
        _graphics.PreferredBackBufferWidth = WINDOW_WIDTH;
        _graphics.PreferredBackBufferHeight = WINDOW_HEIGHT;
        _graphics.ApplyChanges();

        Window.Title = "Adventure Discord RPG - MonoGame Client";
    }

    protected override void Initialize()
    {
        base.Initialize();
        LogService.Info($"Game initialized - Window: {WINDOW_WIDTH}x{WINDOW_HEIGHT}");
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        LogService.Info("Content loaded");
    }

    protected override void Update(GameTime gameTime)
    {
        // Exit on Escape
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        // TODO: Draw game content here
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteBatch?.Dispose();
            _graphics?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Simple logging service for debugging.
/// </summary>
public static class LogService
{
    public static void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} - {message}");
        Console.ResetColor();
    }

    public static void Warning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} - {message}");
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
        Console.ResetColor();
    }
}
