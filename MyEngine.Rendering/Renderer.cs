﻿using System.Drawing;
using System.Numerics;
using MyEngine.Assets;
using MyEngine.Core;
using MyEngine.Core.Rendering;
using MyEngine.Core.Ecs.Resources;
using MyEngine.Rendering.OpenGL;
using MyEngine.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using RendererLoadError = MyEngine.Utils.OneOf<
    MyEngine.Rendering.OpenGL.FragmentShaderCompilationFailed,
    MyEngine.Rendering.OpenGL.VertexShaderCompilationFailed,
    MyEngine.Rendering.OpenGL.ShaderProgramLinkFailed
    >;

namespace MyEngine.Rendering;

public sealed class Renderer : IDisposable, IResource
{
    private Renderer(GL openGL,
        BufferObject<float> spriteVertexBuffer,
        BufferObject<uint> spriteElementBuffer,
        BufferObject<Matrix4x4> spriteMatrixModelBuffer,
        VertexArrayObject spriteVertexArrayObject,
        ShaderProgram spriteShader,
        ShaderProgram lineShader,
        BufferObject<float> lineVertexBuffer,
        VertexArrayObject lineVertexArray,
        BufferObject<float> textVertexBuffer,
        BufferObject<uint> textElementBuffer,
        BufferObject<TextSpriteInstance> textSpriteInstanceBuffer,
        VertexArrayObject textVertexArrayObject,
        ShaderProgram textShader,
        BufferObject<float> spriteInstanceBuffer)
    {
        OpenGL = openGL;

        _spriteVertexBuffer = spriteVertexBuffer;
        _spriteElementBuffer = spriteElementBuffer;
        _spriteInstanceBuffer = spriteInstanceBuffer;
        _matrixModelBuffer = spriteMatrixModelBuffer;
        _spriteVertexArrayObject = spriteVertexArrayObject;
        _spriteShader = spriteShader;
        _lineShader = lineShader;
        _lineVertexBuffer = lineVertexBuffer;
        _lineVertexArray = lineVertexArray;
        _textVertexBuffer = textVertexBuffer;
        _textElementBuffer = textElementBuffer;
        _textVertexArrayObject = textVertexArrayObject;
        _textShader = textShader;
        _textSpriteInstanceBuffer = textSpriteInstanceBuffer;
    }

    private struct TextSpriteInstance
    {
        public Vector2 Position;
        public float Transparency;
    }

    internal GL OpenGL { get; }
    private readonly BufferObject<float> _spriteVertexBuffer;
    private readonly BufferObject<uint> _spriteElementBuffer;
    private readonly BufferObject<float> _spriteInstanceBuffer;
    private readonly BufferObject<Matrix4x4> _matrixModelBuffer;
    private readonly VertexArrayObject _spriteVertexArrayObject;
    private readonly ShaderProgram _spriteShader;
    private readonly ShaderProgram _lineShader;
    private readonly BufferObject<float> _textVertexBuffer;
    private readonly BufferObject<TextSpriteInstance> _textSpriteInstanceBuffer;
    private readonly BufferObject<uint> _textElementBuffer;
    private readonly VertexArrayObject _textVertexArrayObject;
    private readonly ShaderProgram _textShader;

    private readonly BufferObject<float> _lineVertexBuffer;
    private readonly VertexArrayObject _lineVertexArray;
    private readonly Dictionary<AssetId, TextureObject> _textures = new();

    private static readonly uint[] SpriteIndices =
    {
        0, 1, 3,
        1, 2, 3
    };

    private uint _width;
    private uint _height;

