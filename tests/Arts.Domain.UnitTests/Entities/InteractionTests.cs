using Arts.Domain.Entities;
using FluentAssertions;

namespace Arts.Domain.UnitTests;

public class InteractionTests
{
    [Fact]
    public void ToggleLike_ShouldFlipLikeStatusAndManageLikedAtDate()
    {
        var user = new User(Guid.NewGuid(), "viewer");
        var art = new Art("Super Art", "Desc", "http://pic.jpg", false, user);
        var interaction = new Interaction(user, art);

        interaction.Like.Should().BeFalse();
        interaction.LikedAt.Should().BeNull();
        interaction.View.Should().BeTrue();

        interaction.ToggleLike();

        interaction.Like.Should().BeTrue();
        interaction.LikedAt.Should().NotBeNull();
        interaction.LikedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        interaction.ToggleLike();

        interaction.Like.Should().BeFalse();
        interaction.LikedAt.Should().BeNull();
    }
}