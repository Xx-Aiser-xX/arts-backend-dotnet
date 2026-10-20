using Arts.Domain.Entities;
using FluentAssertions;

namespace Arts.Domain.UnitTests;

public class UserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    public void Constructor_WhenInvalidUserName_ShouldThrowArgumentException(string? invalidUserName)
    {
        var act = () => new User(Guid.NewGuid(), invalidUserName!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("userName");
    }

    [Fact]
    public void UpdateProfile_WhenClearingAvatarAndDescription_ShouldAllowNull()
    {
        var user = new User(Guid.NewGuid(), "john_doe");
        user.UpdateProfile("john_doe", "Bio", "http://avatar.jpg");
        user.UpdateProfile("john_doe", null, null);

        user.Description.Should().BeNull();
        user.PhotoUrl.Should().BeNull();
    }

    [Fact]
    public void DecrementJobs_WhenZero_ShouldNotDropBelowZero()
    {
        var user = new User(Guid.NewGuid(), "artist");
        user.DecrementJobs();
        user.CountJobs.Should().Be(0);
    }
}