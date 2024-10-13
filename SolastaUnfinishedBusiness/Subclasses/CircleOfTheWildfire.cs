﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;
using static ActionDefinitions;
using static SolastaUnfinishedBusiness.Builders.Features.AutoPreparedSpellsGroupBuilder;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Models.SpellsContext;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class CircleOfTheWildfire : AbstractSubclass
{
    private const string Name = "CircleOfTheWildfire";
    private const string SpiritName = "WildfireSpirit";
    private const string ConditionCommandSpirit = $"Condition{Name}Command";

    internal const string PowerSummonCauterizingFlamesName = $"Power{Name}SummonCauterizingFlames";

    private static readonly EffectProxyDefinition EffectProxyCauterizingFlames = EffectProxyDefinitionBuilder
        .Create(EffectProxyDefinitions.ProxyDancingLights, $"Proxy{Name}CauterizingFlames")
        .SetOrUpdateGuiPresentation($"Power{Name}SummonCauterizingFlames", Category.Feature)
        .SetCanMove(false, false)
        .SetAdditionalFeatures()
        .AddToDB();

    private static readonly FeatureDefinitionPower PowerCauterizingFlames =
        FeatureDefinitionPowerBuilder
            .Create($"Power{Name}CauterizingFlames")
            .SetGuiPresentationNoContent(true)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .SetShowCasting(false)
            .AddToDB();

    private static readonly FeatureDefinitionPower PowerCauterizingFlamesDamage = FeatureDefinitionPowerBuilder
        .Create($"Power{Name}CauterizingFlamesDamage")
        .SetGuiPresentation(PowerSummonCauterizingFlamesName, Category.Feature, hidden: true)
        .SetUsesFixed(ActivationTime.NoCost)
        .SetShowCasting(false)
        .SetExplicitAbilityScore(AttributeDefinitions.Wisdom)
        .SetEffectDescription(
            EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                .SetEffectForms(
                    EffectFormBuilder
                        .Create()
                        .SetBonusMode(AddBonusMode.AbilityBonus)
                        .SetDamageForm(DamageTypeFire, 2, DieType.D10)
                        .Build())
                //.SetCasterEffectParameters(HeatMetal)
                .SetImpactEffectParameters(FireBolt)
                .Build())
        .AddToDB();

    private static readonly FeatureDefinitionPower PowerCauterizingFlamesHeal = FeatureDefinitionPowerBuilder
        .Create($"Power{Name}CauterizingFlamesHeal")
        .SetGuiPresentation(PowerSummonCauterizingFlamesName, Category.Feature, hidden: true)
        .SetUsesFixed(ActivationTime.NoCost)
        .SetShowCasting(false)
        .SetExplicitAbilityScore(AttributeDefinitions.Wisdom)
        .SetEffectDescription(
            EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                .SetEffectForms(
                    EffectFormBuilder
                        .Create()
                        .SetBonusMode(AddBonusMode.AbilityBonus)
                        .SetHealingForm(
                            HealingComputation.Dice, 0, DieType.D10, 1, false,
                            HealingCap.MaximumHitPoints)
                        .Build())
                //.SetCasterEffectParameters(HeatMetal)
                .SetImpactEffectParameters(CureWounds)
                .Build())
        .AddToDB();

    public CircleOfTheWildfire()
    {
        //
        // LEVEL 03
        //

        var autoPreparedSpellsWildfire = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation("ExpandedSpells", Category.Feature)
            .SetAutoTag("Circle")
            .SetPreparedSpellGroups(
                BuildSpellGroup(2, BurningHands, CureWounds),
                BuildSpellGroup(3, FlamingSphere, ScorchingRay),
                BuildSpellGroup(5, AshardalonStride, Revivify),
                BuildSpellGroup(7, AuraOfLife, FireShield),
                BuildSpellGroup(9, FlameStrike, MassCureWounds))
            .SetSpellcastingClass(CharacterClassDefinitions.Druid)
            .AddToDB();

        //
        // Summon Spirit
        //

        var powerSpiritTeleportDamage = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}SpiritTeleportDamage")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Dexterity, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetBonusMode(AddBonusMode.Proficiency)
                            .SetDamageForm(DamageTypeFire, 1, DieType.D6)
                            .Build())
                    .SetParticleEffectParameters(PowerDomainElementalFireBurst)
                    .Build())
            .AddToDB();

        var powerSpiritTeleport = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}SpiritTeleport")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerSpiritTeleport", Resources.PowerSpiritTeleport, 256, 128))
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 3, TargetType.Position)
                    .InviteOptionalAlly()
                    .SetSavingThrowData(true, AttributeDefinitions.Wisdom, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetMotionForm(MotionForm.MotionType.TeleportToDestination, 1)
                            .Build())
                    .SetCasterEffectParameters(HeatMetal)
                    .Build())
            .AddCustomSubFeatures(new CustomBehaviorSpiritTeleport(powerSpiritTeleportDamage))
            .AddToDB();

        powerSpiritTeleport.EffectDescription.EffectParticleParameters.targetParticleReference = new AssetReference();

        var actionAffinitySpirit =
            FeatureDefinitionActionAffinityBuilder
                .Create($"ActionAffinity{Name}Spirit")
                .SetGuiPresentationNoContent(true)
                .SetForbiddenActions(
                    Id.AttackMain, Id.AttackOff, Id.AttackFree, Id.AttackReadied, Id.AttackOpportunity, Id.Ready,
                    Id.PowerMain, Id.PowerBonus, Id.PowerNoCost, Id.PowerReaction, Id.SpendPower,
                    Id.Shove, Id.ShoveBonus, Id.ShoveFree)
                .AddCustomSubFeatures(new SummonerHasConditionOrKOd(), ForceInitiativeToSummoner.Mark)
                .AddToDB();

        var toHit = FeatureDefinitionAttackModifierBuilder
            .Create($"AttackModifier{Name}AttackRoll")
            .SetGuiPresentation("Feedback/&SpiritBonusTitle", Gui.NoLocalization)
            .SetAttackRollModifier(1, AttackModifierMethod.SourceConditionAmount)
            .AddToDB();

        var toDamage = FeatureDefinitionAttackModifierBuilder
            .Create($"AttackModifier{Name}DamageRoll")
            .SetGuiPresentation("Feedback/&SpiritBonusTitle", Gui.NoLocalization)
            .SetDamageRollModifier(1, AttackModifierMethod.SourceConditionAmount)
            .AddToDB();

        var hpBonus = FeatureDefinitionAttributeModifierBuilder
            .Create($"AttributeModifier{Name}HitPoints")
            .SetGuiPresentation("Feedback/&SpiritBonusTitle", Gui.NoLocalization)
            .SetModifier(FeatureDefinitionAttributeModifier.AttributeModifierOperation.AddConditionAmount,
                AttributeDefinitions.HitPoints)
            .AddToDB();

        var summoningAffinitySpirit = FeatureDefinitionSummoningAffinityBuilder
            .Create($"SummoningAffinity{Name}Spirit")
            .SetGuiPresentationNoContent(true)
            .SetRequiredMonsterTag(SpiritName)
            .SetAddedConditions(
                ConditionDefinitionBuilder
                    .Create($"Condition{Name}CopyProficiencyBonus")
                    .SetGuiPresentationNoContent(true)
                    .SetSilent(Silent.WhenAddedOrRemoved)
                    .SetAmountOrigin(ExtraOriginOfAmount.SourceCopyAttributeFromSummoner,
                        AttributeDefinitions.ProficiencyBonus)
                    .AddToDB(),
                ConditionDefinitionBuilder
                    .Create($"Condition{Name}SpiritAttackRoll")
                    .SetGuiPresentation("Feedback/&SpiritBonusTitle", Gui.EmptyContent)
                    .SetPossessive()
                    .SetSilent(Silent.WhenAddedOrRemoved)
                    .SetAmountOrigin(ConditionDefinition.OriginOfAmount.SourceSpellAttack)
                    .SetFeatures(toHit)
                    .AddToDB(),
                ConditionDefinitionBuilder
                    .Create($"Condition{Name}SpiritDamageRoll")
                    .SetGuiPresentation("Feedback/&SpiritBonusTitle", Gui.EmptyContent)
                    .SetPossessive()
                    .SetSilent(Silent.WhenAddedOrRemoved)
                    .SetAmountOrigin(ExtraOriginOfAmount.SourceProficiencyBonus)
                    .SetFeatures(toDamage)
                    .AddToDB(),
                ConditionDefinitionBuilder
                    .Create($"Condition{Name}SpiritHitPoints")
                    .SetGuiPresentation("Feedback/&SpiritBonusTitle", Gui.EmptyContent)
                    .SetPossessive()
                    .SetSilent(Silent.WhenAddedOrRemoved)
                    .SetAmountOrigin(ExtraOriginOfAmount.SourceClassLevel, DruidClass)
                    .SetFeatures(hpBonus, hpBonus, hpBonus, hpBonus, hpBonus)
                    .AddToDB())
            .AddToDB();

        var attackWildfireSpirit = MonsterAttackDefinitionBuilder
            .Create("AttackWildfireSpirit")
            .SetGuiPresentation(Category.Monster, DatabaseHelper.ActionDefinitions.SpiritRage)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(EffectFormBuilder.DamageForm(DamageTypeFire, 1, DieType.D6))
                    .SetCasterEffectParameters(new AssetReference())
                    .SetImpactEffectParameters(FireBolt)
                    .Build())
            .AddToDB();

        attackWildfireSpirit.actionType = ActionType.Main;
        attackWildfireSpirit.proximity = AttackProximity.Range;
        attackWildfireSpirit.maxRange = 12;
        attackWildfireSpirit.closeRange = 12;

        var monsterDefinitionSpirit = MonsterDefinitionBuilder
            .Create(MonsterDefinitions.Fire_Elemental, "WildfireSpirit")
            .SetOrUpdateGuiPresentation(Category.Monster)
            .SetSizeDefinition(CharacterSizeDefinitions.Small)
            .SetMonsterPresentation(
                MonsterPresentationBuilder
                    .Create()
                    .SetAllPrefab(MonsterDefinitions.Fire_Elemental.MonsterPresentation)
                    .SetPhantom()
                    .SetModelScale(0.3f)
                    .SetHasMonsterPortraitBackground(true)
                    .SetCanGeneratePortrait(true)
                    .Build())
            .SetCreatureTags(SpiritName)
            .SetStandardHitPoints(5)
            .SetHeight(2)
            .NoExperienceGain()
            .SetArmorClass(13)
            .SetChallengeRating(0)
            .SetHitDice(DieType.D8, 1)
            .SetAbilityScores(10, 14, 14, 13, 15, 11)
            .SetDefaultFaction(FactionDefinitions.Party)
            .SetBestiaryEntry(BestiaryDefinitions.BestiaryEntry.None)
            .SetFullyControlledWhenAllied(true)
            .SetDungeonMakerPresence(MonsterDefinition.DungeonMaker.None)
            .SetAttackIterations(new MonsterAttackIteration(attackWildfireSpirit, 1))
            .SetFeatures(
                actionAffinitySpirit,
                powerSpiritTeleport,
                FeatureDefinitionMoveModes.MoveModeMove6,
                FeatureDefinitionMoveModes.MoveModeFly6,
                FeatureDefinitionDamageAffinitys.DamageAffinityFireImmunity,
                FeatureDefinitionConditionAffinitys.ConditionAffinityCharmImmunity,
                FeatureDefinitionConditionAffinitys.ConditionAffinityFrightenedImmunity,
                FeatureDefinitionConditionAffinitys.ConditionAffinityProneImmunity,
                FeatureDefinitionConditionAffinitys.ConditionAffinityRestrainedmmunity,
                FeatureDefinitionSenses.SenseDarkvision)
            .AddToDB();

        // Command Spirit

        var conditionCommandSpirit = ConditionDefinitionBuilder
            .Create(ConditionCommandSpirit)
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddToDB();

        var powerCommandSpirit = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}CommandSpirit")
            .SetGuiPresentation(Category.Feature, Command)
            .SetUsesFixed(ActivationTime.BonusAction)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionCommandSpirit, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .Build())
            .AddCustomSubFeatures(ValidatorsValidatePowerUse.InCombat,
                new ValidatorsValidatePowerUse(x => HasSpirit(x.Guid)))
            .AddToDB();

        powerCommandSpirit.AddCustomSubFeatures(
            new CharacterBeforeTurnEndListenerCommandSpirit(conditionCommandSpirit, powerCommandSpirit));

        // Summon Spirit Damage

        var powerSummonSpiritDamage = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}SummonSpiritDamage")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Dexterity, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetDamageForm(DamageTypeFire, 2, DieType.D6)
                            .Build())
                    .SetImpactEffectParameters(FireBolt)
                    .Build())
            .AddToDB();

        // Summon Spirit

        var powerSummonSpirit = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"PowerSharedPool{Name}SummonSpirit")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerSummonSpirit", Resources.PowerSummonSpirit, 256, 128))
            .SetSharedPool(ActivationTime.Action, PowerDruidWildShape)
            .SetUniqueInstance()
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.HalfClassLevelHours)
                    .SetTargetingData(Side.Ally, RangeType.Distance, 6, TargetType.Position)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetSummonCreatureForm(1, monsterDefinitionSpirit.Name)
                            .Build())
                    .SetParticleEffectParameters(PowerDruidWildShape)
                    .Build())
            .AddCustomSubFeatures(
                RestrictEffectToNotTerminateWhileUnconscious.Marker,
                new PowerOrSpellFinishedByMeSummonSpirit(powerSummonSpiritDamage))
            .AddToDB();

        var featureSetSummonSpirit = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}SummonSpirit")
            .SetGuiPresentation($"PowerSharedPool{Name}SummonSpirit", Category.Feature)
            .SetFeatureSet(
                summoningAffinitySpirit,
                powerCommandSpirit,
                powerSummonSpirit,
                powerSummonSpiritDamage)
            .AddToDB();

        //
        // LEVEL 06 - Enhanced Bond
        //

        var featureEnhancedBond = FeatureDefinitionBuilder
            .Create($"Feature{Name}EnhancedBond")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureEnhancedBond.AddCustomSubFeatures(
            new MagicEffectBeforeHitConfirmedOnEnemyEnhancedBond(featureEnhancedBond));

        //
        // LEVEL 10 - Cauterizing Flames
        //

        EffectProxyCauterizingFlames.actionId = Id.NoAction;

        var powerSummonCauterizingFlames = FeatureDefinitionPowerBuilder
            .Create(PowerSummonCauterizingFlamesName)
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.Position)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetSummonEffectProxyForm(EffectProxyCauterizingFlames)
                            .Build())
                    .Build())
            .AddToDB();

        powerSummonCauterizingFlames.AddCustomSubFeatures(
            new OnReducedToZeroHpByMeOrAllySummonCauterizingFlames(powerSummonCauterizingFlames));

        var featureSetCauterizingFlames = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}CauterizingFlames")
            .SetGuiPresentation($"Power{Name}SummonCauterizingFlames", Category.Feature)
            .SetFeatureSet(
                powerSummonCauterizingFlames,
                PowerCauterizingFlames,
                PowerCauterizingFlamesDamage,
                PowerCauterizingFlamesHeal)
            .AddToDB();

        //
        // LEVEL 14 - Blazing Revival
        //

        var powerBlazingRevival = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}BlazingRevival")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.LongRest)
            .SetShowCasting(false)
            .AddToDB();

        powerBlazingRevival.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new OnReducedToZeroHpByEnemyBlazingRevival(powerBlazingRevival));

        //
        // MAIN
        //

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite(Name, Resources.CircleOfTheWildfire, 256))
            .AddFeaturesAtLevel(2, autoPreparedSpellsWildfire, featureSetSummonSpirit)
            .AddFeaturesAtLevel(6, featureEnhancedBond)
            .AddFeaturesAtLevel(10, featureSetCauterizingFlames)
            .AddFeaturesAtLevel(14, powerBlazingRevival)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Druid;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceDruidCircle;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    private static GameLocationCharacter GetMySpirit(ulong guid)
    {
        var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
        var mySpirit = locationCharacterService.GuestCharacters
            .FirstOrDefault(x =>
                x.RulesetCharacter is RulesetCharacterMonster rulesetCharacterMonster &&
                rulesetCharacterMonster.MonsterDefinition.CreatureTags.Contains(SpiritName) &&
                rulesetCharacterMonster.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagConjure, ConditionConjuredCreature, out var conjured) &&
                conjured.SourceGuid == guid);

        return mySpirit;
    }

    private static bool HasSpirit(ulong guid)
    {
        return GetMySpirit(guid) != null;
    }

    internal static IEnumerator HandleCauterizingFlamesBehavior(GameLocationCharacter character)
    {
        var battleManager = ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;

        if (!battleManager ||
            (character.RulesetCharacter is RulesetCharacterEffectProxy proxy &&
             proxy.EffectProxyDefinition == EffectProxyCauterizingFlames))
        {
            yield break;
        }

        var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
        var cauterizingFlamesProxies = locationCharacterService.AllProxyCharacters
            .Where(u =>
                character.LocationPosition == u.LocationPosition &&
                u.RulesetActor is RulesetCharacterEffectProxy rulesetCharacterEffectProxy &&
                rulesetCharacterEffectProxy.EffectProxyDefinition == EffectProxyCauterizingFlames)
            .ToArray(); // avoid changing enumerator

        foreach (var cauterizingFlamesProxy in cauterizingFlamesProxies)
        {
            var rulesetProxy = cauterizingFlamesProxy.RulesetActor as RulesetCharacterEffectProxy;
            var rulesetSource = EffectHelpers.GetCharacterByGuid(rulesetProxy!.ControllerGuid);
            var usablePower = PowerProvider.Get(PowerCauterizingFlames, rulesetSource);
            var source = GameLocationCharacter.GetFromActor(rulesetSource);

            if (source == null ||
                !source.CanReact() ||
                rulesetSource.GetRemainingUsesOfPower(usablePower) == 0)
            {
                continue;
            }

            yield return source.MyReactToUsePower(
                Id.PowerReaction,
                usablePower,
                [character],
                character,
                character.Side == Side.Enemy ? "CauterizingFlamesDamage" : "CauterizingFlamesHeal",
                reactionValidated: ReactionValidated);

            yield break;

            void ReactionValidated()
            {
                var powerToTerminate = rulesetSource.PowersUsedByMe.FirstOrDefault(x =>
                    x.Guid == rulesetProxy.EffectGuid);

                if (powerToTerminate != null)
                {
                    rulesetSource.TerminatePower(powerToTerminate);
                }

                usablePower = PowerProvider.Get(
                    character.Side == Side.Enemy
                        ? PowerCauterizingFlamesDamage
                        : PowerCauterizingFlamesHeal,
                    rulesetSource);

                // cauterizing flames damage or heal are use at will power
                source.MyExecuteActionSpendPower(usablePower, character);
            }
        }
    }

    // defines which actions will be available on spirit
    // also marks summoner when spirit dies this round
    private sealed class SummonerHasConditionOrKOd : IValidateDefinitionApplication, ICharacterTurnStartListener
    {
        public void OnCharacterTurnStarted(GameLocationCharacter locationCharacter)
        {
            // if commanded allow anything
            if (IsCommanded(locationCharacter.RulesetCharacter))
            {
                return;
            }

            // if not commanded it cannot move
            locationCharacter.usedTacticalMoves = locationCharacter.MaxTacticalMoves;

            // or use powers so force the dodge action
            locationCharacter.MyExecuteActionDodge();
        }

        public bool IsValid(BaseDefinition definition, RulesetCharacter character)
        {
            //Apply limits if not commanded
            return !IsCommanded(character);
        }

        private static bool IsCommanded(RulesetCharacter character)
        {
            //can act freely outside of battle
            if (Gui.Battle == null)
            {
                return true;
            }

            var summoner = character.GetMySummoner()?.RulesetCharacter;

            //shouldn't happen, but consider being commanded in this case
            if (summoner == null)
            {
                return true;
            }

            //can act if summoner is KO
            return summoner.IsUnconscious ||
                   //can act if summoner commanded
                   summoner.HasConditionOfType(ConditionCommandSpirit);
        }
    }

    //
    // Command Spirit
    //

    private sealed class CharacterBeforeTurnEndListenerCommandSpirit(
        ConditionDefinition conditionEldritchCannonCommand,
        FeatureDefinitionPower power) : ICharacterBeforeTurnEndListener
    {
        public void OnCharacterBeforeTurnEnded(GameLocationCharacter locationCharacter)
        {
            var status = locationCharacter.GetActionStatus(Id.PowerBonus, ActionScope.Battle);

            if (status != ActionStatus.Available ||
                !HasSpirit(locationCharacter.Guid))
            {
                return;
            }

            var rulesetCharacter = locationCharacter.RulesetCharacter;

            rulesetCharacter.LogCharacterUsedPower(power);
            rulesetCharacter.InflictCondition(
                conditionEldritchCannonCommand.Name,
                DurationType.Round,
                1,
                TurnOccurenceType.StartOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetCharacter.guid,
                rulesetCharacter.CurrentFaction.Name,
                1,
                conditionEldritchCannonCommand.Name,
                0,
                0,
                0);
        }
    }

    //
    // Summon Spirit
    //

    private sealed class PowerOrSpellFinishedByMeSummonSpirit(FeatureDefinitionPower powerSummonSpiritDamage)
        : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
            var attacker = action.ActingCharacter;
            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerSummonSpiritDamage, rulesetAttacker);
            var spirit = GetMySpirit(attacker.Guid);
            var contenders =
                Gui.Battle?.AllContenders ??
                locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters);
            var targets = contenders
                .Where(x =>
                    attacker != x &&
                    spirit != x &&
                    spirit.IsWithinRange(x, 2))
                .ToArray();

            // spirit summon damage is a use at will power
            attacker.MyExecuteActionSpendPower(usablePower, targets);

            yield break;
        }
    }

    //
    // Spirit Teleport
    //

    private sealed class CustomBehaviorSpiritTeleport(FeatureDefinitionPower powerExplode)
        : IModifyTeleportEffectBehavior, IFilterTargetingCharacter,
            IPowerOrSpellInitiatedByMe, IPowerOrSpellFinishedByMe
    {
        private readonly List<GameLocationCharacter> _targets = [];

        public bool EnforceFullSelection => false;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            if (target.RulesetCharacter == null)
            {
                return false;
            }

            var isValid =
                target.RulesetCharacter is not RulesetCharacterEffectProxy &&
                __instance.ActionParams.ActingCharacter.IsWithinRange(target, 1);

            if (!isValid)
            {
                __instance.actionModifier.FailureFlags.Add("Failure/&MustBeWithin5ft");
            }

            return isValid;
        }

        public bool AllyOnly => true;

        public bool TeleportSelf => true;

        public int MaxTargets => 8;

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var attacker = action.ActingCharacter;
            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerExplode, rulesetAttacker);

            // spirit teleport explode is a use at will power
            attacker.MyExecuteActionSpendPower(usablePower, [.. _targets]);

            yield break;
        }

        public IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var attacker = action.ActingCharacter;
            var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
            var contenders =
                Gui.Battle?.AllContenders ??
                locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters);

            _targets.SetRange(contenders
                .Where(x =>
                    x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                    x != attacker &&
                    !action.ActionParams.TargetCharacters.Contains(x) &&
                    attacker.IsWithinRange(x, 1)));

            yield break;
        }
    }

    //
    // Enhanced Bond
    //

    private sealed class MagicEffectBeforeHitConfirmedOnEnemyEnhancedBond(FeatureDefinition featureEnhancedBond)
        : IMagicEffectInitiatedByMe, IMagicEffectBeforeHitConfirmedOnEnemy, IMagicEffectFinishedByMe
    {
        public IEnumerator OnMagicEffectBeforeHitConfirmedOnEnemy(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetEffect rulesetEffect,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            if (!HasSpirit(attacker.Guid) || !firstTarget)
            {
                yield break;
            }

            var fireDamageForm = actualEffectForms.FirstOrDefault(x =>
                x.FormType == EffectForm.EffectFormType.Damage &&
                x.DamageForm.DamageType == DamageTypeFire);

            if (fireDamageForm == null)
            {
                yield break;
            }

            var index = actualEffectForms.IndexOf(fireDamageForm);
            var newDamageForm = EffectFormBuilder
                .Create()
                .HasSavingThrow(EffectSavingThrowType.Negates)
                .SetDamageForm(DamageTypeFire, 1, DieType.D12)
                .Build();

            newDamageForm.DamageForm.IgnoreCriticalDoubleDice = true;
            newDamageForm.DamageForm.IgnoreSpellAdvancementDamageDice = true;

            attacker.RulesetCharacter.LogCharacterUsedFeature(featureEnhancedBond);
            actualEffectForms.Insert(index + 1, newDamageForm);
        }

        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            if (action is not CharacterActionCastSpell actionCastSpell ||
                actionCastSpell.Countered ||
                actionCastSpell.ExecutionFailed)
            {
                yield break;
            }

            foreach (var rulesetCharacter in targets
                         .Where(x => x.RulesetCharacter != null)
                         .Select(x => x.RulesetCharacter))
            {
                rulesetCharacter.HealingReceived -= HealingReceived;
            }
        }

        public IEnumerator OnMagicEffectInitiatedByMe(
            CharacterAction action,
            RulesetEffect activeEffect,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            if (action is not CharacterActionCastSpell)
            {
                yield break;
            }

            var effectForms = activeEffect.EffectDescription.EffectForms;
            var hasHealingForm = effectForms.Any(x => x.FormType == EffectForm.EffectFormType.Healing);

            if (HasSpirit(attacker.Guid) && hasHealingForm)
            {
                attacker.RulesetCharacter.LogCharacterUsedFeature(featureEnhancedBond);
            }

            foreach (var rulesetCharacter in targets
                         .Where(x => x.RulesetCharacter != null)
                         .Select(x => x.RulesetCharacter))
            {
                rulesetCharacter.HealingReceived += HealingReceived;
            }
        }

        private static void HealingReceived(
            RulesetCharacter character,
            int healing,
            ulong sourceGuid,
            HealingCap healingCaps,
            IHealingModificationProvider healingModificationProvider)
        {
            if (!HasSpirit(sourceGuid))
            {
                return;
            }

            character.HealingReceived -= HealingReceived;

            var healingRoll = character.RollDie(
                DieType.D8, RollContext.HealValueRoll, false, AdvantageType.None, out _, out _);

            character.ReceiveHealing(healingRoll, true, sourceGuid);
        }
    }

    //
    // Cauterizing Flames
    //

    private sealed class OnReducedToZeroHpByMeOrAllySummonCauterizingFlames(
        FeatureDefinitionPower powerSummonCauterizingFlames) : IOnReducedToZeroHpByMeOrAlly
    {
        public IEnumerator HandleReducedToZeroHpByMeOrAlly(
            GameLocationCharacter attacker,
            GameLocationCharacter downedCreature,
            GameLocationCharacter ally,
            RulesetAttackMode attackMode,
            RulesetEffect activeEffect)
        {
            var rulesetAlly = ally.RulesetCharacter;

            if (downedCreature.RulesetCharacter is not RulesetCharacterMonster rulesetCharacterMonster ||
                rulesetAlly.GetRemainingPowerUses(PowerCauterizingFlames) == 0 ||
                (rulesetCharacterMonster.MonsterDefinition.SizeDefinition != CharacterSizeDefinitions.Small &&
                 rulesetCharacterMonster.MonsterDefinition.SizeDefinition != CharacterSizeDefinitions.Medium))
            {
                yield break;
            }

            var spirit = GetMySpirit(ally.Guid);

            if (!ally.IsWithinRange(downedCreature, 6) &&
                (spirit == null || !spirit.IsWithinRange(downedCreature, 6)))
            {
                yield break;
            }

            var implementationService = ServiceRepository.GetService<IRulesetImplementationService>();
            var usablePower = PowerProvider.Get(powerSummonCauterizingFlames, rulesetAlly);
            var actionParams = new CharacterActionParams(ally, Id.PowerNoCost)
            {
                RulesetEffect = implementationService
                    .InstantiateEffectPower(rulesetAlly, usablePower, false),
                UsablePower = usablePower,
                Positions = { downedCreature.LocationPosition }
            };

            ServiceRepository.GetService<IGameLocationActionService>().ExecuteAction(actionParams, null, true);
        }
    }

    //
    // Blazing Revival
    //

    private sealed class OnReducedToZeroHpByEnemyBlazingRevival(FeatureDefinitionPower powerBlazingRevival)
        : IOnReducedToZeroHpByEnemy
    {
        public IEnumerator HandleReducedToZeroHpByEnemy(
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RulesetEffect activeEffect)
        {
            var rulesetCharacter = defender.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerBlazingRevival, rulesetCharacter);

            if (rulesetCharacter.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            yield return defender.MyReactToSpendPower(
                usablePower,
                attacker,
                "BlazingRevival",
                reactionValidated: ReactionValidated);

            yield break;

            void ReactionValidated()
            {
                var hitPoints = rulesetCharacter.TryGetAttributeValue(AttributeDefinitions.HitPoints) / 2;

                defender.MyExecuteActionStabilizeAndStandUp(hitPoints);
            }
        }
    }
}
