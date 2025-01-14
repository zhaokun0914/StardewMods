namespace StardewMods.ToolbarIcons.Framework.Services.Integrations.Vanilla;

using StardewMods.ToolbarIcons.Framework.Enums;
using StardewMods.ToolbarIcons.Framework.Interfaces;
using StardewValley.Menus;

/// <inheritdoc />
internal sealed class SpecialOrders : IVanillaIntegration
{
    /// <inheritdoc />
    public string HoverText => I18n.Button_SpecialOrders();

    /// <inheritdoc />
    public string Icon => InternalIcon.SpecialOrders.ToStringFast();

    /// <inheritdoc />
    public void DoAction() => Game1.activeClickableMenu = new SpecialOrdersBoard();
}