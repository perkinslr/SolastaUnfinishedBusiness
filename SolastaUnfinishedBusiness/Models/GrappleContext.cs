﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Feats;
using SolastaUnfinishedBusiness.FightingStyles;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Validators;
using TA;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.ActionDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionActionAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionMovementAffinitys;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Behaviors.Specific.DistanceCalculation;

namespace SolastaUnfinishedBusiness.Models;

internal static class GrappleContext
{
    private const string Grapple = "Grapple";
    private const string DisableGrapple = "DisableGrapple";

    private const string ConditionGrappleTargetName = $"Condition{Grapple}Target";
    private const string ConditionGrappleSourceName = $"Condition{Grapple}Source";

    internal const string ConditionGrappleSourceWithGrapplerName = $"Condition{Grapple}SourceWithGrappler";
    internal const string ConditionGrappleSourceWithGrapplerLargerName = $"Condition{Grapple}SourceWithGrapplerLarger";

    private static readonly FeatureDefinitionPower PowerGrapple = FeatureDefinitionPowerBuilder
        .Create($"Power{Grapple}")
        .SetGuiPresentation($"Action{Grapple}", Category.Action, hidden: true)
        .SetUsesFixed(ActivationTime.NoCost)
        .SetShowCasting(false)
        .SetEffectDescription(
            EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                .SetImpactEffectParameters(Knock)
                .Build())
        .AddCustomSubFeatures(new CustomBehaviorGrapple())
        .AddToDB();

    private static readonly FeatureDefinitionPower PowerDisableGrapple = FeatureDefinitionPowerBuilder
        .Create($"Power{DisableGrapple}")
        .SetGuiPresentation($"Action{DisableGrapple}", Category.Action, hidden: true)
        .SetShowCasting(false)
        .SetUsesFixed(ActivationTime.NoCost)
        .SetEffectDescription(
            EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                .SetImpactEffectParameters(Slow)
                .Build())
        .AddCustomSubFeatures(new PowerOrSpellFinishedByMeDisableGrapple())
        .AddToDB();

    private static readonly ActionDefinition ActionGrapple = ActionDefinitionBuilder
        .Create($"Action{Grapple}")
        .SetGuiPresentation(Category.Action, AttackFree)
        .OverrideClassName("UsePower")
        .SetActivatedPower(PowerGrapple)
        .SetActionId(ExtraActionId.Grapple)
        .SetActionScope(ActionDefinitions.ActionScope.All)
        .SetActionType(ActionDefinitions.ActionType.NoCost)
        .SetFormType(ActionDefinitions.ActionFormType.Large)
        .AddToDB();

    private static readonly ActionDefinition ActionDisableGrapple = ActionDefinitionBuilder
        .Create($"Action{DisableGrapple}")
        .SetGuiPresentation(Category.Action, AttackFree)
        .OverrideClassName("UsePower")
        .SetActivatedPower(PowerDisableGrapple)
        .SetActionId(ExtraActionId.DisableGrapple)
        .SetActionScope(ActionDefinitions.ActionScope.All)
        .SetActionType(ActionDefinitions.ActionType.NoCost)
        .SetFormType(ActionDefinitions.ActionFormType.Large)
        .AddToDB();

    internal static void LateLoad()
    {
        const SituationalContext TARGET_HAS_CONDITION_FROM_SOURCE =
            (SituationalContext)ExtraSituationalContext.IsConditionSource;

        var battlePackage =
            AiContext.BuildDecisionPackageBreakFree(ConditionGrappleTargetName, AiContext.RandomType.RandomMediumLow);

        var conditionGrappleTarget = ConditionDefinitionBuilder
            .Create(ConditionGrappleTargetName)
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionHindered)
            .SetConditionType(ConditionType.Detrimental)
            .SetFixedAmount((int)AiContext.BreakFreeType.DoStrengthOrDexterityContestCheckAgainstStrengthAthletics)
            .SetBrain(battlePackage, true)
            .SetFeatures(
                ActionAffinityGrappled,
                //prevent grappled target from falling while grappled
                FeatureDefinitionMoveModeBuilder.Create($"{ConditionGrappleTargetName}Flying")
                    .SetGuiPresentation(Category.Feature)
                    .SetMode(MoveMode.Fly, 0)
                    .AddToDB(),
                ActionAffinityConditionRestrained,
                MovementAffinityConditionRestrained)
            .AddCustomSubFeatures(new OnConditionAddedOrRemovedConditionGrappleTarget())
            .SetConditionParticleReference(ConditionDefinitions.ConditionRestrained)
            .AddToDB();

