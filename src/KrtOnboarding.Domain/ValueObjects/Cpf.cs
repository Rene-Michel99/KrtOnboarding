using System.Diagnostics.CodeAnalysis;

namespace KrtOnboarding.Domain.ValueObjects;

public sealed record Cpf
{
    public string Value { get; }

    private Cpf(string value)
    {
        Value = value;
    }

    public static Cpf Create(string value)
    {
        if (!TryCreate(value, out var cpf))
            throw new ArgumentException("CPF inválido.", nameof(value));

        return cpf;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Cpf? cpf)
    {
        cpf = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var digits = value.Replace(".", "").Replace("-", "");
        if (digits.Length != 11)
            return false;

        for (var i = 0; i < digits.Length; i++)
        {
            if (digits[i] is < '0' or > '9')
                return false;
        }

        if (digits.All(digit => digit == digits[0]))
            return false;

        var firstCheckDigit = CalculateCheckDigit(digits[..9]);
        var secondCheckDigit = CalculateCheckDigit(digits[..9] + firstCheckDigit);

        if (digits[9] - '0' != firstCheckDigit || digits[10] - '0' != secondCheckDigit)
            return false;

        cpf = new Cpf(digits);
        return true;
    }

    private static int CalculateCheckDigit(string digits)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
            sum += (digits[i] - '0') * (digits.Length + 1 - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    public override string ToString() => Value;
}
