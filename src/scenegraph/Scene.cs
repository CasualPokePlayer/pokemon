using System;
using System.Numerics;
using System.Collections.Generic;

public class Scene : IDisposable {

    public GameBoy Gb;
    public List<Component> Components;
    public Window Window;
    public Matrix4x4 Projection;

    public Scene(GameBoy gb, int width, int height) {
        Gb = gb;
        Components = new List<Component>();
        Window = new Window(width, height, "");
        Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

        gb.Scene = this;
    }

    public void Dispose() {
        foreach(Component c in Components) {
            c.Dispose(Gb);
        }
        Window.Dispose();
    }

    public void Begin() {
        Renderer.ClearScreen();
        Renderer.BeginScene(Projection);
        foreach(Component c in Components) {
            c.BeginScene(Gb);
        }
    }

    public void Render() {
        foreach(Component c in Components) {
            c.Render(Gb);
        }
    }

    public void End() {
        Renderer.EndScene();
        Window.Present();
        foreach(Component c in Components) {
            c.EndScene(Gb);
        }
    }
}

public abstract class Component {

    public float X;
    public float Y;
    public float Width;
    public float Height;
    public float RenderLayer;

    public virtual void Dispose(GameBoy gb) { }
    public virtual void BeginScene(GameBoy gb) { }
    public virtual void Render(GameBoy gb) { }
    public virtual void EndScene(GameBoy gb) { }
}