    internal static unsafe Result<Renderer, RendererLoadError> Create(GL openGL)
    {
        // todo: try catch around all opengl stuff
        openGL.ClearColor(Color.CornflowerBlue);

        var vertexBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        var elementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        elementBuffer.SetData(SpriteIndices);

        var spriteInstanceBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        var matrixModelBuffer = BufferObject<Matrix4x4>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        var vertexArrayObject = VertexArrayObject.CreateAndBind(openGL, vertexBuffer);
        vertexArrayObject.AttachBuffer(elementBuffer);

        vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 5, 0); // vertex location
        vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, false, 5, 3); // texture coordinate

        vertexArrayObject.AttachBuffer(matrixModelBuffer);

        // model matrix needs 4 attributes, because attributes can only hold 4 values each
        vertexArrayObject.VertexArrayAttribute(2, 4, VertexAttribPointerType.Float, false, 16, 0); // model matrix
        vertexArrayObject.VertexArrayAttribute(3, 4, VertexAttribPointerType.Float, false, 16, 4); // model matrix
        vertexArrayObject.VertexArrayAttribute(4, 4, VertexAttribPointerType.Float, false, 16, 8); // model matrix
        vertexArrayObject.VertexArrayAttribute(5, 4, VertexAttribPointerType.Float, false, 16, 12); // model matrix

        vertexArrayObject.AttachBuffer(spriteInstanceBuffer);
        vertexArrayObject.VertexArrayAttribute(6, 1, VertexAttribPointerType.Float, false, 1, 0); // transparency

        // only progress to the next buffer item when (1) models have been drawn rather than every vertex
        openGL.VertexAttribDivisor(2, 1);
        openGL.VertexAttribDivisor(3, 1);
        openGL.VertexAttribDivisor(4, 1);
        openGL.VertexAttribDivisor(5, 1);
        openGL.VertexAttribDivisor(6, 1);

        // todo: custom shaders
        var vertexCode = File.ReadAllText(Path.Join("Shaders", "shader.vert"));
        var lineVertexCode = File.ReadAllText(Path.Join("Shaders", "lineShader.vert"));
        var fragmentCode = File.ReadAllText(Path.Join("Shaders", "shader.frag"));
        var lineFragmentCode = File.ReadAllText(Path.Join("Shaders", "lineShader.frag"));
        var textVertexCode = File.ReadAllText(Path.Join("Shaders", "textShader.vert"));
        var textFragmentCode = File.ReadAllText(Path.Join("Shaders", "textShader.frag"));

        var spriteShaderResult = ShaderProgram.Create(openGL, vertexCode, fragmentCode)
            .MapError(err =>
            {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
            });
        if (!spriteShaderResult.TryGetValue(out var shader))
        {
            return Result.Failure<Renderer, RendererLoadError>(spriteShaderResult.UnwrapError());
        }

        openGL.Enable(EnableCap.Blend);
        openGL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var textShaderResult = ShaderProgram.Create(openGL, textVertexCode, textFragmentCode)
            .MapError(err =>
           {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
           });

        if (!textShaderResult.TryGetValue(out var textShader))
        {
            return Result.Failure<Renderer, RendererLoadError>(textShaderResult.UnwrapError());
        }

        var textVertexBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        var textElementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw);
        textElementBuffer.SetData(SpriteIndices);

        var textVertexArrayObject = VertexArrayObject.CreateAndBind(openGL, textVertexBuffer);
        textVertexArrayObject.AttachBuffer(textElementBuffer);

        textVertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, normalized: false, vertexSize: 5, offsetSize: 0); // vertex location
        textVertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, normalized: false, vertexSize: 5, offsetSize: 3); // texture coordinate

        var textSpriteInstanceBuffer = BufferObject<TextSpriteInstance>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        textVertexArrayObject.AttachBuffer(textSpriteInstanceBuffer);
        textVertexArrayObject.VertexArrayAttribute(2, 3, VertexAttribPointerType.Float, normalized: false, vertexSize: 3, offsetSize: 0); // character position
        textVertexArrayObject.VertexArrayAttribute(3, 1, VertexAttribPointerType.Float, normalized: false, vertexSize: 3, offsetSize: 2); // transparency

        // only progress to the next buffer item when (1) models have been drawn rather than every vertex
        openGL.VertexAttribDivisor(2, 1);
        openGL.VertexAttribDivisor(3, 1);

        var lineVertexBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

        var lineVertexArray = VertexArrayObject.CreateAndBind(openGL, lineVertexBuffer);
        lineVertexArray.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, false, 3, 0); // location

        var lineShaderResult = ShaderProgram.Create(openGL, lineVertexCode, lineFragmentCode)
            .MapError(err =>
            {
                return err.Match(
                    vertexShaderCompilationError => new RendererLoadError(vertexShaderCompilationError),
                    fragmentShaderCompilationError => new RendererLoadError(fragmentShaderCompilationError),
                    shaderLinkError => new RendererLoadError(shaderLinkError));
            });
        if (!lineShaderResult.TryGetValue(out var lineShader))
        {
            return Result.Failure<Renderer, RendererLoadError>(lineShaderResult.UnwrapError());
        }

        lineVertexArray.Unbind();
        lineVertexBuffer.Unbind();

        var renderer = new Renderer(openGL,
            vertexBuffer,
            elementBuffer,
            matrixModelBuffer,
            vertexArrayObject,
            shader,
            lineShader,
            lineVertexBuffer,
            lineVertexArray,
            textVertexBuffer,
            textElementBuffer,
            textSpriteInstanceBuffer,
            textVertexArrayObject,
            textShader,
            spriteInstanceBuffer);

        return Result.Success<Renderer, RendererLoadError>(renderer);
    }

    public void Resize(Vector2D<int> size)
    {
        OpenGL.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        _width = (uint)size.X;
        _height = (uint)size.Y;
    }

    public readonly record struct LineRender(Vector3 Start, Vector3 End);
    public readonly record struct SpriteRender(
        Sprite Sprite,
        float Transparency,
        Vector2 Dimensions,
        Matrix4x4 ModelMatrix
        );

    void BindOrAddAndBind(Core.Rendering.Texture texture)
    {
        if (!_textures.TryGetValue(texture.Id, out var textureObject))
        {
            textureObject = new TextureObject(OpenGL, texture.Data, (uint)texture.Dimensions.X, (uint)texture.Dimensions.Y, TextureTarget.Texture2D, TextureUnit.Texture0);
            _textures[texture.Id] = textureObject;
        }

        textureObject.Bind(TextureUnit.Texture0);
    }

    private (float LeftEdge, float RightEdge, float BottomEdge, float TopEdge) GetRectEdges(Vector2 dimensions, SpriteOrigin origin)
    {
        return origin switch
        {
            //                          LeftEdge             RightEdge          BottomEdge          TopEdge
            SpriteOrigin.BottomLeft =>  (0f,                 dimensions.X,      0f,                 dimensions.Y),
            SpriteOrigin.BottomRight => (-dimensions.X,      0f,                0f,                 dimensions.Y),
            SpriteOrigin.TopLeft =>     (0f,                 dimensions.X,      -dimensions.Y,      0f),
            SpriteOrigin.TopRight =>    (-dimensions.X,      0f,                -dimensions.Y,      0f),
            _ or SpriteOrigin.Center => (-dimensions.X / 2f, dimensions.X / 2f, -dimensions.Y / 2f, dimensions.Y / 2f),
        };
    }

    private unsafe void DrawSprites(IEnumerable<SpriteRender> sprites, Matrix4x4 viewProjection)
    {
        _spriteShader.UseProgram();
        _spriteShader.SetUniform1("uViewProjection", viewProjection);

        _spriteVertexArrayObject.Bind();
        foreach (var textureGrouping in sprites.GroupBy(x => x.Sprite.Texture))
        {
            var texture = textureGrouping.Key;
            BindOrAddAndBind(textureGrouping.Key);

            foreach (var textureCoordGrouping in textureGrouping.GroupBy(x => (x.Sprite.SpriteHash, x.Transparency)))
            {
                var first = textureCoordGrouping.First();
                var textureCoords = first.Sprite.TextureCoordinates;

                var (leftEdge, rightEdge, bottomEdge, topEdge) = GetRectEdges(first.Dimensions, first.Sprite.Origin);
                var data = new[]
                {
                    // X       Y           Z   textureCoords
                    rightEdge, topEdge,    0f, textureCoords[0].X, textureCoords[0].Y,
                    rightEdge, bottomEdge, 0f, textureCoords[1].X, textureCoords[1].Y,
                    leftEdge,  bottomEdge, 0f, textureCoords[2].X, textureCoords[2].Y,
                    leftEdge,  topEdge,    0f, textureCoords[3].X, textureCoords[3].Y
                };

                _spriteVertexBuffer.Bind();
                _spriteVertexBuffer.SetData(data);

                _spriteInstanceBuffer.Bind();
                _spriteInstanceBuffer.SetData(textureCoordGrouping.Select(x => x.Transparency).ToArray());

                var transforms = textureCoordGrouping.Select(x => x.ModelMatrix).ToArray();
                _matrixModelBuffer.Bind();
                _matrixModelBuffer.SetData(transforms);

                OpenGL.DrawElementsInstanced(PrimitiveType.Triangles, (uint)SpriteIndices.Length, DrawElementsType.UnsignedInt, null, (uint)transforms.Length);
            }

        }
    }

    private void DrawLines(IEnumerable<LineRender> lines, Matrix4x4 viewProjection)
    {
        var linePoints = lines.SelectMany(x => new[] { x.Start.X, x.Start.Y, x.Start.Z, x.End.X, x.End.Y, x.End.Z }).ToArray();
        if (linePoints.Length == 0)
        {
            return;
        }
        _lineShader.UseProgram();
        _lineShader.SetUniform1("uViewProjection", viewProjection);
        _lineVertexBuffer.Bind();
        _lineVertexBuffer.SetData(linePoints);

        _lineVertexArray.Bind();


        OpenGL.DrawArrays(GLEnum.Lines, 0, (uint)(linePoints.Length / 3));
    }

    public sealed record TextRender(Vector2 Position,
        string Text,
        float Transparency,
        Core.Rendering.Texture Texture,
        IReadOnlyDictionary<char, Sprite> CharacterSprites);

    private sealed record CharRender(Vector2[] TextureCoords, Vector2 Dimensions, List<TextSpriteInstance> SpriteInstances, SpriteOrigin Origin);

    private unsafe void DrawText(IEnumerable<TextRender> textRenders, Matrix4x4 projection)
    {
        _textShader.UseProgram();
        _textShader.SetUniform1("uProjection", projection);
        _textVertexArrayObject.Bind();
        foreach (var textureGrouping in textRenders.GroupBy(x => x.Texture))
        {
            BindOrAddAndBind(textureGrouping.Key);

            var characterPositions = new Dictionary<char, CharRender>();
            foreach (var textRender in textureGrouping)
            {
                var position = textRender.Position;
                const int charWidth = 24;
                const int charHeight = 24;
                foreach (var character in textRender.Text)
                {
                    if (character == '\r')
                    {
                        continue;
                    }
                    if (character == '\n')
                    {
                        position = new Vector2(textRender.Position.X, position.Y + charHeight);
                        continue;
                    }
                    if (character == ' ')
                    {
                        position = new Vector2(position.X + charWidth, position.Y);
                        continue;
                    }

                    // this assumes that the char sprite has the same texture atlas.
                    // Find a nicer way around that rather than blindly assuming things have been setup correctly
                    var sprite = textRender.CharacterSprites[character];

                    if (!characterPositions.TryGetValue(character, out var positions))
                    {
                        positions = new CharRender(sprite.TextureCoordinates, sprite.Dimensions, new List<TextSpriteInstance>(), sprite.Origin);
                        characterPositions[character] = positions;
                    }
                    positions.SpriteInstances.Add(new TextSpriteInstance
                    {
                        Position = position,
                        Transparency = textRender.Transparency
                    });
                    position = new Vector2(position.X + charWidth, position.Y);
                }
            }

            foreach (var (textureCoords, dimensions, SpriteInstances, origin) in characterPositions.Values)
            {
                var (leftEdge, rightEdge, bottomEdge, topEdge) = GetRectEdges(dimensions, origin);
                var vertexData = new[]
                {
                    // X       Y           Z   textureCoords
                    rightEdge, topEdge,    0f, textureCoords[0].X, textureCoords[0].Y,
                    rightEdge, bottomEdge, 0f, textureCoords[1].X, textureCoords[1].Y,
                    leftEdge,  bottomEdge, 0f, textureCoords[2].X, textureCoords[2].Y,
                    leftEdge,  topEdge,    0f, textureCoords[3].X, textureCoords[3].Y
                };

                _textVertexBuffer.Bind();
                _textVertexBuffer.SetData(vertexData);

                _textSpriteInstanceBuffer.Bind();
                _textSpriteInstanceBuffer.SetData(SpriteInstances.ToArray());

                OpenGL.DrawElementsInstanced(PrimitiveType.Triangles, (uint)SpriteIndices.Length, DrawElementsType.UnsignedInt, null, (uint)SpriteInstances.Count);
            }
        }
    }

    public unsafe void RenderOrthographic(
        Vector3 cameraPosition,
        Vector2 viewSize,
        IEnumerable<SpriteRender> sprites,
        IEnumerable<SpriteRender> screenSprites,
        IEnumerable<LineRender> lines,
        IEnumerable<TextRender> textRenders)
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit);

        var view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        var projection = Matrix4x4.CreateOrthographic(viewSize.X, viewSize.Y, 0.1f, 100f);

        var viewProjection = projection * view;

        // todo: get this from the outside world
        var screenSize = new Vector2(800, 600);
        var worldToScreen =
            Matrix4x4.CreateTranslation(-screenSize.X / 2f, -screenSize.Y / 2f, 0f)
            * Matrix4x4.CreateScale(viewSize.X / screenSize.X, viewSize.Y / screenSize.Y, 1f);

        // world space
        DrawSprites(sprites, viewProjection);
        DrawLines(lines, viewProjection);

        // screen space
        DrawSprites(screenSprites, worldToScreen * projection);
        DrawText(textRenders, worldToScreen * projection);
    }

    public readonly record struct RenderError(GlobalTransform.GetPositionRotationScaleError Error);

    public unsafe Result<Unit, RenderError> Render(GlobalTransform cameraTransform, IEnumerable<GlobalTransform> transforms)
    {
        OpenGL.Clear(ClearBufferMask.ColorBufferBit);

        _spriteVertexArrayObject.Bind();

        _spriteShader.UseProgram();

        var positionRotationScaleResult = cameraTransform.GetPositionRotationScale()
            .MapError(err => new RenderError(err));

        if (!positionRotationScaleResult.TryGetValue(out var positionRotationScale))
        {
            return Result.Failure<Unit, RenderError>(positionRotationScaleResult.UnwrapError());
        }

        var (cameraPosition, cameraRotation, _) = positionRotationScale;

        var cameraDirection = cameraRotation.ToEulerAngles();

        var cameraFront = Vector3.Normalize(cameraDirection);

        var view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition + cameraFront, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45.0f), _width / _height, 0.1f, 100.0f);

        _spriteShader.SetUniform1("uView", view);
        _spriteShader.SetUniform1("uProjection", projection);

        foreach (var transform in transforms)
        {
            var model = transform.ModelMatrix;

            _spriteShader.SetUniform1("uModel", model);

            OpenGL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        return Result.Success<Unit, RenderError>(Unit.Value);
    }

    public void Dispose()
    {
        _spriteVertexBuffer?.Dispose();
        _spriteElementBuffer?.Dispose();
        _spriteVertexArrayObject?.Dispose();
        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }
    }
}
