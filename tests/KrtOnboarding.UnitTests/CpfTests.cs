using KrtOnboarding.Domain.ValueObjects;

namespace KrtOnboarding.UnitTests;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void TryCreate_ValidCpf_ReturnsNormalizedCpf(string value)
    {
        var isValid = Cpf.TryCreate(value, out var cpf);

        Assert.True(isValid);
        Assert.NotNull(cpf);
        Assert.Equal("52998224725", cpf.Value);
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("52998224724")]
    [InlineData("１２３４５６７８９０１")]
    public void TryCreate_InvalidCpf_ReturnsFalse(string value)
    {
        Assert.False(Cpf.TryCreate(value, out _));
    }
}
