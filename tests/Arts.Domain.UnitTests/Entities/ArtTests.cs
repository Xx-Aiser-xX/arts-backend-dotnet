using Arts.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Arts.Domain.UnitTests;

public class ArtTests
{
    private static User CreateValidUser() => new(Guid.NewGuid(), "valid_artist");

    [Fact]
    public void Constructor_WhenValidData_ShouldInitializeCorrectly()
    {
        var author = CreateValidUser();
        var beforeCreation = DateTime.UtcNow;

        var art = new Art("Masterpiece", "Some description", "http://image.url/pic.jpg", false, author);

        art.Name.Should().Be("Masterpiece");
        art.Description.Should().Be("Some description");
        art.ImageUrl.Should().Be("http://image.url/pic.jpg");
        art.IsNsfw.Should().BeFalse();
        art.Author.Should().Be(author);
        art.AuthorId.Should().Be(author.Id);

        art.CountLikes.Should().Be(0);
        art.CountViews.Should().Be(0);
        art.IsDeleted.Should().BeFalse();

        art.PublicationTime.Should().BeOnOrAfter(beforeCreation);
        art.PublicationTime.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    public void Constructor_WhenInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        var author = CreateValidUser();
        var act = () => new Art(invalidName!, "Desc", "http://url.jpg", false, author);
        act.Should().Throw<ArgumentException>()
           .WithParameterName("name");
    }

    [Fact]
    public void DecrementLikes_WhenLikesIsZero_ShouldNotDropBelowZero()
    {
        var art = new Art("Valid Art Name", "Desc", "http://url.jpg", false, CreateValidUser());
        art.DecrementLikes();
        art.CountLikes.Should().Be(0);
    }

    [Fact]
    public void IncrementAndDecrementLikes_ShouldChangeCounterCorrectly()
    {
        var art = new Art("Valid Art Name", "Desc", "http://url.jpg", false, CreateValidUser());

        art.IncrementLikes();
        art.IncrementLikes();
        art.CountLikes.Should().Be(2);

        art.DecrementLikes();
        art.CountLikes.Should().Be(1);
    }

    [Fact]
    public void UpdateDetails_WhenValid_ShouldUpdateState()
    {
        var art = new Art("Old Art Name", "Old Desc", "http://url.jpg", false, CreateValidUser());
        art.UpdateDetails("New Brand Art", "New Description", true);

        art.Name.Should().Be("New Brand Art");
        art.Description.Should().Be("New Description");
        art.IsNsfw.Should().BeTrue();
    }
}