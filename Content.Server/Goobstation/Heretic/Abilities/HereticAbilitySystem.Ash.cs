using Content.Server.Atmos.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Heretic;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly SplittingFireballSystem _splitball = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobstate = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly StaminaSystem _stam = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private void SubscribeAsh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticAshenShift>(OnJaunt);
        SubscribeLocalEvent<HereticComponent, PolymorphRevertEvent>(OnJauntEnd);

        SubscribeLocalEvent<HereticComponent, EventHereticVolcanoBlast>(OnVolcano);
        SubscribeLocalEvent<HereticComponent, EventHereticNightwatcherRebirth>(OnNWRebirth);
        SubscribeLocalEvent<HereticComponent, EventHereticFlames>(OnFlames);
        SubscribeLocalEvent<HereticComponent, EventHereticCascade>(OnCascade);
    }

    private void OnJaunt(Entity<HereticComponent> ent, ref EventHereticAshenShift args)
    {
        if (!TryUseAbility(ent, args))
            return;

        Spawn("PolymorphAshJauntAnimation", Transform(ent).Coordinates);
        _poly.PolymorphEntity(ent, "AshJaunt");

        args.Handled = true;
    }
    private void OnJauntEnd(Entity<HereticComponent> ent, ref PolymorphRevertEvent args)
    {
        Spawn("PolymorphAshJauntEndAnimation", Transform(ent).Coordinates);
    }

    private void OnVolcano(Entity<HereticComponent> ent, ref EventHereticVolcanoBlast args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (!_splitball.Spawn(ent))
            return;

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
    private void OnNWRebirth(Entity<HereticComponent> ent, ref EventHereticNightwatcherRebirth args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var lookup = _lookup.GetEntitiesInRange(ent, 5f);

        foreach (var look in lookup)
        {
            if (HasComp<HereticComponent>(look)
            || HasComp<GhoulComponent>(look))
                continue;

            if (TryComp<FlammableComponent>(look, out var flam))
            {
                if (flam.OnFire && TryComp<DamageableComponent>(ent, out var dmgc))
                {
                    // heals everything by 10 for each burning target
                    _stam.TryTakeStamina(ent, -10);
                    var dmgdict = dmgc.Damage.DamageDict;
                    foreach (var key in dmgdict.Keys)
                        dmgdict[key] = -10f;

                    var dmgspec = new DamageSpecifier() { DamageDict = dmgdict };
                    _dmg.TryChangeDamage(ent, dmgspec, true, false, dmgc);
                }

                if (!flam.OnFire)
                    _flammable.AdjustFireStacks(look, 5, flam, true);

                if (TryComp<MobStateComponent>(look, out var mobstat))
                    if (mobstat.CurrentState == MobState.Critical)
                        _mobstate.ChangeMobState(look, MobState.Dead, mobstat);
            }
        }

        args.Handled = true;
    }
    private void OnFlames(Entity<HereticComponent> ent, ref EventHereticFlames args)
    {
        if (!TryUseAbility(ent, args))
            return;

        EnsureComp<HereticFlamesComponent>(ent);

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
    private void OnCascade(Entity<HereticComponent> ent, ref EventHereticCascade args)
    {
        if (!TryUseAbility(ent, args) || !Transform(ent).GridUid.HasValue)
            return;

        // yeah. it just generates a ton of plasma which just burns.
        // lame, but we don't have anything fire related atm, so, it works.
        var tilepos = _xform.GetGridOrMapTilePosition(ent, Transform(ent));
        var enumerator = _atmos.GetAdjacentTileMixtures(Transform(ent).GridUid!.Value, tilepos, false, false);
        while (enumerator.MoveNext(out var mix))
        {
            mix.AdjustMoles(Gas.Plasma, 50f);
            mix.Temperature = Atmospherics.T0C + 125f;
        }

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
}
