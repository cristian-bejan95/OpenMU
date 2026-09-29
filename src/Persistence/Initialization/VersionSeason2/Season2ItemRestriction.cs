// <copyright file="Season2ItemRestriction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeason2;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// Makes the items which were introduced after Season 2 unobtainable.
/// </summary>
/// <remarks>
/// The item definitions themselves are kept, because other data may refer to them.
/// An item of a later season is removed from every source a player can get it from:
/// monster drops, drop groups (including the content of boxes and event rewards),
/// merchant stores and crafting (chaos machine) results.
/// <para>
/// An item counts as an item of a later season, if:
/// <list type="bullet">
///   <item>it can only be used by classes which can't be reached in Season 2 (Summoner, Rage Fighter or master classes, e.g. 3rd wings),</item>
///   <item>it has sockets, or it's part of the socket system (seeds, spheres, seed spheres),</item>
///   <item>it's the ticket of an event whose map was introduced after Season 2 (e.g. Illusion Temple).</item>
/// </list>
/// </para>
/// Crafting recipes of later seasons (3rd wings, seed and sphere recipes, Illusion Temple tickets,
/// the complete Secromicon and the cherry blossom event) are removed as well.
/// </remarks>
internal class Season2ItemRestriction : InitializerBase
{
    /// <summary>
    /// The item group of the socket system items (seeds, spheres, seed spheres).
    /// </summary>
    private const byte SocketSystemItemGroup = 12;

    /// <summary>
    /// The numbers of the crafting recipes which were introduced after Season 2.
    /// </summary>
    private static readonly HashSet<byte> CraftingsAfterSeason2 =
    [
        37, // Illusion Temple Ticket
        38, // 3rd Level Wings, Stage 1
        39, // 3rd Level Wings, Stage 2
        41, // Cherry Blossom Event Mix
        42, // Seed Creation
        43, // Seed Sphere Creation
        44, // Mount Seed Sphere
        45, // Remove Seed Sphere
        46, // Complete Secromicon
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="Season2ItemRestriction"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public Season2ItemRestriction(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        var restrictedItems = this.DetermineItemsAfterSeason2();

        foreach (var item in restrictedItems)
        {
            item.DropsFromMonsters = false;
        }

        foreach (var dropItemGroup in this.GetAllDropItemGroups())
        {
            foreach (var item in dropItemGroup.PossibleItems.Where(restrictedItems.Contains).ToList())
            {
                dropItemGroup.PossibleItems.Remove(item);
            }
        }

        foreach (var monster in this.GameConfiguration.Monsters)
        {
            if (monster.MerchantStore is { } store)
            {
                foreach (var storeItem in store.Items.Where(i => i.Definition is { } definition && restrictedItems.Contains(definition)).ToList())
                {
                    store.Items.Remove(storeItem);
                }
            }

            foreach (var crafting in monster.ItemCraftings.Where(c => IsCraftingAfterSeason2(c, restrictedItems)).ToList())
            {
                monster.ItemCraftings.Remove(crafting);
            }
        }
    }

    private static bool IsCraftingAfterSeason2(ItemCrafting crafting, HashSet<ItemDefinition> restrictedItems)
    {
        if (CraftingsAfterSeason2.Contains(crafting.Number))
        {
            return true;
        }

        // A recipe which only results in items of later seasons isn't useful in Season 2.
        var resultItems = crafting.SimpleCraftingSettings?.ResultItems
            .Select(r => r.ItemDefinition)
            .OfType<ItemDefinition>()
            .ToList();
        return resultItems is { Count: > 0 } && resultItems.All(restrictedItems.Contains);
    }

    private static bool IsSocketSystemItem(ItemDefinition item)
    {
        return item.Group == SocketSystemItemGroup
               && item.Number is (>= 60 and <= 65) or (>= 70 and <= 74) or (>= 100 and <= 129);
    }

    private HashSet<ItemDefinition> DetermineItemsAfterSeason2()
    {
        var reachableClasses = this.DetermineReachableClasses();
        var result = new HashSet<ItemDefinition>();
        foreach (var item in this.GameConfiguration.Items)
        {
            var isForUnreachableClassesOnly = item.QualifiedCharacters.Count > 0
                                             && !item.QualifiedCharacters.Any(reachableClasses.Contains);
            if (isForUnreachableClassesOnly || item.MaximumSockets > 0 || IsSocketSystemItem(item))
            {
                result.Add(item);
            }
        }

        // Tickets of events which only take place on maps of later seasons.
        var ticketsOfRemainingEvents = this.GameConfiguration.MiniGameDefinitions
            .Where(game => game.Entrance?.Map is not { } map || !Season2ContentRestriction.IsAfterSeason2(map))
            .Select(game => game.TicketItem)
            .OfType<ItemDefinition>()
            .ToHashSet();
        var ticketsOfRemovedEvents = this.GameConfiguration.MiniGameDefinitions
            .Where(game => game.Entrance?.Map is { } map && Season2ContentRestriction.IsAfterSeason2(map))
            .Select(game => game.TicketItem)
            .OfType<ItemDefinition>()
            .Where(ticket => !ticketsOfRemainingEvents.Contains(ticket));
        result.UnionWith(ticketsOfRemovedEvents);

        return result;
    }

    private HashSet<CharacterClass> DetermineReachableClasses()
    {
        var result = new HashSet<CharacterClass>();
        foreach (var characterClass in this.GameConfiguration.CharacterClasses.Where(c => c.CanGetCreated))
        {
            var current = characterClass;
            while (current is not null && result.Add(current))
            {
                current = current.NextGenerationClass;
            }
        }

        return result;
    }

    private IEnumerable<DropItemGroup> GetAllDropItemGroups()
    {
        return this.GameConfiguration.DropItemGroups
            .Concat(this.GameConfiguration.Monsters.SelectMany(m => m.DropItemGroups))
            .Concat(this.GameConfiguration.Maps.SelectMany(m => m.DropItemGroups))
            .Concat(this.GameConfiguration.Items.SelectMany(i => i.DropItems))
            .Concat(this.GameConfiguration.MiniGameDefinitions.SelectMany(g => g.Rewards).Select(r => r.ItemReward).OfType<DropItemGroup>())
            .Distinct()
            .ToList();
    }
}
