using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Tests.ValueObjects.EmailTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        Email email = Email.Create("test@example.com").FromResult();

        email.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        Email email = Email.Create("test@example.com").FromResult();

        email.Equals(email).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        Email a = Email.Create("test@example.com").FromResult();
        Email b = Email.Create("test@example.com").FromResult();

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        Email a = Email.Create("a@example.com").FromResult();
        Email b = Email.Create("b@example.com").FromResult();

        a.Equals(b).ShouldBeFalse();
    }
}