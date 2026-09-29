// <copyright file="Season2DataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the Season 2 (Classic) data initialization.
/// </summary>
[TestFixture]
internal class Season2DataTest
{
    private static readonly short[] MapsAfterSeason2 = [41, 42, 45, 46, 47, 48, 49, 50, 51, 52, 53, 56, 57, 58, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 79, 80, 81];

    private GameConfiguration _gameConfiguration = null!;

    /// <summary>
    /// Creates the Season 2 configuration once for all tests.
    /// </summary>
    [OneTimeSetUp]
    public async Task CreateConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeason2.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, false).ConfigureAwait(false);
        using var context = contextProvider.CreateNewContext();
        this._gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
    }

    /// <summary>
    /// Tests that Summoner and Rage Fighter can't be created.
    /// </summary>
    /// <param name="classNumber">The class number.</param>
    [TestCase(20)]
    [TestCase(24)]
    public void ClassOfLaterSeasonCantBeCreated(int classNumber)
    {
        var characterClass = this._gameConfiguration.CharacterClasses.Single(c => c.Number == classNumber);
        Assert.That(characterClass.CanGetCreated, Is.False);
    }

    /// <summary>
    /// Tests that the classes of Season 2 can still be created.
    /// </summary>
    /// <param name="classNumber">The class number.</param>
    [TestCase(0)]
    [TestCase(4)]
    [TestCase(8)]
    [TestCase(12)]
    [TestCase(16)]
    public void ClassOfSeason2CanBeCreated(int classNumber)
    {
        var characterClass = this._gameConfiguration.CharacterClasses.Single(c => c.Number == classNumber);
        Assert.That(characterClass.CanGetCreated, Is.True);
    }

    /// <summary>
    /// Tests that there is no way to become a master class.
    /// </summary>
    [Test]
    public void NoClassLeadsToMasterClass()
    {
        var classesLeadingToMaster = this._gameConfiguration.CharacterClasses
            .Where(c => c.NextGenerationClass?.IsMasterClass is true)
            .Select(c => c.Name.ToString())
            .ToList();
        Assert.That(classesLeadingToMaster, Is.Empty);
    }

    /// <summary>
    /// Tests that the warp list doesn't contain maps of later seasons.
    /// </summary>
    [Test]
    public void WarpListContainsNoMapOfLaterSeasons()
    {
        var warps = this._gameConfiguration.WarpList
            .Where(w => w.Gate?.Map is { } map && MapsAfterSeason2.Contains(map.Number))
            .Select(w => w.Name.ToString())
            .ToList();
        Assert.That(warps, Is.Empty);
        Assert.That(this._gameConfiguration.WarpList.Any(w => w.Gate?.Map?.Number == 0), Is.True, "Lorencia should still be in the warp list.");
    }

    /// <summary>
    /// Tests that no gate of a Season 2 map leads to a map of a later season.
    /// </summary>
    [Test]
    public void NoGateLeadsToMapOfLaterSeasons()
    {
        var gates = this._gameConfiguration.Maps
            .Where(m => !MapsAfterSeason2.Contains(m.Number))
            .SelectMany(m => m.EnterGates.Select(g => (Map: m, Gate: g)))
            .Where(t => t.Gate.TargetGate?.Map is { } target && MapsAfterSeason2.Contains(target.Number))
            .Select(t => $"{t.Map.Name} gate {t.Gate.Number}")
            .ToList();
        Assert.That(gates, Is.Empty);
    }
}
