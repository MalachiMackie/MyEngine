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
using System.Runtime.InteropServices;

namespace MyEngine.Rendering;

public sealed class Renderer : IDisposable, IResource
{
    private Renderer(GL openGL,
        BufferObject<Vector3> spriteVertexBuffer,
        BufferObject<uint> spriteElementBuffer,
        VertexArrayObject spriteVertexArrayObject,
        ShaderProgram spriteShader,
        ShaderProgram lineShader,
        BufferObject<float> lineVertexBuffer,
        VertexArrayObject lineVertexArray,
        BufferObject<uint> textElementBuffer,
        BufferObject<Vector3> textSpriteVertexBuffer,
        BufferObject<TextInstanceData> textSpriteInstanceBuffer,
        VertexArrayObject textVertexArrayObject,
        ShaderProgram textShader,
        BufferObject<SpriteInstanceData> spriteInstanceBuffer)
    {
        OpenGL = openGL;

        _spriteVertexBuffer = spriteVertexBuffer;
        _spriteElementBuffer = spriteElementBuffer;
        _spriteInstanceBuffer = spriteInstanceBuffer;
        _spriteVertexArrayObject = spriteVertexArrayObject;
        _spriteShader = spriteShader;
        _lineShader = lineShader;
        _lineVertexBuffer = lineVertexBuffer;
        _lineVertexArray = lineVertexArray;
        _textElementBuffer = textElementBuffer;
        _textVertexArrayObject = textVertexArrayObject;
        _textShader = textShader;
        _textSpriteVertexBuffer = textSpriteVertexBuffer;
        _textSpriteInstanceBuffer = textSpriteInstanceBuffer;
        _textureArray = new TextureArray(OpenGL, 2048, 2048, TextureTarget.Texture2DArray);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextInstanceData
    {
        public float Transparency;
        public float TextureSlot;
        public Vector2 Position;
        public Vector2 Scale;
        public Vector2 TextureCoordinate1;
        public Vector2 TextureCoordinate2;
        public Vector2 TextureCoordinate3;
        public Vector2 TextureCoordinate4;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpriteInstanceData
    {
        public Vector2 TextureCoordinateA;
        public Vector2 TextureCoordinateB;
        public Vector2 TextureCoordinateC;
        public Vector2 TextureCoordinateD;
        public Matrix4x4 ModelMatrix;
        public float Transparency;
        public float TextureSlot;
    }

    internal GL OpenGL { get; }
    private readonly BufferObject<Vector3> _spriteVertexBuffer;
    private readonly BufferObject<uint> _spriteElementBuffer;
    private readonly BufferObject<SpriteInstanceData> _spriteInstanceBuffer;
    private readonly VertexArrayObject _spriteVertexArrayObject;
    private readonly ShaderProgram _spriteShader;
    private readonly ShaderProgram _lineShader;
    private readonly BufferObject<Vector3> _textSpriteVertexBuffer;
    private readonly BufferObject<TextInstanceData> _textSpriteInstanceBuffer;
    private readonly BufferObject<uint> _textElementBuffer;
    private readonly VertexArrayObject _textVertexArrayObject;
    private readonly ShaderProgram _textShader;

    private readonly BufferObject<float> _lineVertexBuffer;
    private readonly VertexArrayObject _lineVertexArray;
    private readonly TextureArray _textureArray;
    private readonly Dictionary<AssetId, uint> _textureArrayTextures = new();

    private uint GetTextureSlot(Core.Rendering.Texture texture)
    {
        if (_textureArrayTextures.TryGetValue(texture.Id, out var slot))
        {
            return slot;
        }

        // todo: pack multiple textures into single texture
        slot = _textureArray.AddTexture(texture.Data.AsSpan(), xOffset: 0, yOffset: 0, (uint)texture.Dimensions.X, (uint)texture.Dimensions.Y, newTexture: true);
        _textureArrayTextures[texture.Id] = slot;

        return slot;
    }

    private uint _width;
    private uint _height;

    internal static Result<Renderer, RendererLoadError> Create(GL openGL)
    {
        Span<uint> spriteIndices = stackalloc uint[]
        {
            0, 1, 3,
            1, 2, 3
        };

        var (leftEdge, rightEdge, bottomEdge, topEdge) = GetRectEdges(Vector2.One, SpriteOrigin.Center);
        Span<Vector3> spriteVertices = stackalloc[]
        {
            new Vector3(rightEdge, topEdge, 0),
            new Vector3(rightEdge, bottomEdge, 0),
            new Vector3(leftEdge, bottomEdge, 0),
            new Vector3(leftEdge, topEdge, 0),
        };

        // todo: try catch around all opengl stuff
        openGL.ClearColor(Color.CornflowerBlue);

        var vertexBuffer = BufferObject<Vector3>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        vertexBuffer.SetData(spriteVertices);

        var elementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw);
        elementBuffer.SetData(spriteIndices);

        var spriteInstanceBuffer = BufferObject<SpriteInstanceData>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        var vertexArrayObject = VertexArrayObject.CreateAndBind(openGL);
        vertexArrayObject.AttachBuffer(elementBuffer);
        vertexArrayObject.AttachBuffer(vertexBuffer);

        vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, sizeof(float), false, 3, 0); // vertex location

        vertexArrayObject.AttachBuffer(spriteInstanceBuffer);

        vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, sizeof(float), false, 26, 0); // texture coordinate 0
        vertexArrayObject.VertexArrayAttribute(2, 2, VertexAttribPointerType.Float, sizeof(float), false, 26, 2); // texture coordinate 1
        vertexArrayObject.VertexArrayAttribute(3, 2, VertexAttribPointerType.Float, sizeof(float), false, 26, 4); // texture coordinate 2
        vertexArrayObject.VertexArrayAttribute(4, 2, VertexAttribPointerType.Float, sizeof(float), false, 26, 6); // texture coordinate 3

