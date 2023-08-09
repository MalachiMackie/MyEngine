using MyEngine.Core.Input;

namespace MyEngine.Core.Tests.Input;

public class KeyboardTests
{
    private readonly MyKeyboard _keyboard = new();

    [Fact]
    public void IsKeyPressed_Should_ReturnTrue_When_KeyIsPressed()
    {
        _keyboard.InternalKeyStates[MyKey.N] = KeyState.Pressed;

        _keyboard.IsKeyPressed(MyKey.N).Should().BeTrue();
    }

    [Theory]
    [InlineData(KeyState.Held)]
    [InlineData(KeyState.NotPressed)]
    [InlineData(KeyState.Released)]
    public void IsKeyPressed_Should_ReturnFalse_When_KeyIsNotPressed(KeyState keyState)
    {
        _keyboard.InternalKeyStates[MyKey.N] = keyState;
        _keyboard.IsKeyPressed(MyKey.N).Should().BeFalse();
    }

    [Fact]
    public void IsKeyDown_Should_ReturnTrue_When_KeyIsHeldOrPressed()
    {
        _keyboard.InternalKeyStates[MyKey.N] = KeyState.Held;
        _keyboard.InternalKeyStates[MyKey.M] = KeyState.Pressed;

        _keyboard.IsKeyDown(MyKey.N).Should().BeTrue();
        _keyboard.IsKeyDown(MyKey.M).Should().BeTrue();
    }

    [Fact]
    public void KeyStates_Should_BeNotPressedByDefault()
    {
        _keyboard.KeyStates.Should().BeEquivalentTo(Enum.GetValues<MyKey>()
            .ToDictionary(x => x, _ => KeyState.NotPressed));
    }

}