        var combatAffinityGrappleSource = FeatureDefinitionCombatAffinityBuilder
            .Create("CombatAffinityGrappleSource")
            .SetGuiPresentationNoContent(true)
            .SetSituationalContext(TARGET_HAS_CONDITION_FROM_SOURCE, conditionGrappleTarget)
            .SetAttackOfOpportunityImmunity(true)
            .AddToDB();

        var combatAffinityGrappleSourceWithGrappler = FeatureDefinitionCombatAffinityBuilder
            .Create("CombatAffinityGrappleSourceWithGrappler")
            .SetGuiPresentationNoContent(true)
            .SetSituationalContext(TARGET_HAS_CONDITION_FROM_SOURCE, conditionGrappleTarget)
            .SetAttackOfOpportunityImmunity(true)
            .SetMyAttackAdvantage(AdvantageType.Advantage)
            .AddToDB();

        _ = ConditionDefinitionBuilder
            .Create(ConditionGrappleSourceName)
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionEncumbered)
            .SetConditionType(ConditionType.Neutral)
            .SetFeatures(
                MovementAffinityNoClimb,
                MovementAffinityNoSpecialMoves,
                combatAffinityGrappleSource,
                MovementAffinityConditionSlowed)
            .AddCustomSubFeatures(CustomBehaviorConditionGrappleSource.Marker)
            .SetCancellingConditions(
                DatabaseRepository.GetDatabase<ConditionDefinition>().Where(x =>
                    x.IsSubtypeOf(ConditionIncapacitated) || x.IsSubtypeOf(ConditionFlying)).ToArray())
            .SetConditionParticleReference(ConditionDefinitions.ConditionSlowed)
            .AddToDB();

        _ = ConditionDefinitionBuilder
            .Create(ConditionGrappleSourceWithGrapplerName)
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionReckless)
            .SetConditionType(ConditionType.Neutral)
            .SetFeatures(
                MovementAffinityNoClimb,
                MovementAffinityNoSpecialMoves,
                combatAffinityGrappleSourceWithGrappler)
            .AddCustomSubFeatures(CustomBehaviorConditionGrappleSource.Marker)
            .SetCancellingConditions(
                DatabaseRepository.GetDatabase<ConditionDefinition>().Where(x =>
                    x.IsSubtypeOf(ConditionIncapacitated) || x.IsSubtypeOf(ConditionFlying)).ToArray())
            .SetConditionParticleReference(ConditionDefinitions.ConditionSlowed)
            .AddToDB();

        _ = ConditionDefinitionBuilder
            .Create(ConditionGrappleSourceWithGrapplerLargerName)
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionSlowed)
            .SetConditionType(ConditionType.Neutral)
            .SetFeatures(
                MovementAffinityNoClimb,
                MovementAffinityNoSpecialMoves,
                combatAffinityGrappleSourceWithGrappler,
                MovementAffinityConditionSlowed)
            .AddCustomSubFeatures(CustomBehaviorConditionGrappleSource.Marker)
            .SetCancellingConditions(
                DatabaseRepository.GetDatabase<ConditionDefinition>().Where(x =>
                    x.IsSubtypeOf(ConditionIncapacitated) || x.IsSubtypeOf(ConditionFlying)).ToArray())
            .SetConditionParticleReference(ConditionDefinitions.ConditionSlowed)
            .AddToDB();

        // Brawler feat
        _ = ActionDefinitionBuilder
            .Create($"Action{Grapple}Bonus")
            .SetGuiPresentation($"Action{Grapple}", Category.Action, AttackFree)
            .OverrideClassName("UsePower")
            .SetActivatedPower(PowerGrapple)
            .SetActionId(ExtraActionId.GrappleBonus)
            .SetActionScope(ActionDefinitions.ActionScope.All)
            .SetActionType(ActionDefinitions.ActionType.Bonus)
            .SetFormType(ActionDefinitions.ActionFormType.Large)
            .RequiresAuthorization()
            .AddToDB();
    }

    internal static void SwitchGrappleAction()
    {
        ActionGrapple.formType = Main.Settings.AddGrappleActionToAllRaces
            ? ActionDefinitions.ActionFormType.Large
            : ActionDefinitions.ActionFormType.Invisible;

        ActionDisableGrapple.formType = Main.Settings.AddGrappleActionToAllRaces
            ? ActionDefinitions.ActionFormType.Large
            : ActionDefinitions.ActionFormType.Invisible;
    }

    internal static void ValidateGrappleAfterForcedMove(GameLocationCharacter target)
    {
        var rulesetTarget = target.RulesetCharacter;

        if (rulesetTarget is not { IsDeadOrDying: false })
        {
            return;
        }

        if (GetGrappledActor(rulesetTarget, out var rulesetGrappled, out var activeCondition))
        {
            var grappled = GameLocationCharacter.GetFromActor(rulesetGrappled);
            var allowedRange = GetUnarmedReachRange(target);

            if (!target.IsWithinRange(grappled, allowedRange))
            {
                rulesetGrappled.RemoveCondition(activeCondition);
            }
        }

        // ReSharper disable once InvertIf
        if (rulesetTarget.TryGetConditionOfCategoryAndType(
                AttributeDefinitions.TagEffect, ConditionGrappleTargetName, out var activeConditionTarget))
        {
            var rulesetGrappler = EffectHelpers.GetCharacterByGuid(activeConditionTarget.SourceGuid);
            var grappler = GameLocationCharacter.GetFromActor(rulesetGrappler);
            var allowedRange = GetUnarmedReachRange(grappler);

            if (!target.IsWithinRange(grappler, allowedRange))
            {
                rulesetTarget.RemoveCondition(activeConditionTarget);
            }
        }
    }

    internal static void ValidateActionAvailability(
        GameLocationCharacter __instance,
        ref ActionDefinitions.ActionStatus __result,
        ActionDefinitions.Id actionId)
    {
        if (!Main.Settings.AddGrappleActionToAllRaces)
        {
            return;
        }

        var rulesetCharacter = __instance.RulesetCharacter;
        var hasGrappleSource = HasGrappleSource(rulesetCharacter);
        var extraActionId = (ExtraActionId)actionId;

        if ((extraActionId is ExtraActionId.Grapple or ExtraActionId.GrappleBonus &&
             (hasGrappleSource ||
              !ValidatorsCharacter.HasFreeHand(rulesetCharacter) ||
              !ValidatorsCharacter.HasMainAttackAvailable(rulesetCharacter))) ||
            (extraActionId == ExtraActionId.DisableGrapple && !hasGrappleSource))
        {
            __result = ActionDefinitions.ActionStatus.Unavailable;
        }
    }

    internal static void ValidateIfCastingValid(
        RulesetCharacter caster, SpellDefinition spell, ref bool result, ref string failure,
        SpellValidationType validationType)
    {
        if (!result ||
            !HasGrappleSource(caster) ||
            (validationType == SpellValidationType.Somatic && !spell.SomaticComponent) ||
            (validationType == SpellValidationType.Material &&
             spell.MaterialComponentType == MaterialComponentType.None) ||
            ServiceRepository.GetService<IGameSettingsService>().MaterialComponent ==
            SettingDefinitions.SomaticComponentDisabled)
        {
            return;
        }

        if (ValidatorsCharacter.HasBothHandsFree(caster) ||
            caster.HasSubFeatureOfType<OtherFeats.WarCasterMarker>())
        {
            return;
        }

        result = false;
        failure = validationType == SpellValidationType.Somatic
            ? "Failure/&FailureFlagSomaticComponentHandsFull"
            : "Failure/&FailureFlagMaterialComponentHandsFull";
    }

    private static bool HasGrappleSource(RulesetCharacter rulesetCharacter)
    {
        return rulesetCharacter.HasConditionOfCategoryAndType(
                   AttributeDefinitions.TagEffect, ConditionGrappleSourceName) ||
               rulesetCharacter.HasConditionOfCategoryAndType(
                   AttributeDefinitions.TagEffect, ConditionGrappleSourceWithGrapplerName) ||
               rulesetCharacter.HasConditionOfCategoryAndType(
                   AttributeDefinitions.TagEffect, ConditionGrappleSourceWithGrapplerLargerName);
    }

    private static bool GetGrappledActor(
        RulesetCharacter rulesetSource,
        out RulesetCharacter rulesetTarget,
        out RulesetCondition rulesetTargetCondition)
    {
        var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
        var contenders =
            Gui.Battle?.AllContenders ??
            locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters);

        RulesetCondition foundCondition = null;

        rulesetTarget = contenders.FirstOrDefault(x =>
            x.RulesetCharacter.TryGetConditionOfCategoryAndType(
                AttributeDefinitions.TagEffect, ConditionGrappleTargetName, out foundCondition) &&
            foundCondition.SourceGuid == rulesetSource.Guid)?.RulesetCharacter;

        var found = rulesetTarget != null;

        rulesetTargetCondition = found ? foundCondition : null;

        return found;
    }

    private static int GetUnarmedReachRange(GameLocationCharacter character)
    {
        var hero = character.RulesetCharacter.GetOriginalHero();

        if (hero != null &&
            // only Astral Reach grants reach on unarmed
            hero.GetFeaturesByType<FeatureDefinition>().Any(x => x.Name == AstralReach.AstralReachFeatureName))
        {
            return 2;
        }

        return 1;
    }

    internal enum SpellValidationType
    {
        Somatic,
        Material
    }

    private sealed class CustomBehaviorGrapple : IFilterTargetingCharacter, IPowerOrSpellFinishedByMe
    {
        public bool EnforceFullSelection => false;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            var rulesetTarget = target.RulesetCharacter;

            if (rulesetTarget == null)
            {
                return false;
            }

            var actingCharacter = __instance.ActionParams.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;
            var isValid =
                rulesetCharacter.SizeDefinition.WieldingSize - rulesetTarget.SizeDefinition.WieldingSize >= -1;

            if (!isValid)
            {
                __instance.actionModifier.FailureFlags.Add("Failure/&TargetMustBeNoMoreThanOneSizeLarger");

                return false;
            }

            var allowedRange = GetUnarmedReachRange(actingCharacter);

            isValid = actingCharacter.IsWithinRange(target, allowedRange);

            if (isValid)
            {
                return true;
            }

            __instance.actionModifier.FailureFlags.Add("Failure/&FailureFlagNoReachForTargetDescription");

            return false;
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var attacker = action.ActingCharacter;
            var defender = action.ActionParams.TargetCharacters[0];
            var abilityCheckData = new AbilityCheckData();

            attacker.BurnOneMainAttack();

            var success = true;

            if (!Main.Settings.AlliesDoNotResolveRollsWhenGrappled || defender.IsOppositeSide(attacker.Side))
            {
                yield return TryAlterOutcomeAttributeCheck.ResolveRolls(
                    attacker, defender, ActionDefinitions.Id.NoAction, abilityCheckData);

                success =
                    abilityCheckData.AbilityCheckRollOutcome is RollOutcome.Success or RollOutcome.CriticalSuccess;
            }

            if (!success)
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var rulesetDefender = defender.RulesetCharacter;

            // should only be grappled by one grappler at a time, last wins
            var conditionsToRemove = rulesetDefender.ConditionsByCategory
                .SelectMany(x => x.Value)
                .Where(x => x.ConditionDefinition.Name == ConditionGrappleTargetName)
                .ToArray();

            foreach (var condition in conditionsToRemove)
            {
                rulesetDefender.RemoveCondition(condition);
            }

            // apply new grappler condition
            var sourceConditionName = ConditionGrappleSourceName;

            // factor in Grappler feat
            OtherFeats.MaybeChangeGrapplerConditionForGrappleFeatBehavior(
                rulesetAttacker, rulesetDefender, ref sourceConditionName);

            rulesetAttacker.InflictCondition(
                sourceConditionName,
                DurationType.UntilAnyRest,
                0,
                TurnOccurenceType.EndOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetAttacker.guid,
                rulesetAttacker.CurrentFaction.Name,
                1,
                sourceConditionName,
                0,
                0,
                0);

            // apply new grappled condition
            rulesetDefender.InflictCondition(
                ConditionGrappleTargetName,
                DurationType.UntilAnyRest,
                0,
                TurnOccurenceType.EndOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetAttacker.guid,
                rulesetAttacker.CurrentFaction.Name,
                1,
                ConditionGrappleTargetName,
                40,
                0,
                0);
        }
    }

    private sealed class PowerOrSpellFinishedByMeDisableGrapple : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var rulesetAttacker = action.ActingCharacter.RulesetCharacter;

            if (GetGrappledActor(rulesetAttacker, out var rulesetDefender, out var activeConditionTarget))
            {
                // this will also take care of removing the source condition at OnConditionAddedOrRemovedConditionGrappleTarget
                rulesetDefender.RemoveCondition(activeConditionTarget);
            }

            yield break;
        }
    }

    private sealed class OnConditionAddedOrRemovedConditionGrappleTarget : IOnConditionAddedOrRemoved
    {
        private static readonly string[] PossibleConditionsToRemove =
        [
            ConditionGrappleSourceName,
            ConditionGrappleSourceWithGrapplerName,
            ConditionGrappleSourceWithGrapplerLargerName
        ];

        public void OnConditionAdded(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            // empty
        }

        // should remove source tracker condition as well
        public void OnConditionRemoved(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            var rulesetSource = EffectHelpers.GetCharacterByGuid(rulesetCondition.SourceGuid);

            foreach (var conditionName in PossibleConditionsToRemove)
            {
                if (rulesetSource.TryGetConditionOfCategoryAndType(
                        AttributeDefinitions.TagEffect, conditionName, out var activeConditionSource) &&
                    activeConditionSource.SourceGuid == rulesetCondition.SourceGuid)
                {
                    rulesetSource.RemoveCondition(activeConditionSource);
                }
            }

            var glc = GameLocationCharacter.GetFromActor(target);
            
            glc.StopMoving(LocationDefinitions.Orientation.Down, CharacterAction.InterruptionType.ForcedMovement);
        }
    }

    private sealed class CustomBehaviorConditionGrappleSource
        : IModifyWeaponAttackMode, IMoveStepStarted, IOnItemEquipped, IPhysicalAttackInitiatedByMe,
            IOnConditionAddedOrRemoved, IOnReducedToZeroHpByEnemy
    {
        internal static readonly CustomBehaviorConditionGrappleSource Marker = new();

        // should not use a versatile weapon in two-handed mode
        public void ModifyAttackMode(RulesetCharacter character, RulesetAttackMode attackMode)
        {
            attackMode.UseVersatileDamage = false;
        }

        // should drag target whenever move considering all movement blockers as well
        public void MoveStepStarted(GameLocationCharacter mover, int3 source, int3 destination)
        {
            var rulesetMover = mover.RulesetCharacter;

            if (!GetGrappledActor(rulesetMover, out var rulesetTarget, out var activeCondition))
            {
                return;
            }

            var pathfindingService = ServiceRepository.GetService<IGameLocationPathfindingService>();
            var target = GameLocationCharacter.GetFromActor(rulesetTarget);
            var targetPosition = target.LocationPosition;
            var targetDestinationPosition = source;

            pathfindingService
                .ComputeValidDestinationsAsync(target, target.LocationPosition, 1, 0, true, true)
                .ExecuteUntilDone();

            var validPositions = pathfindingService.ValidDestinations
                .Where(x => x.moveMode is MoveMode.Walk or MoveMode.Fly)
                .Select(x => x.position)
                .ToArray();

            bool canTeleport;

            // handle better movement on larger enemies by applying same movement directions as source
            if (target.SizeParameters.maxExtent.x > 0 ||
                target.SizeParameters.maxExtent.y > 0 ||
                target.SizeParameters.maxExtent.z > 0)
            {
                targetDestinationPosition = targetPosition + destination - source;

                canTeleport = validPositions.Contains(targetDestinationPosition);
            }
            // for an unknown reason ComputeValidDestinationsAsync isn't adding the mover one even when flagging ignoreOccupants
            // fix it here to avoid breaking one cell targets
            else
            {
                canTeleport = true;
            }

            if (canTeleport)
            {
                target.StartTeleportTo(targetDestinationPosition, mover.Orientation, false);
                target.Pushed = true;
                target.FinishMoveTo(targetDestinationPosition, mover.Orientation);
                target.StopMoving(mover.Orientation);
                target.Pushed = false;

                var isLastStep = GetDistanceFromCharacter(mover, mover.DestinationPosition) <= 1;

                if (isLastStep)
                {
                    EjectCharactersInArea(mover, target);
                }
            }
            else
            {
                rulesetTarget.RemoveCondition(activeCondition);
                EjectCharactersInArea(mover, target);
            }
        }

        public void OnConditionAdded(RulesetCharacter source, RulesetCondition rulesetCondition)
        {
            // empty
        }

        // remove grappled on target if source becomes incapacitated
        public void OnConditionRemoved(RulesetCharacter source, RulesetCondition rulesetCondition)
        {
            if (GetGrappledActor(source, out var target, out var activeConditionTarget))
            {
                target.RemoveCondition(activeConditionTarget);
            }
        }

        // should lose grapple if no free hand anymore
        public void OnItemEquipped(RulesetCharacterHero hero)
        {
            if (!ValidatorsCharacter.HasFreeHand(hero) &&
                GetGrappledActor(hero, out var rulesetTarget, out var activeCondition))
            {
                rulesetTarget.RemoveCondition(activeCondition);
            }
        }

        // remove grappled on target if source becomes unconscious
        public IEnumerator HandleReducedToZeroHpByEnemy(
            GameLocationCharacter attacker,
            GameLocationCharacter source,
            RulesetAttackMode attackMode,
            RulesetEffect activeEffect)
        {
            if (GetGrappledActor(source.RulesetCharacter, out var target, out var activeConditionTarget))
            {
                target.RemoveCondition(activeConditionTarget);
            }

            yield break;
        }

        // should lose grapple if attacks with two-handed or with any free hand
        public IEnumerator OnPhysicalAttackInitiatedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier attackModifier,
            RulesetAttackMode attackMode)
        {
            var rulesetAttacker = attacker.RulesetCharacter;

            if (!ValidatorsCharacter.HasFreeHandWithoutTwoHandedInMain(rulesetAttacker) &&
                GetGrappledActor(rulesetAttacker, out var rulesetTarget, out var activeCondition))
            {
                rulesetTarget.RemoveCondition(activeCondition);
            }

            yield break;
        }

        private static void EjectCharactersInArea(
            GameLocationCharacter grappleSource, GameLocationCharacter grappleTarget)
        {
            var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();
            var targetingService = ServiceRepository.GetService<IGameLocationTargetingService>();
            var boxArea = grappleTarget.LocationBoundingBox;
            var affectedCharacters = new List<GameLocationCharacter>();

            targetingService.ComputeTargetsOfAreaOfEffect(boxArea, affectedCharacters, null);

            foreach (var affectedCharacter in affectedCharacters)
            {
                if (affectedCharacter == grappleSource ||
                    affectedCharacter == grappleTarget ||
                    affectedCharacter.RulesetCharacter == null ||
                    !affectedCharacter.RulesetActor.CanReceiveMotion)
                {
                    continue;
                }

                var horizontalBoxArea = boxArea;

                horizontalBoxArea.Min.y = affectedCharacter.LocationPosition.y;
                horizontalBoxArea.Max.y = affectedCharacter.LocationPosition.y;

                if (!ComputeEjectDestination(
                        horizontalBoxArea,
                        affectedCharacter,
                        affectedCharacter.LocationPosition,
                        false,
                        positioningService,
                        grappleSource.DestinationPosition,
                        out var destination))
                {
                    continue;
                }

                affectedCharacter.MyExecuteActionTacticalMove(destination);
            }
        }

        private static bool ComputeEjectDestination(
            BoxInt boxArea,
            GameLocationCharacter character,
            int3 currentPosition,
            bool canStandOnly,
            IGameLocationPositioningService positioningService,
            int3 forbiddenPosition,
            out int3 destination)
        {
            var boxInt = boxArea;
            var ejectDestination = false;

            boxInt.Inflate(1, 0, 1);

            destination = currentPosition;

            foreach (var candidateDestination in boxInt.EnumerateAllPositionsWithin())
            {
                if (candidateDestination == currentPosition ||
                    candidateDestination == forbiddenPosition ||
                    boxArea.Contains(candidateDestination))
                {
                    continue;
                }

                if (!positioningService.CanPlaceCharacter(
                        character, candidateDestination, CellHelpers.PlacementMode.Station) ||
                    positioningService.RaycastGrid(
                        currentPosition,
                        candidateDestination,
                        CellFlags.Surface.MovementBlocker,
                        CellFlags.Side.AllHorizontalSides) ||
                    (canStandOnly &&
                     !positioningService.CanCharacterStayAtPosition_Floor(character, candidateDestination, true)))
                {
                    continue;
                }

                if (ejectDestination)
                {
                    var magnitudeCandidate = (candidateDestination - currentPosition).magnitude2DSqr;
                    var magnitudeDestination = (destination - currentPosition).magnitude2DSqr;

                    if (!magnitudeCandidate.IsReallyInferior(magnitudeDestination))
                    {
                        continue;
                    }
                }

                destination = candidateDestination;
                ejectDestination = true;
            }

            return ejectDestination;
        }
    }
}