        vertexArrayObject.VertexArrayAttribute(5, 4, VertexAttribPointerType.Float, sizeof(float), false, 26, 8); // model matrix row 1
        vertexArrayObject.VertexArrayAttribute(6, 4, VertexAttribPointerType.Float, sizeof(float), false, 26, 12); // model matrix row 2
        vertexArrayObject.VertexArrayAttribute(7, 4, VertexAttribPointerType.Float, sizeof(float), false, 26, 16); // model matrix row 3
        vertexArrayObject.VertexArrayAttribute(8, 4, VertexAttribPointerType.Float, sizeof(float), false, 26, 20); // model matrix row 4

        vertexArrayObject.VertexArrayAttribute(9, 1, VertexAttribPointerType.Float, sizeof(float), false, 26, 24); // transparency
        vertexArrayObject.VertexArrayAttribute(10, 1, VertexAttribPointerType.Float, sizeof(float), false, 26, 25); // texture slot

        // only progress to the next buffer item when (1) models have been drawn rather than every vertex
        openGL.VertexAttribDivisor(1, 1);
        openGL.VertexAttribDivisor(2, 1);
        openGL.VertexAttribDivisor(3, 1);
        openGL.VertexAttribDivisor(4, 1);
        openGL.VertexAttribDivisor(5, 1);
        openGL.VertexAttribDivisor(6, 1);
        openGL.VertexAttribDivisor(7, 1);
        openGL.VertexAttribDivisor(8, 1);
        openGL.VertexAttribDivisor(9, 1);
        openGL.VertexAttribDivisor(10, 1);

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

