using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Abstractions;

public sealed class ValueObjectEqualsTests
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        Email email = Email.Create("test@example.com").FromResult();

        email.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsFalse_WhenComparingDifferentSubtypesWithSameUnderlyingValue()
    {
        Gender gender = Gender.Male;
        Slug slug = Slug.Create("m");

        gender.Equals(slug).ShouldBeFalse();
    }
}