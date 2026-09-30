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
/// The game is played with the Season 6 Episode 3 client or the open source client (MuMain).
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
    /// <remarks>
    /// The Season 6 Episode 3 client connects on port 44405 and the open source client (MuMain) on port 44406.
    /// </remarks>
    protected override void CreateGameClientDefinition()
    {
        var season6Client = this.Context.CreateNew<GameClientDefinition>();
        season6Client.SetGuid(0x104D);
        season6Client.Season = 6;
        season6Client.Episode = 3;
        season6Client.Language = ClientLanguage.English;
        season6Client.Version = [0x31, 0x30, 0x34, 0x30, 0x34];
        season6Client.Serial = "k1Pk2jcET48mxL3b"u8.ToArray();
        season6Client.Description = "Season 6 Episode 3 GMO Client";

        // The open source client (MuMain), which uses a slightly extended Season 6 protocol.
        var openSourceClient = this.Context.CreateNew<GameClientDefinition>();
        openSourceClient.SetGuid(0x204D);
        openSourceClient.Season = 106;
        openSourceClient.Episode = 3;
        openSourceClient.Language = ClientLanguage.English;
        openSourceClient.Version = [0x32, 0x30, 0x34, 0x30, 0x34];
        openSourceClient.Serial = "k1Pk2jcET48mxL3b"u8.ToArray();
        openSourceClient.Description = "Season 6 Episode 3 Open Source Client";
    }
}
