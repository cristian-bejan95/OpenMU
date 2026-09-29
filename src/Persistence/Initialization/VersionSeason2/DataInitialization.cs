// <copyright file="DataInitialization.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeason2;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Initialization of the data for a classic Season 2 server.
/// </summary>
/// <remarks>
/// The game data is based on the Season 6 data of OpenMU, restricted to the content
/// which existed up to Season 2 (see <see cref="Season2ContentRestriction"/>).
/// Until the protocol of an original Season 2 client is supported, the server
/// accepts the Season 6 Episode 3 client, so that the data can already be tested in game.
/// </remarks>
[Guid("6E3A5F52-2B0C-4E5B-9C1D-5A2F0E7B8C42")]
[PlugIn]
[Display(Name = nameof(PlugInResources.DataInitializationSeason2_Name), Description = nameof(PlugInResources.DataInitializationSeason2_Description), ResourceType = typeof(PlugInResources))]
public class DataInitialization : DataInitializationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataInitialization"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public DataInitialization(IPersistenceContextProvider persistenceContextProvider, ILoggerFactory loggerFactory)
        : base(persistenceContextProvider, loggerFactory)
    {
    }

    /// <summary>
    /// Gets the identifier of this initialization.
    /// </summary>
    public static string Id => "season2";

    /// <inheritdoc />
    public override string Key => Id;

    /// <inheritdoc />
    public override string Caption => "Season 2 (Classic)";

    /// <inheritdoc/>
    /// <remarks>
    /// The test accounts of Season 6 contain classes which don't exist in Season 2,
    /// so we don't create any test accounts yet.
    /// </remarks>
    protected override IInitializer? TestAccountsInitializer => null;

    /// <inheritdoc/>
    protected override IInitializer GameConfigurationInitializer => new GameConfigurationInitializer(this.Context, this.GameConfiguration);

    /// <inheritdoc/>
    protected override IGameMapsInitializer GameMapsInitializer => new VersionSeasonSix.GameMapsInitializer(this.Context, this.GameConfiguration);

    /// <inheritdoc />
    protected override void CreateGameClientDefinition()
    {
        // Until the Season 2 protocol is implemented, the Season 6 Episode 3 client is used to connect.
        var client = this.Context.CreateNew<GameClientDefinition>();
        client.SetGuid(0x104D);
        client.Season = 6;
        client.Episode = 3;
        client.Language = ClientLanguage.English;
        client.Version = [0x31, 0x30, 0x34, 0x30, 0x34];
        client.Serial = "k1Pk2jcET48mxL3b"u8.ToArray();
        client.Description = "Season 6 Episode 3 GMO Client (temporary for Season 2 data)";
    }
}
