using Robust.Shared.GameStates;

namespace Content.Shared._CM14.IconLabel;

/// <summary>
/// A text icon label on a layer above the rest of the icon
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IconLabelComponent : Component
{
    [DataField, AutoNetworkedField]
    public string LabelText = string.Empty;
}
