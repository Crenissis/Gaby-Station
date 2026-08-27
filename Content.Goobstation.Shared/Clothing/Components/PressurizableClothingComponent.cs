using Content.Goobstation.Shared.Clothing.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Clothing.Components;

/// <summary>
///     Component used to designate control of pressurizable clothing. It'll contain action to pressurize clothing.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPressurizableClothingSystem))]
public sealed partial class PressurizableClothingComponent : Component
{
    /// <summary>
    ///     Action that used to start pressurizing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId PressurizeAction = "ActionClothingPressurize";

    [DataField, AutoNetworkedField]
    public EntityUid? PressurizeActionEntity;

    /// <summary>
    ///     Slot required for the clothing to show action
    /// </summary>
    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredControlSlot = SlotFlags.OUTERCLOTHING;

    /// <summary>
    ///     True if clothing in pressurizing/depressurizing process, false if not
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInProcess = false;

    /// <summary>
    ///     True if clothing is currently pressurized and need to start depressurizing process. False if opposite.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCurrentlyPressurized = false;

    /// <summary>
    ///     Queue of attached parts that should be pressurized/depressurized
    /// </summary>
    [DataField, AutoNetworkedField]
    public Queue<NetEntity> ProcessQueue = new();

    /// <summary>
    ///     Uid of entity that currently wear the clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? WearerEntity;

    /// <summary>
    ///     Doafter time for other players to start pressurizing via stripping menu
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NonWearerPressurizingTime = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     if true; after ClothingControlPressurizeCompleteEvent it will unToggle the control
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UnequipAfterUnpressurize = false;

    [DataField, AutoNetworkedField]
    public bool IsPressurized = false;

    [DataField, AutoNetworkedField]
    public TimeSpan PressurizingTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public LocId PressurizeUpPopup = "sealable-clothing-seal-up";

    [DataField]
    public LocId PressurizeDownPopup = "sealable-clothing-seal-down";

    [DataField]
    public SoundSpecifier PressurizeUpSound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");

    [DataField]
    public SoundSpecifier PressurizeDownSound = new SoundPathSpecifier("/Audio/Mecha/mechmove03.ogg");

    #region Popups & Sounds

    [DataField]
    public LocId ToggleFailedPopup = "sealable-clothing-equipment-not-toggled";

    [DataField]
    public LocId PressurizeFailedPopup = "sealable-clothing-equipment-seal-failed";

    [DataField]
    public LocId PressurizedInProcessToggleFailPopup = "sealable-clothing-sealed-process-toggle-fail";

    [DataField]
    public LocId UnpressurizedInProcessToggleFailPopup = "sealable-clothing-unsealed-process-toggle-fail";

    [DataField]
    public LocId CurrentlyPressurizedToggleFailPopup = "sealable-clothing-pressurized-toggle-fail";

    [DataField]
    public LocId PressurizeBrokenPopup = "sealable-clothing-seal-was-broken";

    [DataField]
    public LocId VerbText = "sealable-clothing-seal-verb";

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/_Goobstation/Machines/ErrorBeep2.wav");

    [DataField]
    public SoundSpecifier PressurizeCompleteSound = new SoundPathSpecifier("/Audio/_Goobstation/Mecha/nominal.ogg");

    [DataField]
    public SoundSpecifier UnpressurizeCompleteSound = new SoundPathSpecifier("/Audio/_Goobstation/Machines/computer_end.ogg");

    [DataField]
    public SoundSpecifier GenericSuitWarning = new SoundPathSpecifier("/Audio/_Goobstation/Machines/MaxTempAlertCut.wav");
    #endregion
}
