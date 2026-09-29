// <copyright file="GameConfigurationInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeason2;

/// <summary>
/// Initializes the <see cref="GameConfiguration"/> for a classic Season 2 server.
/// </summary>
/// <remarks>
/// It creates the complete Season 6 configuration first, so that all references between
/// maps, monsters, items and events stay consistent. Afterwards, the content which came
/// after Season 2 is made unreachable by the <see cref="Season2ContentRestriction"/>
/// and the <see cref="Season2ItemRestriction"/>.
/// </remarks>
public class GameConfigurationInitializer : VersionSeasonSix.GameConfigurationInitializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigurationInitializer"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public GameConfigurationInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        new Season2ContentRestriction(this.Context, this.GameConfiguration).Initialize();
        new Season2ItemRestriction(this.Context, this.GameConfiguration).Initialize();
    }
}
