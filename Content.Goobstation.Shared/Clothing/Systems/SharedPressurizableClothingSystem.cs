using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Vector2 = System.Numerics.Vector2;
using Content.Shared.Silicons.StationAi;


namespace Content.Goobstation.Shared.Clothing.Systems;

/// <summary>
///     System used for pressurizable clothing (like envirosuits)
/// </summary>
public abstract class SharedPressurizableClothingSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainerSystem = default!;
    [Dependency] private readonly ComponentTogglerSystem _componentTogglerSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ToggleableClothingSystem _toggleableSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressurizableClothingComponent, ClothingPartPressurizeCompleteEvent>(OnPartPressurizationComplete);

        SubscribeLocalEvent<PressurizableClothingComponent, ClothingControlPressurizeCompleteEvent>(OnControlPressurizationComplete);
        SubscribeLocalEvent<PressurizableClothingComponent, ClothingGotEquippedEvent>(OnControlEquip);
        SubscribeLocalEvent<PressurizableClothingComponent, ClothingGotUnequippedEvent>(OnControlUnequip);
        SubscribeLocalEvent<PressurizableClothingComponent, ComponentRemove>(OnControlRemove);
        SubscribeLocalEvent<PressurizableClothingComponent, GetItemActionsEvent>(OnControlGetItemActions);
        SubscribeLocalEvent<PressurizableClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnEquipmentVerb);
        SubscribeLocalEvent<PressurizableClothingComponent, MapInitEvent>(OnControlMapInit);
        SubscribeLocalEvent<PressurizableClothingComponent, SealClothingDoAfterEvent>(OnPressurizeClothingDoAfter);
        SubscribeLocalEvent<PressurizableClothingComponent, SealClothingEvent>(OnControlPressurizeEvent);
        SubscribeLocalEvent<PressurizableClothingComponent, StartSealingProcessDoAfterEvent>(OnStartPressurizingDoAfter);
        SubscribeLocalEvent<PressurizableClothingComponent, ToggleClothingAttemptEvent>(OnToggleClothingAttempt);
        SubscribeLocalEvent<PressurizableClothingComponent, ToggledBackClothingFullUnequipAndInsertedEvent>(OnBackClothingUnequipped);
        SubscribeLocalEvent<PressurizableClothingComponent, BeingUnequippedAttemptEvent>(OnToggleableUnequipAttemptPressurizeCheck);
        SubscribeLocalEvent<PressurizableClothingComponent, OnToggleableUnequipAttemptEvent>(OnToggleSanityChecker);
        SubscribeLocalEvent<PressurizableClothingComponent, InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>>>(OnRelayedVerbRequest);

        SubscribeLocalEvent<PressurizableClothingComponent, OnAttachedUnequipAttemptEvent>(OnAttachedUnequipAttemptPressurizeCheck);



    }

    #region Events

    /// <summary>
    /// Toggles components on part when suit complete pressurizing process
    /// </summary>
    /// <param name="part"></param>
    /// <param name="args"></param>
    private void OnPartPressurizationComplete(Entity<PressurizableClothingComponent> part, ref ClothingPartPressurizeCompleteEvent args)
    {
        _componentTogglerSystem.ToggleComponent(part, args.IsPressurized);
    }

    /// <summary>
    ///     Toggles components on clothing when suit complete pressurizing process
    /// </summary>
    private void OnControlPressurizationComplete(Entity<PressurizableClothingComponent> control, ref ClothingControlPressurizeCompleteEvent args)
    {
        if (control.Comp.WearerEntity == null)
            return;

        _componentTogglerSystem.ToggleComponent(control, args.IsPressurized);

        if (!control.Comp.UnequipAfterUnpressurize)
            return;
        if (args.IsPressurized)
        {
            control.Comp.UnequipAfterUnpressurize = false;
            return;
        }

        var slot = control.Comp.RequiredControlSlot.ToString().ToLowerInvariant();
        var wearer = control.Comp.WearerEntity;
        _inventorySystem.TryUnequip(wearer.Value, wearer.Value, slot, force:true);
        _inventorySystem.TryEquip(wearer.Value, wearer.Value, control, slot, force: true);
        control.Comp.UnequipAfterUnpressurize = false;
    }

    /// <summary>
    /// Add/Remove wearer on clothing equip/unequip
    /// </summary>
    private void OnControlEquip(Entity<PressurizableClothingComponent> control, ref ClothingGotEquippedEvent args)
    {
        control.Comp.WearerEntity = args.Wearer;
        Dirty(control);
    }

    private void OnControlUnequip(Entity<PressurizableClothingComponent> control, ref ClothingGotUnequippedEvent args)
    {
        control.Comp.WearerEntity = null;
        Dirty(control);
    }

    /// <summary>
    /// Removes pressurize action on component remove
    /// </summary>
    private void OnControlRemove(Entity<PressurizableClothingComponent> control, ref ComponentRemove args)
    {
        var comp = control.Comp;

        _actionsSystem.RemoveAction(comp.PressurizeActionEntity);
    }

    /// <summary>
    /// Ensures pressurize action to wearer when it equip the pressurize control
    /// </summary>
    private static void OnControlGetItemActions(Entity<PressurizableClothingComponent> control, ref GetItemActionsEvent args)
    {
        var (uid, comp) = control;

        if (comp.PressurizeActionEntity == null || args.SlotFlags != comp.RequiredControlSlot)
            return;

        args.AddAction(comp.PressurizeActionEntity.Value);
    }


    private void OnRelayedVerbRequest(Entity<PressurizableClothingComponent> control, ref InventoryRelayedEvent<GetVerbsEvent<EquipmentVerb>> args)
    {
        OnEquipmentVerb(control, ref args.Args);
    }
    /// <summary>
    /// Adds depressurizing verbs to pressurizable clothing allowing other users to depressurize/pressurize clothing via stripping
    /// </summary>
    private void OnEquipmentVerb(Entity<PressurizableClothingComponent> control, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var (uid, comp) = control;
        var user = args.User;

        if (!args.CanComplexInteract)
            return;

        // Prevent Station AI from toggling clothing pressurization
        if (HasComp<StationAiHeldComponent>(user))
            return;
        // Since pressurizing clothing in wearer's container system just won't show verb on args.CanAccess
        if (!_interactionSystem.InRangeUnobstructed(user, uid))
            return;

        if (comp.WearerEntity == null)
            return;

        var verbIcon = comp.IsCurrentlyPressurized ?
            new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")) :
            new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png"));

        var verb = new EquipmentVerb()
        {
            Icon = verbIcon,
            Priority = 5,
            Text = Loc.GetString(comp.VerbText),
        };

        if (args.User == comp.WearerEntity)
        {
            verb.Act = () => TryStartPressurizeToggleProcess(control, user);
        }
        else
        {
            verb.Act = () => StartPressurizeDoAfter(user, control, comp.WearerEntity.Value);
        }

        args.Verbs.Add(verb);
    }
    private void StartPressurizeDoAfter(EntityUid user, Entity<PressurizableClothingComponent> control, EntityUid wearer)
    {
        _popupSystem.PopupClient("You start the suits' pressurizing process", wearer, user);
        var args = new DoAfterArgs(EntityManager, user, control.Comp.NonWearerPressurizingTime, new StartSealingProcessDoAfterEvent(), control, wearer, control)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfterSystem.TryStartDoAfter(args))
        {
            return;
        }

        var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", control));
        _popupSystem.PopupEntity(popup, wearer, wearer, PopupType.Large);

    }

    /// <summary>
    /// Ensure actionEntity on map init
    /// </summary>
    private void OnControlMapInit(Entity<PressurizableClothingComponent> control, ref MapInitEvent args)
    {
        var (uid, comp) = control;
        _actionContainerSystem.EnsureAction(uid, ref comp.PressurizeActionEntity, comp.PressurizeAction);
    }

    private void OnStartPressurizingDoAfter(Entity<PressurizableClothingComponent> control, ref StartSealingProcessDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        var user = args.User;
        // unless you have another way to do doafters inside doafters then yeah
        Timer.Spawn(0, () => TryStartPressurizeToggleProcess(control, user));
    }

    /// <summary>
    /// Trying to start sealing on action. It'll notify wearer if process already started
    /// </summary>
    private void OnControlPressurizeEvent(Entity<PressurizableClothingComponent> control, ref SealClothingEvent args)
    {
        var (uid, comp) = control;

        if (!_actionBlockerSystem.CanInteract(args.Performer, null))
            return;

        if (comp.IsInProcess)
        {
            _popupSystem.PopupClient(comp.IsCurrentlyPressurized
                    ? Loc.GetString(comp.PressurizedInProcessToggleFailPopup)
                    : Loc.GetString(comp.UnpressurizedInProcessToggleFailPopup),
                uid,
                args.Performer);

            _audioSystem.PlayPredicted(comp.FailSound, uid, args.Performer);

            return;
        }

        TryStartPressurizeToggleProcess(control, args.Performer);
    }

    /// <summary>
    /// Toggle seal on one part and starts same process on next part
    /// </summary>
    private void OnPressurizeClothingDoAfter(Entity<PressurizableClothingComponent> control, ref SealClothingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var part = args.Target;

        if (SealPart(part.Value, control, false))
            NextPressurizeProcess(control);
    }

    public bool SealPart(Entity<PressurizableClothingComponent?> ent, Entity<PressurizableClothingComponent> control, bool silent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var (uid, comp) = control;
        var (part, pressurizableComponent) = (ent.Owner, ent.Comp);

        pressurizableComponent.IsPressurized = !comp.IsCurrentlyPressurized;

        Dirty(part, pressurizableComponent);

        if (!silent)
        {
            if (pressurizableComponent.IsPressurized)
                _audioSystem.PlayPvs(pressurizableComponent.PressurizeUpSound, uid);
            else
                _audioSystem.PlayPvs(pressurizableComponent.PressurizeUpSound, uid);
        }

        //_appearanceSystem.SetData(part, SealableClothingVisuals.Sealed, pressurizableComponent.IsPressurized);

        var ev = new ClothingPartPressurizeCompleteEvent(pressurizableComponent.IsPressurized);
        RaiseLocalEvent(part, ref ev);

        return true;
    }

    /// <summary>
    /// Handles clothing toggling if it's pressurized or in pressurizing process
    /// </summary>
    private void OnToggleClothingAttempt(Entity<PressurizableClothingComponent> control, ref ToggleClothingAttemptEvent args)
    {

        var (uid, comp) = control;
        var wearer = control.Comp.WearerEntity;

        if (wearer == null)
        {
            args.Cancel();
            return;
        }

        // Popup if currently sealing
        if (comp.IsInProcess)
        {
            _popupSystem.PopupClient(Loc.GetString(comp.UnpressurizedInProcessToggleFailPopup), uid, args.User);
            _audioSystem.PlayPvs(comp.FailSound, uid);
            args.Cancel();
            return;
        }

        /*
        // Seal after toggling for others
        var toggleStatus = _toggleableSystem.GetAttachedToggleStatus(wearer.Value, control, false);
        if (!comp.IsCurrentlyPressurized && toggleStatus == ToggleableClothingAttachedStatus.NoneToggled && wearer != args.User)
        {
            StartPressurizeDoAfter(args.User, control, wearer.Value);
        }
        */
        if (!comp.IsCurrentlyPressurized)
            return;

        // Popup for attempting to singular unequip with full seal
        if (!args.Multiple)
        {
            _popupSystem.PopupClient(Loc.GetString(comp.CurrentlyPressurizedToggleFailPopup), uid, args.User);
            _audioSystem.PlayPvs(comp.FailSound, uid);
            args.Cancel();
            return;
        }

        // Otherwise its a multiple toggle so we start unseal process
        if (wearer == args.User)
        {
            comp.UnequipAfterUnpressurize = true;
            TryStartPressurizeToggleProcess(control, args.User);
            args.Cancel();
            return;
        }
        comp.UnequipAfterUnpressurize = true;
        StartPressurizeDoAfter(args.User, control, wearer.Value);
        args.Cancel();
    }
    #endregion

    /// <summary>
    ///     Tries to start sealing process
    /// </summary>
    /// <param name="control"></param>
    /// <returns></returns>
    public bool TryStartPressurizeToggleProcess(Entity<PressurizableClothingComponent> control, EntityUid? user = null)
    {
        var (uid, comp) = control;

        // Prevent sealing/unsealing if modsuit don't have wearer or already started process
        if (comp.WearerEntity == null || comp.IsInProcess)
            return false;

        var wearer = comp.WearerEntity;

        var ev = new ClothingPressurizeAttemptEvent(wearer.Value);
        RaiseLocalEvent(control, ev);

        if (ev.Cancelled)
            return false;

        var equippedItems = new List<EntityUid>();

        if (TryComp<ContainerManagerComponent>(wearer.Value, out var containerManager))
        {
            foreach (var container in containerManager.Containers.Values)
            {
                if (container is not ContainerSlot slot || slot.ContainedEntity is not { } itemUid)
                    continue;

                if (!_inventorySystem.TryGetSlot(wearer.Value, slot.ID, out var slotDefinition))
                    continue;

                if ((slotDefinition.SlotFlags & (SlotFlags.OUTERCLOTHING | SlotFlags.HEAD | SlotFlags.FEET | SlotFlags.GLOVES)) == 0)
                    continue;

                equippedItems.Add(itemUid);
            }
        }

        // All parts required to be toggled to perform sealing
        // fix; now able to unseal even if all parts not toggled
        // edge cases where a sealed part may be unequipped and you get stuck with a broke suit
        // PressurizeBreaker along with OnAttachedUnequip in Toggleableclothing should take care if a sealed part unequips
        // but we still let the user manually unseal as a fallback to an impossible situation
        if (!comp.IsCurrentlyPressurized && equippedItems.Count != 4)
        {
            if (user == wearer) // Popup spam prevent
            {
                _popupSystem.PopupClient(Loc.GetString(comp.PressurizeFailedPopup), uid, user);
                Log.Error($"Chegou aqui 1");
                _audioSystem.PlayPredicted(comp.FailSound, user.Value, user);
                return false;
            }
            if (_netManager.IsClient) // Popup spam prevent
                return false;
            _popupSystem.PopupEntity(Loc.GetString(comp.PressurizeFailedPopup), uid);
            Log.Error($"Chegou aqui 2");
            _audioSystem.PlayPvs(comp.FailSound, uid);
            return false;

        }

        // Trying to get all clothing to seal

        foreach (var item in equippedItems)
        {
            if (!HasComp<PressurizableClothingComponent>(item))
            {
                _popupSystem.PopupEntity(Loc.GetString(comp.PressurizeFailedPopup), uid);
                Log.Error($"Chegou aqui 3");
                _audioSystem.PlayPvs(comp.FailSound, uid);

                comp.ProcessQueue.Clear();
                Dirty(control);

                return false;
            }

            comp.ProcessQueue.Enqueue(EntityManager.GetNetEntity(item));
        }

        /*
        var sealeableList = _toggleableSystem.GetAttachedClothingsList(uid);
        if (sealeableList == null || sealeableList.Count == 0)
            return false;

        foreach (var sealeable in sealeableList)
        {
            if (!HasComp<PressurizableClothingComponent>(sealeable))
            {
                _popupSystem.PopupEntity(Loc.GetString(comp.ToggleFailedPopup), uid);
                _audioSystem.PlayPvs(comp.FailSound, uid);

                comp.ProcessQueue.Clear();
                Dirty(control);

                return false;
            }

            comp.ProcessQueue.Enqueue(EntityManager.GetNetEntity(sealeable));
        }
        */
        comp.IsInProcess = true;
        Dirty(control);

        NextPressurizeProcess(control);

        return true;
    }

    /// <summary>
    ///     Iteratively seals/unseals all parts of sealable clothing
    /// </summary>
    /// <param name="control"></param>
    private void NextPressurizeProcess(Entity<PressurizableClothingComponent> control)
    {
        var (uid, comp) = control;
        while (true) // ugly but this used to be recursion so if we're doing this that way use iteration instead.
        {
            if (comp.WearerEntity is not { Valid: true } || !comp.IsInProcess) // dont just fucking assume the fucking entity will never be null what if a dev gibbs you at 3 in the fucking morning.
                return;

            // Finish sealing process
            if (comp.ProcessQueue.Count == 0)
            {
                EndPressurizeProcess(control);
                return;
            }

            var processingPart = EntityManager.GetEntity(comp.ProcessQueue.Dequeue());
            Dirty(control);

            if (!TryComp<PressurizableClothingComponent>(processingPart, out var pressurizableComponent) || !comp.IsInProcess)
            {
                _popupSystem.PopupClient(Loc.GetString(comp.PressurizeFailedPopup), uid, comp.WearerEntity);
                Log.Error($"Chegou aqui 4");
                _audioSystem.PlayPredicted(comp.FailSound, uid, comp.WearerEntity);

                continue;
            }

            // If part is sealed when control trying to seal - it should just skip this part
            if (pressurizableComponent.IsPressurized != comp.IsCurrentlyPressurized)
                continue;

            var doAfterArgs = new DoAfterArgs(EntityManager, uid, pressurizableComponent.PressurizingTime, new SealClothingDoAfterEvent(), uid, target: processingPart, showTo: comp.WearerEntity) { NeedHand = false, RequireCanInteract = false, };

            // Checking for client here to skip first process popup spam that happens. Predicted popups don't work here because doafter starts on sealable control, not on player.
            if (!_doAfterSystem.TryStartDoAfter(doAfterArgs) || _netManager.IsClient)
                return;

            // This is mostly for faster seal unseal times so that the popups dont overlay on eachother
            var xform = Transform(comp.WearerEntity.Value);
            var baseCoords = xform.Coordinates;
            var offsetY = 0.25f * comp.ProcessQueue.Count;
            var popupCoords = baseCoords.Offset(new Vector2(0f, offsetY));

            var popupText = Loc.GetString(
                comp.IsCurrentlyPressurized ? pressurizableComponent.PressurizeDownPopup : pressurizableComponent.PressurizeUpPopup,
                ("partName", Identity.Name(processingPart, EntityManager))
            );
            var type = comp.IsCurrentlyPressurized ?  PopupType.SmallCaution :  PopupType.Small;
            _popupSystem.PopupCoordinates(popupText, popupCoords, comp.WearerEntity.Value, type);

            break;
        }
    }

    /// <summary>
    ///     Finishes sealing process on control
    /// </summary>
    public void EndPressurizeProcess(Entity<PressurizableClothingComponent> control, bool silent = false)
    {
        var (uid, comp) = control;
        // if this system was more foolproof we could swap it around as we did
        // but no so much shit can fuck up
        // so just actually sanity check the parts when the process is done.
        //var attachedParts = _toggleableSystem.GetAttachedClothingsList(uid);
        var slots = new[] { "outerClothing", "head", "feet", "gloves" };
        var equippedItems = new List<EntityUid>();

        foreach (var slot in slots)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, slot, out var item))
                equippedItems.Add(item.Value);
        }
        var allpartsPressurized = true;
        if (equippedItems == null)
            return;

        foreach (var item in equippedItems)
        {
            if (TryComp<PressurizableClothingComponent>(item, out var pSeal) && pSeal.IsPressurized)
                    continue;
            allpartsPressurized = false;
            break;
        }

        comp.IsCurrentlyPressurized = allpartsPressurized;
        // if you gib or remove while sound plays it throws exception so yeah we DO CHECK IF NULL
        if (comp.WearerEntity is not { Valid: true })
            return;

        if (!silent)
        {
            _audioSystem.PlayEntity(comp.IsCurrentlyPressurized ? comp.PressurizeCompleteSound : comp.UnpressurizeCompleteSound,
                comp.WearerEntity.Value,
                uid);
        }

        var ev = new ClothingControlPressurizeCompleteEvent(comp.IsCurrentlyPressurized);
        RaiseLocalEvent(control, ref ev);
        //_appearanceSystem.SetData(uid, SealableClothingVisuals.Sealed, comp.IsCurrentlyPressurized);
        comp.IsInProcess = false;
        Dirty(control);
    }

    private void OnBackClothingUnequipped(Entity<PressurizableClothingComponent> control, ref ToggledBackClothingFullUnequipAndInsertedEvent args)
    {
        var comp = control.Comp;
        var uid = control;
        // Check if it's in the middle of sealing/unsealing
        if (!comp.IsInProcess || comp.UnequipAfterUnpressurize) // yes im looking at UnequipAfterUnpressurize here to make sure we dont seal after untoggling with that bad way of doing it earlier.
            return;

        comp.ProcessQueue.Clear();
        comp.IsInProcess = false;
        // Force all sealed parts to sealed state immediately
        //var attachedParts = _toggleableSystem.GetAttachedClothingsList(control.Owner);
        var slots = new[] { "outerClothing", "head", "feet", "gloves" };
        var equippedItems = new List<EntityUid>();

        foreach (var slot in slots)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, slot, out var item))
                equippedItems.Add(item.Value);
        }
        if (equippedItems == null)
            return;

        foreach (var item in equippedItems)
        {
            if (!TryComp<PressurizableClothingComponent>(item, out var partPressurize))
                continue;
            partPressurize.IsPressurized = true;
            Dirty(item, partPressurize);
            //_appearanceSystem.SetData(part, SealableClothingVisuals.Sealed, true);
        }
        PressurizeBreaker(control);
    }

    private void PressurizeBreaker(EntityUid controlUid)
    {
        if (!TryComp<PressurizableClothingComponent>(controlUid, out var comp))
            return;

        if (comp is { IsInProcess: false, IsCurrentlyPressurized: false })
            return;

        comp.ProcessQueue.Clear();
        comp.IsCurrentlyPressurized = false;
        comp.IsInProcess = false;
        Dirty(controlUid, comp);

        //_appearanceSystem.SetData(controlUid, SealableClothingVisuals.Sealed, false);

        //var attached = _toggleableSystem.GetAttachedClothingsList(controlUid);
        var slots = new[] { "outerClothing", "head", "feet", "gloves" };
        var equippedItems = new List<EntityUid>();

        foreach (var slot in slots)
        {
            if (_inventorySystem.TryGetSlotEntity(controlUid, slot, out var item))
                equippedItems.Add(item.Value);
        }
        if (equippedItems == null || equippedItems.Count == 0)
            return;

        foreach (var item in equippedItems)
        {
            if (!TryComp(item, out PressurizableClothingComponent? partPressurize) || !partPressurize.IsPressurized)
                continue;

            partPressurize.IsPressurized = false;
            Dirty(item, partPressurize);
            //_appearanceSystem.SetData(item, SealableClothingVisuals.Sealed, false);
        }
        if (comp.WearerEntity == null)
            return;
        _popupSystem.PopupEntity(Loc.GetString(comp.PressurizeBrokenPopup), controlUid, comp.WearerEntity.Value, PopupType.LargeCaution);
        _audioSystem.PlayPvs(comp.GenericSuitWarning, controlUid);
    }
    private void OnAttachedUnequipAttemptPressurizeCheck(Entity<PressurizableClothingComponent> attached, ref OnAttachedUnequipAttemptEvent args)
    {

        var toggleableEnt = args.Toggleable;
        // Cancel if the control unit is sealed.
        if (TryComp<PressurizableClothingComponent>(toggleableEnt, out var controlPressurize) && controlPressurize.IsCurrentlyPressurized || controlPressurize!.IsInProcess)
        {
            _popupSystem.PopupClient(Loc.GetString("sealable-clothing-sealed-toggle-fail"), toggleableEnt, args.UnEquipTarget);
            args.Cancel();
            return;
        }

        if (!TryComp<ToggleableClothingComponent>(attached, out var toggleableComp))
            return;

        // Cancel if any parts are sealed.
        foreach (var partUid in toggleableComp.ClothingUids.Keys)
        {
            if (!TryComp<PressurizableClothingComponent>(partUid, out var partPressurize) || !partPressurize.IsPressurized)
                continue;
            _popupSystem.PopupClient(Loc.GetString("sealable-clothing-sealed-toggle-fail"), toggleableEnt, args.UnEquipTarget);
            args.Cancel();
            return;
        }
    }

    private void OnToggleableUnequipAttemptPressurizeCheck(Entity<PressurizableClothingComponent> toggleable, ref BeingUnequippedAttemptEvent args)
    {
        var toggleableEnt = toggleable.Owner;
        // Cancel if the control unit is sealed.
        if (TryComp<PressurizableClothingComponent>(toggleableEnt, out var controlPressurize) && controlPressurize.IsCurrentlyPressurized || controlPressurize!.IsInProcess)
        {
            _popupSystem.PopupClient(Loc.GetString("sealable-clothing-sealed-toggle-fail"), toggleableEnt, args.Unequipee);
            args.Cancel();
            return;
        }

        if (!TryComp<ToggleableClothingComponent>(toggleable, out var toggleableComp))
            return;

        // Cancel if any parts are sealed.
        foreach (var partUid in toggleableComp.ClothingUids.Keys)
        {
            if (!TryComp<PressurizableClothingComponent>(partUid, out var partPressurize) || !partPressurize.IsPressurized)
                continue;
            _popupSystem.PopupClient(Loc.GetString("sealable-clothing-sealed-toggle-fail"), toggleableEnt, args.Unequipee);
            args.Cancel();
            return;
        }
    }
    private void OnToggleSanityChecker(Entity<PressurizableClothingComponent> sealable, ref OnToggleableUnequipAttemptEvent args)
    {
        // AttachedUid = Toggleable Part | args.Toggleable
        // Owner       = Toggled Part    | args.Attached
        if (!TryComp<PressurizableClothingComponent>(args.Attached, out var pressurizableComp))
            return;

        if (!pressurizableComp.IsPressurized)
            return;

        var inSlot = _inventorySystem.TryGetContainingSlot(args.Attached, out var slot);

        if (inSlot && slot != null)
            return;
        PressurizeBreaker(args.Toggleable);
    }
}

[Serializable, NetSerializable]
public sealed partial class PressurizeClothingDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class StartPressurizingProcessDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class PressurizeClothingEvent : InstantActionEvent
{
}

/// <summary>
///     Raises on control when clothing finishes it's sealing or unsealing process
/// </summary>
[ByRefEvent]
public readonly record struct ClothingControlPressurizeCompleteEvent(bool IsPressurized)
{
    public readonly bool IsPressurized = IsPressurized;
}

/// <summary>
///     Raises on part when clothing finishes it's sealing or unsealing process
/// </summary>
[ByRefEvent]
public readonly record struct ClothingPartPressurizeCompleteEvent(bool IsPressurized)
{
    public readonly bool IsPressurized = IsPressurized;
}

public sealed class ClothingPressurizeAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User = user;
}

