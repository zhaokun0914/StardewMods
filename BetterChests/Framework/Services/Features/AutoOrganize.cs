namespace StardewMods.BetterChests.Framework.Services.Features;

using StardewModdingAPI.Events;
using StardewMods.BetterChests.Framework.Interfaces;
using StardewMods.BetterChests.Framework.Services.Factory;
using StardewMods.Common.Interfaces;
using StardewMods.Common.Services;
using StardewMods.Common.Services.Integrations.BetterChests;
using StardewValley.Menus;

/// <summary>Automatically organizes items between chests during sleep.</summary>
internal sealed class AutoOrganize : BaseFeature<AutoOrganize>
{
    private readonly ContainerFactory containerFactory;
    private readonly ContainerHandler containerHandler;

    /// <summary>Initializes a new instance of the <see cref="AutoOrganize" /> class.</summary>
    /// <param name="containerFactory">Dependency used for accessing containers.</param>
    /// <param name="containerHandler">Dependency used for handling operations by containers.</param>
    /// <param name="eventManager">Dependency used for managing events.</param>
    /// <param name="modConfig">Dependency used for accessing config data.</param>
    public AutoOrganize(
        ContainerFactory containerFactory,
        ContainerHandler containerHandler,
        IEventManager eventManager,
        IModConfig modConfig)
        : base(eventManager, modConfig)
    {
        this.containerHandler = containerHandler;
        this.containerFactory = containerFactory;
    }

    /// <inheritdoc />
    public override bool ShouldBeActive => this.Config.DefaultOptions.AutoOrganize != FeatureOption.Disabled;

    /// <inheritdoc />
    protected override void Activate() => this.Events.Subscribe<DayEndingEventArgs>(this.OnDayEnding);

    /// <inheritdoc />
    protected override void Deactivate() => this.Events.Unsubscribe<DayEndingEventArgs>(this.OnDayEnding);

    private void OnDayEnding(DayEndingEventArgs e) => this.OrganizeAll();

    private void OrganizeAll()
    {
        var containerGroupsTo = this
            .containerFactory.GetAll(container => container.AutoOrganize == FeatureOption.Enabled)
            .GroupBy(container => (int)container.StashToChestPriority)
            .ToDictionary(containerGroup => containerGroup.Key, group => group.ToList());

        var containerGroupsFrom = new Dictionary<int, List<IStorageContainer>>();
        foreach (var (priority, containers) in containerGroupsTo)
        {
            containerGroupsFrom.Add(priority, [..containers]);
        }

        var topPriority = containerGroupsTo.Keys.Max();
        var bottomPriority = containerGroupsTo.Keys.Min();

        for (var priorityTo = topPriority; priorityTo >= bottomPriority; --priorityTo)
        {
            if (!containerGroupsTo.TryGetValue(priorityTo, out var containersTo))
            {
                continue;
            }

            for (var priorityFrom = priorityTo - 1; priorityFrom >= bottomPriority; --priorityFrom)
            {
                if (!containerGroupsFrom.TryGetValue(priorityFrom, out var containersFrom))
                {
                    continue;
                }

                for (var indexTo = containersTo.Count - 1; indexTo >= 0; --indexTo)
                {
                    var containerTo = containersTo[indexTo];
                    for (var indexFrom = containersFrom.Count - 1; indexFrom >= 0; --indexFrom)
                    {
                        var containerFrom = containersFrom[indexFrom];
                        if (!this.containerHandler.Transfer(containerFrom, containerTo, out var amounts))
                        {
                            continue;
                        }

                        foreach (var (name, amount) in amounts)
                        {
                            if (amount > 0)
                            {
                                Log.Info(
                                    "{0}: {{ Item: {1}, Quantity: {2}, From: {3}, To: {4} }}",
                                    this.Id,
                                    name,
                                    amount,
                                    containerFrom,
                                    containerTo);
                            }
                        }
                    }

                    ItemGrabMenu.organizeItemsInList(containerTo.Items);
                }
            }
        }
    }
}