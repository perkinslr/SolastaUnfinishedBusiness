﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Helpers;
using TA;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class GameLocationPositioningManagerPatcher
{
    //PATCH: avoids a trace message when party greater than 4 (PARTYSIZE)
    [HarmonyPatch(typeof(GameLocationPositioningManager), nameof(GameLocationPositioningManager.CharacterMoved),
        typeof(GameLocationCharacter),
        typeof(int3), typeof(int3), typeof(RulesetActor.SizeParameters), typeof(RulesetActor.SizeParameters))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CharacterMoved_Patch
    {
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            var logErrorMethod = typeof(Trace).GetMethod("LogError", BindingFlags.Public | BindingFlags.Static,
                Type.DefaultBinder, [typeof(string)], null);

            return instructions.ReplaceCalls(logErrorMethod, "GameLocationPositioningManager.CharacterMoved",
                new CodeInstruction(OpCodes.Pop));
        }

        [UsedImplicitly]
        public static bool Prefix(GameLocationCharacter character,
            int3 oldPosition,
            int3 newPosition,
            RulesetActor.SizeParameters oldSize,
            RulesetActor.SizeParameters newSize)
        {
            //PATCH: process location change only if position or size has changed
            //FIX: fixes Spike Growth and similar spells triggering twice on last tile of movement
            return oldPosition != newPosition
                   || oldSize.minExtent != newSize.minExtent
                   || oldSize.maxExtent != newSize.maxExtent;
        }
    }
}
