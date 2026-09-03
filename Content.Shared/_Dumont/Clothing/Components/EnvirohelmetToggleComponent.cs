using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Inventory;
using Content.Shared.Hands.Components;

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
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// The components to add when activated.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// The components to restore when deactivated.
    /// </summary>
    [DataField]
    public ComponentRegistry ClosedComponents = new();

    /// <summary>
    /// The components to remove when deactivated.
    /// If this is null <see cref="Components"/> is reused.
    /// </summary>
    [DataField]
    public ComponentRegistry? RemoveComponents;

    /// <summary>
    /// If true, adds components on the entity's parent instead of the entity itself.
    /// </summary>
    [DataField]
    public bool Parent;

    // <summary>
    // It holds the entity that the component gave the component to, so it can remove from it even if it changes parent.
    // </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// Sprite layer that will have its visibility toggled when this item is toggled.
    /// </summary>
    [DataField(required: true)]
    public string? SpriteLayer;

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    /// <summary>
    /// Layers to add to the sprite of the player that is wearing this entity (while the component is toggled on).
    /// </summary>
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}

public sealed partial class ToggleEnvirohelmetEvent : InstantActionEvent
{
    /// <summary>
    ///     Generic enum keys for toggle-visualizer appearance data & sprite layers.
    /// </summary>
    [Serializable, NetSerializable]
    public enum ToggleableVisuals : byte
    {
        Enabled,
        Layer
    }

    /// <summary>
    ///     Generic sprite layer keys.
    /// </summary>
    [Serializable, NetSerializable]
    public enum LightLayers : byte
    {
        Light,

        /// <summary>
        ///     Used as a key for generic unshaded layers. Not necessarily related to an entity with an actual light source.
        ///     Use this instead of creating a unique single-purpose "unshaded" enum for every visualizer.
        /// </summary>
        Unshaded,
    }
}

[Serializable, NetSerializable]
public enum EnvirohelmetVisuals : byte
{
    IsOpen
}
