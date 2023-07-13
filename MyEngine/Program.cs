using MyEngine;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using StbImageSharp;
using System.Drawing;
using System.Runtime.CompilerServices;

// https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.2%20-%20Hello%20quad/Program.cs 

internal class Program
{
    private static async Task Main(string[] args)
    {
        using var renderer = await Renderer.CreateAsync("My OpenGL App", new Vector2D<int>(800, 600));

        renderer.Run();
    }
}