        var textSpriteInstanceBuffer = BufferObject<TextInstanceData>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        var textSpriteVertexBuffer = BufferObject<Vector3>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        textSpriteVertexBuffer.SetData(spriteVertices);
        var textElementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw);
        textElementBuffer.SetData(spriteIndices);

        var textVertexArrayObject = VertexArrayObject.CreateAndBind(openGL);
        textVertexArrayObject.AttachBuffer(textElementBuffer);
        textVertexArrayObject.AttachBuffer(textSpriteVertexBuffer);

        textVertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 3, offsetSize: 0); // vertex location

        textVertexArrayObject.AttachBuffer(textSpriteInstanceBuffer);

        textVertexArrayObject.VertexArrayAttribute(1, 1, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 0); // transparency
        textVertexArrayObject.VertexArrayAttribute(2, 1, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 1); // textureSlot
        textVertexArrayObject.VertexArrayAttribute(3, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 2); // position
        textVertexArrayObject.VertexArrayAttribute(4, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 4); // scale
        textVertexArrayObject.VertexArrayAttribute(5, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 6); // texture coordinate 1
        textVertexArrayObject.VertexArrayAttribute(6, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 8); // texture coordinate 2
        textVertexArrayObject.VertexArrayAttribute(7, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 10); // texture coordinate 3
        textVertexArrayObject.VertexArrayAttribute(8, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 14, offsetSize: 12); // texture coordinate 4

        openGL.VertexAttribDivisor(1, 1);
        openGL.VertexAttribDivisor(2, 1);
        openGL.VertexAttribDivisor(3, 1);
        openGL.VertexAttribDivisor(4, 1);
        openGL.VertexAttribDivisor(5, 1);
        openGL.VertexAttribDivisor(6, 1);
        openGL.VertexAttribDivisor(7, 1);
        openGL.VertexAttribDivisor(8, 1);

        var lineVertexBuffer = BufferObject<float>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

        var lineVertexArray = VertexArrayObject.CreateAndBind(openGL);
        lineVertexArray.AttachBuffer(lineVertexBuffer);
        lineVertexArray.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, sizeof(float), false, 3, 0); // location

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
            vertexArrayObject,
            shader,
            lineShader,
            lineVertexBuffer,
            lineVertexArray,
            textElementBuffer,
            textSpriteVertexBuffer,
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

    private static (float LeftEdge, float RightEdge, float BottomEdge, float TopEdge) GetRectEdges(Vector2 dimensions, SpriteOrigin origin)
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

    private void DrawSprites(IEnumerable<SpriteRender> sprites, Render2DBatch renderBatch)
    {
        foreach (var sprite in sprites)
        {
            var textureSlot = GetTextureSlot(sprite.Sprite.Texture);
            var textureCoordinates = new Vector2[4];

            for (var i = 0; i < 4; i++)
            {
                var og = sprite.Sprite.TextureCoordinates[i];

                textureCoordinates[i] = new Vector2(
                    og.X * sprite.Sprite.Dimensions.X / _textureArray.Width,
                    og.Y * sprite.Sprite.Dimensions.Y / _textureArray.Height);
            }

            renderBatch.RenderSprite(
                sprite.ModelMatrix,
                sprite.Sprite.WorldDimensions,
                sprite.Sprite.Origin,
                textureCoordinates,
                textureSlot,
                sprite.Transparency);
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
        float LineHeight,
        float SpaceWidth,
        IReadOnlyDictionary<char, Sprite> CharacterSprites);

    private void DrawText(IEnumerable<TextRender> textRenders, Render2DBatch renderBatch)
    {
        foreach (var textRender in textRenders)
        {
            var position = textRender.Position;
            foreach (var character in textRender.Text)
            {
                if (character == '\r')
                {
                    continue;
                }
                if (character == '\n')
                {
                    position = new Vector2(textRender.Position.X, position.Y - textRender.LineHeight);
                    continue;
                }
                if (character == ' ')
                {
                    position = new Vector2(position.X + textRender.SpaceWidth, position.Y);
                    continue;
                }

                // this assumes that the char sprite has the same texture atlas.
                // Find a nicer way around that rather than blindly assuming things have been setup correctly
                var sprite = textRender.CharacterSprites[character];
                var textureSlot = GetTextureSlot(sprite.Texture);

                var textureCoordinates = new Vector2[4];

                for (var i = 0; i < 4; i++)
                {
                    var og = sprite.TextureCoordinates[i];

                    textureCoordinates[i] = new Vector2(
                        og.X * sprite.Texture.Dimensions.X / _textureArray.Width,
                        og.Y * sprite.Texture.Dimensions.Y / _textureArray.Height);
                }

                renderBatch.RenderText(position, sprite.Dimensions, sprite.Origin, textureCoordinates, textureSlot, textRender.Transparency);
                
                position = new Vector2(position.X + sprite.Dimensions.X, position.Y);
            }
        }
    }

    private sealed class Render2DBatch
    {
        public const uint MaxQuadCount = 20000;

        // Text
        private uint _textInstanceCount;
        public required ShaderProgram TextShader { get; init; }
        public required BufferObject<Vector3> TextVertexBuffer { get; init; }
        public required BufferObject<TextInstanceData> TextInstanceBuffer { get; init; }
        public required VertexArrayObject TextVertexArrayObject { get; init; }
        private readonly TextInstanceData[] _textInstanceData = new TextInstanceData[MaxQuadCount];

        // Sprites
        private uint _spriteInstanceCount;
        public required ShaderProgram SpriteShader { get; init; }
        public required BufferObject<Vector3> SpriteVertexBuffer { get; init; }
        public required BufferObject<SpriteInstanceData> SpriteInstanceBuffer { get; init; }
        public required VertexArrayObject SpriteVertexArrayObject { get; init; }
        private readonly SpriteInstanceData[] _spriteInstanceData = new SpriteInstanceData[MaxQuadCount];

        public required GL OpenGl { get; init; }
        public required Matrix4x4 ScreenSpaceProjection { get; init; }
        public required Matrix4x4 WorldViewProjection { get; init; }
        public required TextureArray TextureArray { get; init; }

        public void Flush()
        {
            if (_textInstanceCount > 0)
            {
                TextShader.UseProgram();
                TextShader.SetUniform1("uProjection", ScreenSpaceProjection);

                TextureArray.Bind();

                TextVertexArrayObject.Bind();

                TextVertexBuffer.Bind();

                TextInstanceBuffer.Bind();
                TextInstanceBuffer.SetData(_textInstanceData.AsSpan(0, (int)_textInstanceCount));

                OpenGl.DrawElementsInstanced(PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, ReadOnlySpan<uint>.Empty, _textInstanceCount);
                _textInstanceCount = 0;
            }

            if (_spriteInstanceCount > 0)
            {
                SpriteShader.UseProgram();
                SpriteShader.SetUniform1("uViewProjection", WorldViewProjection);

                TextureArray.Bind();

                SpriteVertexArrayObject.Bind();

                SpriteVertexBuffer.Bind();

                SpriteInstanceBuffer.Bind();
                SpriteInstanceBuffer.SetData(_spriteInstanceData.AsSpan(0, (int)_spriteInstanceCount));

                OpenGl.DrawElementsInstanced(PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, ReadOnlySpan<uint>.Empty, _spriteInstanceCount);
                _spriteInstanceCount = 0;
            }
        }

        public void RenderText(
            Vector2 position,
            Vector2 spriteDimensions,
            SpriteOrigin spriteOrigin,
            Vector2[] textureCoords,
            uint textureSlot,
            float transparency)
        {
            if (_textInstanceCount >= MaxQuadCount)
            {
                Flush();
            }

            _textInstanceData[_textInstanceCount] = new TextInstanceData
            {
                Transparency = transparency,
                TextureSlot = textureSlot,
                TextureCoordinate1 = textureCoords[0],
                TextureCoordinate2 = textureCoords[1],
                TextureCoordinate3 = textureCoords[2],
                TextureCoordinate4 = textureCoords[3],
                Position = position,
                Scale = spriteDimensions,
            };

            _textInstanceCount++;
        }

        public void RenderSprite(
            Matrix4x4 transform,
            Vector2 spriteDimensions,
            SpriteOrigin spriteOrigin,
            Vector2[] textureCoords,
            uint textureSlot,
            float transparency)
        {
            if (_spriteInstanceCount >= MaxQuadCount)
            {
                Flush();
            }

            var translation = spriteOrigin switch
            {
                SpriteOrigin.Center => Vector2.Zero,
                SpriteOrigin.TopLeft => new Vector2(spriteDimensions.X * 0.5f, spriteDimensions.Y * -0.5f),
                SpriteOrigin.BottomLeft => new Vector2(spriteDimensions.X * 0.5f, spriteDimensions.Y * 0.5f),
                SpriteOrigin.TopRight => new Vector2(spriteDimensions.X * -0.5f, spriteDimensions.Y * -0.5f),
                SpriteOrigin.BottomRight => new Vector2(spriteDimensions.X * -0.5f, spriteDimensions.Y * 0.5f),
                _ => throw new Exception()
            };
            var transformUpdate = Matrix4x4.CreateTranslation(translation.Extend(0f)) * Matrix4x4.CreateScale(spriteDimensions.Extend(1f));

            _spriteInstanceData[_spriteInstanceCount] = new SpriteInstanceData
            {
                TextureCoordinateA = textureCoords[0],
                TextureCoordinateB = textureCoords[1],
                TextureCoordinateC = textureCoords[2],
                TextureCoordinateD = textureCoords[3],
                ModelMatrix = transformUpdate * transform,
                Transparency = transparency,
                TextureSlot = textureSlot
            };

            _spriteInstanceCount++;
        }
    }

    public void RenderOrthographic(
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

        // todo: depth sorting for transparency

        var renderBatch = new Render2DBatch
        {
            OpenGl = OpenGL,
            TextShader = _textShader,
            TextVertexArrayObject = _textVertexArrayObject,
            TextVertexBuffer = _textSpriteVertexBuffer,
            TextInstanceBuffer = _textSpriteInstanceBuffer,
            ScreenSpaceProjection = worldToScreen * projection,
            WorldViewProjection = viewProjection,
            SpriteInstanceBuffer = _spriteInstanceBuffer,
            SpriteVertexBuffer = _spriteVertexBuffer,
            SpriteVertexArrayObject = _spriteVertexArrayObject,
            SpriteShader = _spriteShader,
            TextureArray = _textureArray
        };

        // world space
        DrawSprites(sprites, renderBatch);
        DrawLines(lines, viewProjection);

        // screen space
        //DrawSprites(screenSprites, worldToScreen * projection);
        DrawText(textRenders, renderBatch);

        renderBatch.Flush();
    }

    public readonly record struct RenderError(GlobalTransform.GetPositionRotationScaleError Error);

    public Result<Unit, RenderError> Render(GlobalTransform cameraTransform, IEnumerable<GlobalTransform> transforms)
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

            OpenGL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, ReadOnlySpan<uint>.Empty);
        }

        return Result.Success<Unit, RenderError>(Unit.Value);
    }

    public void Dispose()
    {
        _spriteVertexBuffer?.Dispose();
        _spriteElementBuffer?.Dispose();
        _spriteVertexArrayObject?.Dispose();
    }
}
