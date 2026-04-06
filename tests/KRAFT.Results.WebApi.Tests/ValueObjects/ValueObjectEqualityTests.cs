using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Tests.ValueObjects;

public sealed class ValueObjectEqualityTests
{
    [Fact]
    public void Email_SameValue_AreEqual()
    {
        // Arrange
        Email email1 = Email.Create("test@example.com").FromResult();
        Email email2 = Email.Create("test@example.com").FromResult();

        // Act & Assert
        email1.ShouldBe(email2);
        (email1 == email2).ShouldBeTrue();
        email1.GetHashCode().ShouldBe(email2.GetHashCode());
    }

    [Fact]
    public void Email_DifferentValue_AreNotEqual()
    {
        // Arrange
        Email email1 = Email.Create("a@example.com").FromResult();
        Email email2 = Email.Create("b@example.com").FromResult();

        // Act & Assert
        (email1 != email2).ShouldBeTrue();
        email1.Equals(email2).ShouldBeFalse();
    }

    [Fact]
    public void Password_SameHashedValue_AreEqual()
    {
        // Arrange
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        // Act & Assert
        password.ShouldBe(same);
        (password == same).ShouldBeTrue();
        password.GetHashCode().ShouldBe(same.GetHashCode());
    }

    [Fact]
    public void Password_DifferentHashedValues_AreNotEqual()
    {
        // Arrange
        Password password1 = Password.Hash("secret123").FromResult();
        Password password2 = Password.Hash("secret456").FromResult();

        // Act & Assert
        (password1 != password2).ShouldBeTrue();
        password1.Equals(password2).ShouldBeFalse();
    }

    [Fact]
    public void Gender_SameValue_AreEqual()
    {
        // Arrange
        Gender gender1 = Gender.Male;
        Gender gender2 = Gender.Parse("m");

        // Act & Assert
        gender1.ShouldBe(gender2);
        (gender1 == gender2).ShouldBeTrue();
        gender1.GetHashCode().ShouldBe(gender2.GetHashCode());
    }

    [Fact]
    public void Gender_DifferentValue_AreNotEqual()
    {
        // Arrange
        Gender male = Gender.Male;
        Gender female = Gender.Female;

        // Act & Assert
        (male != female).ShouldBeTrue();
        male.Equals(female).ShouldBeFalse();
    }

    [Fact]
    public void Slug_SameValue_AreEqual()
    {
        // Arrange
        Slug slug1 = Slug.Create("hello-world");
        Slug slug2 = Slug.Create("hello-world");

        // Act & Assert
        slug1.ShouldBe(slug2);
        (slug1 == slug2).ShouldBeTrue();
        slug1.GetHashCode().ShouldBe(slug2.GetHashCode());
    }

    [Fact]
    public void Slug_DifferentValue_AreNotEqual()
    {
        // Arrange
        Slug slug1 = Slug.Create("hello");
        Slug slug2 = Slug.Create("world");

        // Act & Assert
        (slug1 != slug2).ShouldBeTrue();
        slug1.Equals(slug2).ShouldBeFalse();
    }

    [Fact]
    public void IpfPoints_SameValue_AreEqual()
    {
        // Arrange
        IpfPoints points1 = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints points2 = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);

        // Act & Assert
        points1.ShouldBe(points2);
        (points1 == points2).ShouldBeTrue();
        points1.GetHashCode().ShouldBe(points2.GetHashCode());
    }

    [Fact]
    public void IpfPoints_DifferentValue_AreNotEqual()
    {
        // Arrange
        IpfPoints points1 = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints points2 = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 300m);

        // Act & Assert
        (points1 != points2).ShouldBeTrue();
        points1.Equals(points2).ShouldBeFalse();
    }

    [Fact]
    public void CrossType_SameUnderlyingValue_AreNotEqual()
    {
        // Arrange
        Gender gender = Gender.Male;
        Slug slug = Slug.Create("m");

        // Act
        bool result = gender.Equals(slug);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        Email email = Email.Create("test@example.com").FromResult();

        // Act & Assert
        email.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithSameInstance_ReturnsTrue()
    {
        // Arrange
        Email email = Email.Create("test@example.com").FromResult();

        // Act & Assert
        email.Equals(email).ShouldBeTrue();
    }
}