// <copyright file="Season2ContentRestriction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeason2;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Restricts a Season 6 configuration to the content which existed up to Season 2.
/// </summary>
/// <remarks>
/// Nothing gets deleted which is referenced by other data. Instead, content of later seasons
/// is made unreachable:
/// <list type="bullet">
///   <item>Summoner and Rage Fighter can't be created.</item>
///   <item>There are no master classes (3rd class change), so there is no master level.</item>
///   <item>Maps of later seasons are removed from the warp list (/move) and all gates leading to them are removed.</item>
/// </list>
/// </remarks>
internal class Season2ContentRestriction : InitializerBase
{
    /// <summary>
    /// The map number of devil square 5 to 7, which share one map number but differ by the discriminator.
    /// </summary>
    private const short DevilSquareHighMapNumber = 32;

    /// <summary>
    /// The discriminator of Devil Square 7, which was introduced for master characters.
    /// </summary>
    private const int DevilSquare7Discriminator = 7;

    /// <summary>
    /// The numbers of the maps which were introduced after Season 2.
    /// </summary>
    private static readonly HashSet<short> MapsAfterSeason2 =
    [
        41, // Barracks of Balgass
        42, // Balgass Refuge
        45, 46, 47, 48, 49, 50, // Illusion Temple 1-6
        51, // Elvenland
        52, // Blood Castle 8
        53, // Chaos Castle 7
        56, // Swamp of Calmness
        57, 58, // Raklion
        62, // Santa Village
        63, // Vulcanus
        64, // Duel Arena
        65, 66, 67, 68, // Doppelganger
        69, 70, 71, 72, // Fortress of Imperial Guardian
        79, // Loren Market
        80, 81, // Karutan
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="Season2ContentRestriction"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public Season2ContentRestriction(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        this.RestrictCharacterClasses();
        this.RestrictMaps();
    }

    private static bool IsAfterSeason2(GameMapDefinition map)
    {
        return MapsAfterSeason2.Contains(map.Number)
               || (map.Number == DevilSquareHighMapNumber && map.Discriminator == DevilSquare7Discriminator);
    }

    private void RestrictCharacterClasses()
    {
        // Classes which came after Season 2 can't be created anymore.
        foreach (var classNumber in new[] { CharacterClassNumber.Summoner, CharacterClassNumber.RageFighter })
        {
            if (this.GetCharacterClass(classNumber) is { } characterClass)
            {
                characterClass.CanGetCreated = false;
            }
        }

        // The second classes (and the classes without a second class) are the final classes.
        // This way, a character can't become a master class (3rd class change came with Season 3).
        foreach (var classNumber in new[]
                 {
                     CharacterClassNumber.SoulMaster,
                     CharacterClassNumber.BladeKnight,
                     CharacterClassNumber.MuseElf,
                     CharacterClassNumber.MagicGladiator,
                     CharacterClassNumber.DarkLord,
                 })
        {
            if (this.GetCharacterClass(classNumber) is { } characterClass)
            {
                characterClass.NextGenerationClass = null;
            }
        }
    }

    private void RestrictMaps()
    {
        var removedMaps = this.GameConfiguration.Maps.Where(IsAfterSeason2).ToList();
        var gatesOfRemovedMaps = removedMaps.SelectMany(map => map.ExitGates).ToHashSet();

        // Remove the /move (warp) entries to the maps of later seasons.
        var warpsToRemove = this.GameConfiguration.WarpList
            .Where(warp => warp.Gate is not null && gatesOfRemovedMaps.Contains(warp.Gate))
            .ToList();
        foreach (var warp in warpsToRemove)
        {
            this.GameConfiguration.WarpList.Remove(warp);
        }

        // Remove the gates of the remaining maps which lead to the maps of later seasons.
        foreach (var map in this.GameConfiguration.Maps.Where(map => !IsAfterSeason2(map)))
        {
            var gatesToRemove = map.EnterGates
                .Where(gate => gate.TargetGate is not null && gatesOfRemovedMaps.Contains(gate.TargetGate))
                .ToList();
            foreach (var gate in gatesToRemove)
            {
                map.EnterGates.Remove(gate);
            }
        }
    }
}
