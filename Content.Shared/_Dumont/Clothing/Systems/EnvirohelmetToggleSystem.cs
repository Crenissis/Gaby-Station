using Content.Shared.Actions;
using Content.Shared._Dumont.Clothing.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Dumont.Clothing.Systems;

public sealed partial class EnvirohelmetToggleSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = null!;
    [Dependency] private SharedItemSystem _item = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedAppearanceSystem _appearance = null!;
    [Dependency] private ClothingSystem _clothing = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnvirohelmetToggleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, ToggleEnvirohelmetEvent>(OnToggle);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, GetEquipmentVisualsEvent>(OnGetVisuals, after: [typeof(ClothingSystem)]);
    }

    private void OnMapInit(Entity<EnvirohelmetToggleComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);

        ApplyComponentMode(ent, ent.Comp.IsActive);
        UpdateAppearance(ent);
    }

    private void OnShutdown(Entity<EnvirohelmetToggleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnGetActions(Entity<EnvirohelmetToggleComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.ActionEntity != null)
            args.AddAction(ent.Comp.ActionEntity.Value);
    }

    private void OnToggle(Entity<EnvirohelmetToggleComponent> ent, ref ToggleEnvirohelmetEvent args)
    {
        args.Handled = true;

        ent.Comp.IsActive = !ent.Comp.IsActive;
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);

        ApplyComponentMode(ent, ent.Comp.IsActive);
        UpdateAppearance(ent);
        _item.VisualsChanged(ent.Owner);
    }

    private void ApplyComponentMode(Entity<EnvirohelmetToggleComponent> ent, bool active)
    {
        if (active)
        {
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.ClosedComponents);
            EntityManager.AddComponents(ent.Owner, ent.Comp.OpenComponents);
        }
        else
        {
            EntityManager.RemoveComponents(ent.Owner, ent.Comp.OpenComponents);
            EntityManager.AddComponents(ent.Owner, ent.Comp.ClosedComponents);
        }
    }

    private void OnGetVisuals(Entity<EnvirohelmetToggleComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        var state = ent.Comp.IsActive ? ent.Comp.StateOpen : ent.Comp.StateClosed;

        if (string.IsNullOrEmpty(state))
            return;

        Log.Debug($"Componente está {ent.Comp.IsActive} no OnGetVisuals.");

        if (ent.Comp.IsActive)
        {
            var baseState = "open-equipped-" + args.Slot;

            Log.Debug($"baseState: {baseState} no OnGetVisuals.");
            var layer = new PrototypeLayerData()
            {
                State = state,
                Visible = true
            };

            Log.Debug($"layer: {layer} no OnGetVisuals.");

            var insertIndex = args.Layers.Count;
            for (var i = 0; i < args.Layers.Count; i++)
            {
                if (args.Layers[i].Item1 == "open-light")
                {
                    insertIndex = i;
                    break;
                }
            }

            Log.Debug($"VisualLayer: {ent.Comp.OpenVisualLayer} no OnGetVisuals.");
            args.Layers.Insert(insertIndex, (ent.Comp.OpenVisualLayer, layer));

            if (TryComp<ClothingComponent>(ent, out var clothing))
                Dirty(ent, clothing);
        }
        else
        {
            var baseState = "equipped-" + args.Slot;

            Log.Debug($"baseState: {baseState} no OnGetVisuals.");

            foreach (var (key, layerData) in args.Layers)
            {
                if (key.StartsWith(args.Slot) && !string.IsNullOrEmpty(layerData.State))
                {
                    if (layerData.State.StartsWith(baseState))
                    {
                        var suffix = layerData.State.Substring(baseState.Length);

                        Log.Debug($"sufixo: {suffix} no OnGetVisuals.");

                        if (suffix.Contains("-unshaded"))
                            continue;

                        state += suffix;
                        Log.Debug($"state final: {state} no OnGetVisuals.");
                        break;
                    }
                }
            }
            var layer = new PrototypeLayerData()
            {
                State = state,
                Visible = true
            };

            Log.Debug($"layer: {layer.State} no OnGetVisuals.");

            var insertIndex = args.Layers.Count;
            for (var i = 0; i < args.Layers.Count; i++)
            {
                if (args.Layers[i].Item1 == "light")
                {
                    insertIndex = i;
                    break;
                }
            }

            if (TryComp<ClothingComponent>(ent, out var clothing))
            {
                Dirty(ent, clothing);
            }

            Log.Debug($"VisualLayer: {ent.Comp.ClosedVisualLayer} no OnGetVisuals.");
            args.Layers.Insert(insertIndex, (ent.Comp.ClosedVisualLayer, layer));
        }
    }

    private void UpdateAppearance(Entity<EnvirohelmetToggleComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance) && ent.Comp.IsActive)
        {
            Log.Debug($"Componente está ativo e usando o IsOpen no SetData.");
            _appearance.SetData(ent, EnvirohelmetVisuals.IsOpen, ent.Comp.IsActive, appearance);
        }
        else
        {
            Log.Debug($"Componente está desativado e usando o IsClosed no SetData.");
            _appearance.SetData(ent, EnvirohelmetVisuals.IsClosed, !ent.Comp.IsActive, appearance);
        }
    }
}
