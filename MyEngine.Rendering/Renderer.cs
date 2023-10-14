using System.Drawing;
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
        BufferObject<SpriteVertexData> spriteVertexBuffer,
        BufferObject<uint> spriteElementBuffer,
        VertexArrayObject spriteVertexArrayObject,
        ShaderProgram spriteShader,
        ShaderProgram lineShader,
        BufferObject<float> lineVertexBuffer,
        VertexArrayObject lineVertexArray,
        BufferObject<uint> textElementBuffer,
        BufferObject<TextVertexData> textSpriteVertexBuffer,
        BufferObject<TextInstanceData> textSpriteInstanceBuffer,
        VertexArrayObject textVertexArrayObject,
        ShaderProgram textShader,
        BufferObject<SpriteInstanceData> spriteInstanceBuffer
        )
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
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextVertexData
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextInstanceData
    {
        public float Transparency;
        public float TextureSlot;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpriteVertexData
    {
        public Vector3 Position;
        public Vector2 TextCoordinate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SpriteInstanceData
    {
        public Matrix4x4 ModelMatrix;
        public float Transparency;
    }

    internal GL OpenGL { get; }
    private readonly BufferObject<SpriteVertexData> _spriteVertexBuffer;
    private readonly BufferObject<uint> _spriteElementBuffer;
    private readonly BufferObject<SpriteInstanceData> _spriteInstanceBuffer;
    private readonly VertexArrayObject _spriteVertexArrayObject;
    private readonly ShaderProgram _spriteShader;
    private readonly ShaderProgram _lineShader;
    private readonly BufferObject<TextVertexData> _textSpriteVertexBuffer;
    private readonly BufferObject<TextInstanceData> _textSpriteInstanceBuffer;
    private readonly BufferObject<uint> _textElementBuffer;
    private readonly VertexArrayObject _textVertexArrayObject;
    private readonly ShaderProgram _textShader;

    private readonly BufferObject<float> _lineVertexBuffer;
    private readonly VertexArrayObject _lineVertexArray;
    private readonly Dictionary<AssetId, TextureObject> _textureObjects = new();

    private uint _width;
    private uint _height;

    internal static Result<Renderer, RendererLoadError> Create(GL openGL)
    {
        var spriteIndices = new uint[Render2DBatch.MaxQuadCount * 6];
        uint offset = 0;
        for (var i = 0; i < spriteIndices.Length; i += 6)
        {
            spriteIndices[i + 0] = offset + 0;
            spriteIndices[i + 1] = offset + 1;
            spriteIndices[i + 2] = offset + 3;

            spriteIndices[i + 3] = offset + 1;
            spriteIndices[i + 4] = offset + 2;
            spriteIndices[i + 5] = offset + 3;

            offset += 4;
        }

        // todo: try catch around all opengl stuff
        openGL.ClearColor(Color.CornflowerBlue);

        var vertexBuffer = BufferObject<SpriteVertexData>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        var elementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        elementBuffer.SetData(spriteIndices);

        var spriteInstanceBuffer = BufferObject<SpriteInstanceData>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);

        var vertexArrayObject = VertexArrayObject.CreateAndBind(openGL);
        vertexArrayObject.AttachBuffer(elementBuffer);
        vertexArrayObject.AttachBuffer(vertexBuffer);

        vertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, sizeof(float), false, 5, 0); // vertex location
        vertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, sizeof(float), false, 5, 3); // texture coordinate

        vertexArrayObject.AttachBuffer(spriteInstanceBuffer);

        // model matrix needs 4 attributes, because attributes can only hold 4 values each
        vertexArrayObject.VertexArrayAttribute(2, 4, VertexAttribPointerType.Float, sizeof(float), false, 17, 0); // model matrix
        vertexArrayObject.VertexArrayAttribute(3, 4, VertexAttribPointerType.Float, sizeof(float), false, 17, 4); // model matrix
        vertexArrayObject.VertexArrayAttribute(4, 4, VertexAttribPointerType.Float, sizeof(float), false, 17, 8); // model matrix
        vertexArrayObject.VertexArrayAttribute(5, 4, VertexAttribPointerType.Float, sizeof(float), false, 17, 12); // model matrix
        vertexArrayObject.VertexArrayAttribute(6, 1, VertexAttribPointerType.Float, sizeof(float), false, 17, 16); // transparency

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

        var textSpriteInstanceBuffer = BufferObject<TextInstanceData>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        var textSpriteVertexBuffer = BufferObject<TextVertexData>.CreateAndBind(openGL, BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw);
        var textElementBuffer = BufferObject<uint>.CreateAndBind(openGL, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw);
        textElementBuffer.SetData(spriteIndices);

        var textVertexArrayObject = VertexArrayObject.CreateAndBind(openGL);
        textVertexArrayObject.AttachBuffer(textElementBuffer);
        textVertexArrayObject.AttachBuffer(textSpriteVertexBuffer);

        textVertexArrayObject.VertexArrayAttribute(0, 3, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 5, offsetSize: 0); // vertex location
        textVertexArrayObject.VertexArrayAttribute(1, 2, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 5, offsetSize: 3); // texture coordinate

        textVertexArrayObject.AttachBuffer(textSpriteInstanceBuffer);

        textVertexArrayObject.VertexArrayAttribute(2, 1, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 2, offsetSize: 0); // transparency
        textVertexArrayObject.VertexArrayAttribute(3, 1, VertexAttribPointerType.Float, sizeof(float), normalized: false, vertexSize: 2, offsetSize: 1); // textureSlot

        openGL.VertexAttribDivisor(2, 1);
        openGL.VertexAttribDivisor(3, 1);

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
            spriteInstanceBuffer
            );

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

    private TextureObject GetTextureObject(Core.Rendering.Texture texture)
    {
        if (!_textureObjects.TryGetValue(texture.Id, out var textureObject))
        {
            textureObject = new TextureObject(OpenGL, texture.Data, (uint)texture.Dimensions.X, (uint)texture.Dimensions.Y, TextureTarget.Texture2D, TextureUnit.Texture0);
            _textureObjects[texture.Id] = textureObject;
        }
        return textureObject;
    }

    void BindOrAddAndBind(Core.Rendering.Texture texture)
    {
        if (!_textureObjects.TryGetValue(texture.Id, out var textureObject))
        {
            textureObject = new TextureObject(OpenGL, texture.Data, (uint)texture.Dimensions.X, (uint)texture.Dimensions.Y, TextureTarget.Texture2D, TextureUnit.Texture0);
            _textureObjects[texture.Id] = textureObject;
        }

        textureObject.Bind(TextureUnit.Texture0);
    }

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

    private void DrawSprites(IEnumerable<SpriteRender> sprites, Matrix4x4 viewProjection)
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
                    new SpriteVertexData{Position = new Vector3(rightEdge, topEdge, 0), TextCoordinate = textureCoords[0]},
                    new SpriteVertexData{Position = new Vector3(rightEdge, bottomEdge, 0), TextCoordinate = textureCoords[1]},
                    new SpriteVertexData{Position = new Vector3(leftEdge, bottomEdge, 0), TextCoordinate = textureCoords[2]},
                    new SpriteVertexData{Position = new Vector3(leftEdge, topEdge, 0), TextCoordinate = textureCoords[3]},
                };

                _spriteVertexBuffer.Bind();
                _spriteVertexBuffer.SetData(data);

                _spriteInstanceBuffer.Bind();
                var instanceData = textureCoordGrouping.Select(x => new SpriteInstanceData
                {
                    ModelMatrix = x.ModelMatrix,
                    Transparency = x.Transparency
                }).ToArray();
                _spriteInstanceBuffer.SetData(instanceData);

                OpenGL.DrawElementsInstanced(PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, ReadOnlySpan<uint>.Empty, (uint)instanceData.Length);
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

    private void DrawText(IEnumerable<TextRender> textRenders, Render2DBatch renderBatch)
    {
        foreach (var textRender in textRenders)
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
                var texture = GetTextureObject(sprite.Texture);

                var bottomLeft = Vector2.Zero;
                var topRight = Vector2.One;
                var textureCoordinates = new[]
                {
                    new Vector2(topRight.X, bottomLeft.Y),
                    new Vector2(topRight.X, topRight.Y),
                    new Vector2(bottomLeft.X, topRight.Y),
                    new Vector2(bottomLeft.X, bottomLeft.Y),
                };

                renderBatch.RenderText(position, sprite.Dimensions, sprite.Origin, sprite.TextureCoordinates, texture, textRender.Transparency);
                
                position = new Vector2(position.X + charWidth, position.Y);
            }
        }
    }

    private sealed class Render2DBatch
    {
        public uint SpriteCount { get; }
        public uint LineCount { get; }
        public uint TextInstanceCount { get; private set; }
        public const uint MaxQuadCount = 20000;
        public required ShaderProgram TextShader { get; init; }
        public required BufferObject<TextVertexData> TextVertexBuffer { get; init; }
        public required BufferObject<TextInstanceData> TextInstanceBuffer { get; init; }
        public required BufferObject<uint> TextElementBuffer { get; init; }
        public required VertexArrayObject TextVertexArrayObject { get; init; }
        public required GL OpenGl { get; init; }
        public required Matrix4x4 ScreenSpaceProjection { get; init; }

        private const uint MaxTextureSlots = 32;
        public Dictionary<TextureObject, uint> UsedTextureSlots { get; } = new(capacity: (int)MaxTextureSlots);

        private const uint TextSpriteVertexSize = 3 + 2 + 1 + 1;
        public TextVertexData[] TextVertexData = new TextVertexData[MaxQuadCount];
        public TextInstanceData[] TextInstanceData = new TextInstanceData[MaxQuadCount];

        public void Flush()
        {
            if (TextInstanceCount > 0)
            {
                TextShader.UseProgram();
                TextShader.SetUniform1("uProjection", ScreenSpaceProjection);
                TextVertexArrayObject.Bind();

                TextVertexBuffer.Bind();
                TextVertexBuffer.SetData(TextVertexData.AsSpan(0, (int)TextInstanceCount * 4));

                TextInstanceBuffer.Bind();
                TextInstanceBuffer.SetData(TextInstanceData.AsSpan(0, (int)TextInstanceCount));

                foreach (var (textureObject, textureSlot) in UsedTextureSlots)
                {
                    textureObject.Bind(textureSlot);
                }

                OpenGl.DrawElementsInstanced(PrimitiveType.Triangles, 6u * TextInstanceCount, DrawElementsType.UnsignedInt, ReadOnlySpan<uint>.Empty, TextInstanceCount);
                TextInstanceCount = 0;
            }
            UsedTextureSlots.Clear();
        }

        private uint GetTextureSlot(TextureObject texture)
        {
            if (!UsedTextureSlots.TryGetValue(texture, out var slot))
            {
                if (UsedTextureSlots.Count >= MaxTextureSlots)
                {
                    Flush();
                }
                slot = UsedTextureSlots.Count == 0 ? 0 : UsedTextureSlots.Values.Max();
                UsedTextureSlots.Add(texture, slot);
                texture.Bind(slot);
            }
            return slot;
        }

        public void RenderText(
            Vector2 position,
            Vector2 spriteDimensions,
            SpriteOrigin spriteOrigin,
            Vector2[] textureCoords,
            TextureObject texture,
            float transparency)
        {
            if (TextInstanceCount >= MaxQuadCount)
            {
                Flush();
            }

            var slot = GetTextureSlot(texture);
            var (leftEdge, rightEdge, bottomEdge, topEdge) = GetRectEdges(spriteDimensions, spriteOrigin);

            Span<Vector2> vertexPositions = stackalloc[]
            {
                new Vector2(rightEdge, topEdge)  + position,
                new Vector2(rightEdge, bottomEdge) + position,
                new Vector2(leftEdge, bottomEdge) + position,
                new Vector2(leftEdge, topEdge) + position
            }; 

            for (var i = 0; i < vertexPositions.Length; i++)
            {
                TextVertexData[(TextInstanceCount * 4) + i] = new TextVertexData
                {
                    Position = vertexPositions[i].Extend(0f),
                    TextureCoordinate = textureCoords[i],
                };
            }

            TextInstanceData[TextInstanceCount] = new TextInstanceData
            {
                Transparency = transparency,
                TextureSlot = slot
            };

            TextInstanceCount++;
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
            TextElementBuffer = _textElementBuffer,
        };

        // world space
        DrawSprites(sprites, viewProjection);
        DrawLines(lines, viewProjection);

        // screen space
        DrawSprites(screenSprites, worldToScreen * projection);
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
        foreach (var texture in _textureObjects.Values)
        {
            texture.Dispose();
        }
    }
}
