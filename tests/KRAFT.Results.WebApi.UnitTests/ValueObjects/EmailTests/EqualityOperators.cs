using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.EmailTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameValue()
    {
        Email a = Email.Create("test@example.com").FromResult();
        Email b = Email.Create("test@example.com").FromResult();

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentValue()
    {
        Email a = Email.Create("a@example.com").FromResult();
        Email b = Email.Create("b@example.com").FromResult();

        (a != b).ShouldBeTrue();
    }
}