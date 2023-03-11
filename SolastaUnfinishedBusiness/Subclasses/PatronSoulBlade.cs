﻿using System.Linq;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomBehaviors;
using SolastaUnfinishedBusiness.CustomInterfaces;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Subclasses.CommonBuilders;

namespace SolastaUnfinishedBusiness.Subclasses;

internal sealed class PatronSoulBlade : AbstractSubclass
{
    internal PatronSoulBlade()
    {
        //
        // LEVEL 01
        //

        var spellListSoulBlade = SpellListDefinitionBuilder
            .Create(SpellListDefinitions.SpellListWizard, "SpellListSoulBlade")
            .SetGuiPresentationNoContent(true)
            .ClearSpells()
            .SetSpellsAtLevel(1, Shield, SpellsContext.WrathfulSmite)
            .SetSpellsAtLevel(2, Blur, BrandingSmite)
            .SetSpellsAtLevel(3, SpellsContext.BlindingSmite, SpellsContext.ElementalWeapon)
            .SetSpellsAtLevel(4, PhantasmalKiller, SpellsContext.StaggeringSmite)
            .SetSpellsAtLevel(5, SpellsContext.BanishingSmite, ConeOfCold)
            .FinalizeSpells(true, 9)
            .AddToDB();

        var magicAffinitySoulBladeExpandedSpells = FeatureDefinitionMagicAffinityBuilder
            .Create("MagicAffinitySoulBladeExpandedSpells")
            .SetOrUpdateGuiPresentation("MagicAffinityPatronExpandedSpells", Category.Feature)
            .SetExtendedSpellList(spellListSoulBlade)
            .AddToDB();

        // Empower Weapon

        var powerSoulBladeEmpowerWeapon = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeEmpowerWeapon")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerSoulEmpower", Resources.PowerSoulEmpower, 256, 128))
            .SetUniqueInstance()
            .SetCustomSubFeatures(
                DoNotTerminateWhileUnconscious.Marker,
                ExtraCarefulTrackedItem.Marker,
                SkipEffectRemovalOnLocationChange.Always,
                new CustomItemFilter(CanWeaponBeEmpowered))
            .SetUsesFixed(ActivationTime.Action, RechargeRate.ShortRest)
            .SetExplicitAbilityScore(AttributeDefinitions.Charisma)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetDurationData(DurationType.Permanent)
                .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Item,
                    itemSelectionType: ActionDefinitions.ItemSelectionType.Carried)
                .SetEffectForms(EffectFormBuilder.Create()
                    .SetItemPropertyForm(
                        ItemPropertyUsage.Unlimited,
                        1, new FeatureUnlockByLevel(
                            FeatureDefinitionAttackModifierBuilder
                                .Create("AttackModifierSoulBladeEmpowerWeapon")
                                .SetGuiPresentation(Category.Feature, PowerOathOfDevotionSacredWeapon)
                                .SetCustomSubFeatures(ExtraCarefulTrackedItem.Marker)
                                .SetMagicalWeapon()
                                .SetAbilityScoreReplacement(AbilityScoreReplacement.SpellcastingAbility)
                                .AddToDB(),
                            0))
                    .Build())
                .Build())
            .AddToDB();

        // Common Hex Feature

        var additionalDamageHex = FeatureDefinitionAdditionalDamageBuilder
            .Create("AdditionalDamageSoulBladeHex")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag("Hex")
            .SetDamageValueDetermination(AdditionalDamageValueDetermination.ProficiencyBonus)
            .AddToDB();

        var attributeModifierHex = FeatureDefinitionAttributeModifierBuilder
            .Create("AttributeModifierSoulBladeHex")
            .SetGuiPresentationNoContent(true)
            .SetModifier(FeatureDefinitionAttributeModifier.AttributeModifierOperation.Additive,
                AttributeDefinitions.CriticalThreshold, -1)
            .AddToDB();

        var conditionHexAttacker = ConditionDefinitionBuilder
            .Create("ConditionSoulBladeHexAttacker")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetSpecialDuration(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
            .SetFeatures(additionalDamageHex, attributeModifierHex)
            .AddToDB();

        var conditionHexDefender = ConditionDefinitionBuilder
            .Create("ConditionSoulBladeHexDefender")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionBranded)
            .SetConditionType(ConditionType.Detrimental)
            .AddToDB();

        var featureHex = FeatureDefinitionBuilder
            .Create("FeatureSoulBladeHex")
            .SetGuiPresentationNoContent(true)
            .SetCustomSubFeatures(
                new OnComputeAttackModifierHex(conditionHexAttacker, conditionHexDefender))
            .AddToDB();

        var spriteSoulHex = Sprites.GetSprite("PowerSoulHex", Resources.PowerSoulHex, 256, 128);

        // Soul Hex - Basic

        var powerBasicHex = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeBasicHex")
            .SetGuiPresentation("PowerSoulBladeHex", Category.Feature, spriteSoulHex)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ShortRest)
            .SetShowCasting(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionHexDefender, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .Build())
            .AddToDB();

        conditionHexDefender.SetCustomSubFeatures(new NotifyConditionRemovalHex(powerBasicHex, conditionHexDefender));

        //
        // LEVEL 06
        //

        // Soul Hex - Intermediate

        var powerIntermediateHex = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeIntermediateHex")
            .SetGuiPresentation("PowerSoulBladeHex", Category.Feature, spriteSoulHex)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ShortRest, 1, 2)
            .SetShowCasting(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionHexDefender, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .Build())
            .SetOverriddenPower(powerBasicHex)
            .AddToDB();

        // Summon Pact Weapon

        var powerSoulBladeSummonPactWeapon = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeSummonPactWeapon")
            .SetGuiPresentation(Category.Feature, SpiritualWeapon)
            .SetUniqueInstance()
            .SetCustomSubFeatures(SkipEffectRemovalOnLocationChange.Always)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ShortRest)
            .SetExplicitAbilityScore(AttributeDefinitions.Charisma)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(SpiritualWeapon.EffectDescription)
                    .Build())
            .AddToDB();

        powerSoulBladeSummonPactWeapon.EffectDescription.savingThrowDifficultyAbility = AttributeDefinitions.Charisma;

        var featureSetLevel06 = FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetSoulBladeLevel06")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                powerIntermediateHex,
                powerSoulBladeSummonPactWeapon)
            .AddToDB();

        //
        // LEVEL 10
        //

        // Soul Hex - Advanced

        var powerAdvancedHex = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeAdvancedHex")
            .SetGuiPresentation("PowerSoulBladeHex", Category.Feature, spriteSoulHex)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ShortRest, 1, 3)
            .SetShowCasting(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionHexDefender, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .Build())
            .SetOverriddenPower(powerBasicHex)
            .AddToDB();

        // Soul Shield

        var effectDescriptionSoulShield = EffectDescriptionBuilder
            .Create()
            .SetDurationData(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
            .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
            .SetEffectForms(
                EffectFormBuilder
                    .Create()
                    .SetConditionForm(
                        ConditionDefinitionBuilder
                            .Create("ConditionSoulBladeSoulShield")
                            .SetFeatures(
                                FeatureDefinitionAttributeModifierBuilder
                                    .Create("AttributeModifierSoulBladeSoulShield")
                                    .SetGuiPresentation("PowerSoulBladeSoulShield", Category.Feature)
                                    .SetModifier(
                                        FeatureDefinitionAttributeModifier.AttributeModifierOperation.Additive,
                                        AttributeDefinitions.ArmorClass,
                                        5)
                                    .AddToDB())
                            .AddToDB(),
                        ConditionForm.ConditionOperation.Add)
                    .Build())
            .Build();

        var powerSoulBladeSoulShieldBasic = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeSoulShieldBasic")
            .SetGuiPresentation("PowerSoulBladeSoulShield", Category.Feature, PowerFighterSecondWind)
            .SetUsesFixed(ActivationTime.Reaction, RechargeRate.ShortRest)
            .SetEffectDescription(effectDescriptionSoulShield)
            .SetReactionContext(ReactionTriggerContext.HitByMelee)
            .AddToDB();

        var featureSetLevel10 = FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetSoulBladeLevel10")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                powerAdvancedHex,
                powerSoulBladeSoulShieldBasic)
            .AddToDB();

        // Master Hex

        var powerMasterHex = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeMasterHex")
            .SetGuiPresentation("PowerSoulBladeHex", Category.Feature, spriteSoulHex)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ShortRest, 1, 4)
            .SetShowCasting(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionHexDefender, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .Build())
            .SetOverriddenPower(powerIntermediateHex)
            .AddToDB();

        // Master Soul Shield

        var powerSoulBladeSoulShieldAdvanced = FeatureDefinitionPowerBuilder
            .Create("PowerSoulBladeSoulShieldAdvanced")
            .SetGuiPresentation("PowerSoulBladeSoulShield", Category.Feature, PowerFighterSecondWind)
            .SetUsesFixed(ActivationTime.Reaction, RechargeRate.ShortRest, 2)
            .SetEffectDescription(effectDescriptionSoulShield)
            .SetReactionContext(ReactionTriggerContext.HitByMelee)
            .SetOverriddenPower(powerSoulBladeSoulShieldBasic)
            .AddToDB();

        var featureSetLevel14 = FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetSoulBladeLevel14")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                powerMasterHex,
                powerSoulBladeSoulShieldAdvanced)
            .AddToDB();

        Subclass = CharacterSubclassDefinitionBuilder
            .Create("PatronSoulBlade")
            .SetGuiPresentation(Category.Subclass,
                Sprites.GetSprite("PatronSoulBlade", Resources.PatronSoulBlade, 256))
            .AddFeaturesAtLevel(1,
                FeatureSetCasterFightingProficiency,
                magicAffinitySoulBladeExpandedSpells,
                featureHex,
                powerBasicHex,
                powerSoulBladeEmpowerWeapon)
            .AddFeaturesAtLevel(6,
                featureSetLevel06)
            .AddFeaturesAtLevel(10,
                featureSetLevel10)
            .AddFeaturesAtLevel(14,
                featureSetLevel14)
            .AddToDB();
    }

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceWarlockOtherworldlyPatrons;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    private static bool CanWeaponBeEmpowered(RulesetCharacter character, RulesetItem item)
    {
        var definition = item.ItemDefinition;

        if (!definition.IsWeapon || !character.IsProficientWithItem(definition))
        {
            return false;
        }

        if (character is RulesetCharacterHero hero &&
            hero.ActiveFeatures.Any(p => p.Value.Contains(FeatureDefinitionFeatureSets.FeatureSetPactBlade)))
        {
            return true;
        }

        return !definition.WeaponDescription.WeaponTags.Contains(TagsDefinitions.WeaponTagTwoHanded);
    }

    private sealed class OnComputeAttackModifierHex : IOnComputeAttackModifier
    {
        private readonly ConditionDefinition _conditionHexAttacker;
        private readonly ConditionDefinition _conditionHexDefender;

        public OnComputeAttackModifierHex(
            ConditionDefinition conditionHexAttacker,
            ConditionDefinition conditionHexDefender)
        {
            _conditionHexAttacker = conditionHexAttacker;
            _conditionHexDefender = conditionHexDefender;
        }

        public void ComputeAttackModifier(
            RulesetCharacter myself,
            RulesetCharacter defender,
            BattleDefinitions.AttackProximity attackProximity,
            RulesetAttackMode attackMode,
            ref ActionModifier attackModifier)
        {
            var battle = Gui.Battle;

            if (battle == null)
            {
                return;
            }

            if (defender.HasAnyConditionOfType(_conditionHexDefender.Name))
            {
                var rulesetCondition = RulesetCondition.CreateActiveCondition(
                    myself.guid,
                    _conditionHexAttacker,
                    DurationType.Round,
                    0,
                    TurnOccurenceType.StartOfTurn,
                    myself.guid,
                    myself.CurrentFaction.Name);

                myself.AddConditionOfCategory(AttributeDefinitions.TagCombat, rulesetCondition);
            }
            else
            {
                var rulesetCondition =
                    myself.AllConditions.FirstOrDefault(x => x.ConditionDefinition == _conditionHexAttacker);

                if (rulesetCondition != null)
                {
                    myself.RemoveConditionOfCategory(AttributeDefinitions.TagCombat, rulesetCondition);
                }
            }
        }
    }

    private sealed class NotifyConditionRemovalHex : INotifyConditionRemoval
    {
        private readonly ConditionDefinition _conditionHexDefender;
        private readonly FeatureDefinition _featureUsed;

        public NotifyConditionRemovalHex(FeatureDefinition featureUsed, ConditionDefinition conditionHexDefender)
        {
            _featureUsed = featureUsed;
            _conditionHexDefender = conditionHexDefender;
        }

        public void AfterConditionRemoved(RulesetActor removedFrom, RulesetCondition rulesetCondition)
        {
            // empty
        }

        public void BeforeDyingWithCondition(RulesetActor rulesetActor, RulesetCondition rulesetCondition)
        {
            if (rulesetCondition.ConditionDefinition != _conditionHexDefender)
            {
                return;
            }

            var sourceGuid = rulesetCondition.SourceGuid;

            if (RulesetEntity.TryGetEntity<RulesetCharacter>(sourceGuid, out var rulesetCharacter))
            {
                ReceiveHealing(rulesetCharacter);
            }
        }

        private void ReceiveHealing(RulesetCharacter rulesetCharacter)
        {
            var characterLevel = rulesetCharacter.GetAttribute(AttributeDefinitions.CharacterLevel).CurrentValue;
            var charisma = rulesetCharacter.GetAttribute(AttributeDefinitions.Charisma).CurrentValue;
            var charismaModifier = AttributeDefinitions.ComputeAbilityScoreModifier(charisma);
            var healingReceived = characterLevel + charismaModifier;

            GameConsoleHelper.LogCharacterUsedFeature(rulesetCharacter, _featureUsed, indent: true);

            if (rulesetCharacter.MissingHitPoints > 0)
            {
                rulesetCharacter.ReceiveHealing(healingReceived, true, rulesetCharacter.Guid);
            }
            else if (rulesetCharacter.TemporaryHitPoints <= healingReceived)
            {
                rulesetCharacter.ReceiveTemporaryHitPoints(healingReceived, DurationType.Minute, 1,
                    TurnOccurenceType.EndOfTurn, rulesetCharacter.Guid);
            }
        }
    }
}
