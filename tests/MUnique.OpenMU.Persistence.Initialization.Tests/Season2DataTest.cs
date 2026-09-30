// <copyright file="Season2DataTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Quests;
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

    /// <summary>
    /// Tests that items of later seasons don't drop from monsters.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    [TestCase(12, 36)] // Wing of Storm (3rd wings)
    [TestCase(12, 41)] // Wings of Curse (Summoner)
    [TestCase(12, 49)] // Cape of Fighter (Rage Fighter)
    [TestCase(0, 26)] // Flameberge (socket item)
    [TestCase(12, 70)] // Sphere (Mono)
    public void ItemOfLaterSeasonIsNotObtainable(int group, int number)
    {
        var item = this._gameConfiguration.Items.Single(i => i.Group == group && i.Number == number);
        Assert.That(item.DropsFromMonsters, Is.False);
        Assert.That(this._gameConfiguration.DropItemGroups.Where(g => g.PossibleItems.Contains(item)), Is.Empty);
        Assert.That(this._gameConfiguration.Monsters.Where(m => m.MerchantStore?.Items.Any(i => i.Definition == item) is true), Is.Empty);
    }

    /// <summary>
    /// Tests that crafting recipes of later seasons are not available.
    /// </summary>
    /// <param name="craftingNumber">The crafting number.</param>
    [TestCase(38)] // 3rd Level Wings, Stage 1
    [TestCase(39)] // 3rd Level Wings, Stage 2
    [TestCase(37)] // Illusion Temple Ticket
    public void CraftingOfLaterSeasonIsNotAvailable(int craftingNumber)
    {
        var npcs = this._gameConfiguration.Monsters.Where(m => m.ItemCraftings.Any(c => c.Number == craftingNumber));
        Assert.That(npcs, Is.Empty);
    }

    /// <summary>
    /// Tests that the second wings are still craftable.
    /// </summary>
    [Test]
    public void SecondWingsCraftingIsAvailable()
    {
        Assert.That(this._gameConfiguration.Monsters.Any(m => m.ItemCraftings.Any(c => c.Number == 7)), Is.True);
    }

    /// <summary>
    /// Tests that there is no quest for the 3rd class change.
    /// </summary>
    [Test]
    public void NoQuestForThirdClassChange()
    {
        var quests = this._gameConfiguration.Monsters
            .SelectMany(npc => npc.Quests)
            .Where(q => q.Rewards.Any(r => r.RewardType == QuestRewardType.CharacterEvolutionSecondToThird))
            .Select(q => q.Name.ToString())
            .ToList();
        Assert.That(quests, Is.Empty);
    }

    /// <summary>
    /// Tests that there are no quests for classes which can't be reached.
    /// </summary>
    /// <param name="classNumber">The class number.</param>
    [TestCase(20)] // Summoner
    [TestCase(22)] // Bloody Summoner
    public void NoQuestForUnreachableClass(int classNumber)
    {
        var quests = this._gameConfiguration.Monsters
            .SelectMany(npc => npc.Quests)
            .Where(q => q.QualifiedCharacter?.Number == classNumber)
            .Select(q => q.Name.ToString())
            .ToList();
        Assert.That(quests, Is.Empty);
    }

    /// <summary>
    /// Tests that the quest for the 2nd class change still exists.
    /// </summary>
    [Test]
    public void QuestForSecondClassChangeExists()
    {
        var hasQuest = this._gameConfiguration.Monsters
            .SelectMany(npc => npc.Quests)
            .Any(q => q.QualifiedCharacter?.Number == 4 && q.Rewards.Any(r => r.RewardType == QuestRewardType.CharacterEvolutionFirstToSecond));
        Assert.That(hasQuest, Is.True);
    }
}
