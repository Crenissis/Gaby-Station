using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Inventory;

namespace Content.Shared._Dumont.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnvirohelmetToggleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField]
    public EntProtoId Action = "ActionToggleEnvirohelmet";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public string StateClosed = "";

    [DataField]
    public string StateOpen = "open-equipped-HELMET";

    [DataField]
    public string OpenVisualLayer = "open";

    [DataField]
    public string ClosedVisualLayer = "closed";

    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    // Componentes aplicados quando o capacete está fechado
    [DataField]
    public ComponentRegistry ClosedComponents = new();

    // Componentes aplicados quando o capacete está aberto
    [DataField]
    public ComponentRegistry OpenComponents = new();

    // Camadas visuais equipadas para cada modo (chave = SlotFlags)
    [DataField]
    public Dictionary<SlotFlags, List<PrototypeLayerData>>? ClosedClothingVisuals;

    [DataField]
    public Dictionary<SlotFlags, List<PrototypeLayerData>>? OpenClothingVisuals;
}

public sealed partial class ToggleEnvirohelmetEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum EnvirohelmetVisuals : byte
{
    IsOpen,
    IsClosed
}
