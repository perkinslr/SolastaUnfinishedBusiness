﻿using System.Collections.Generic;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.Infrastructure;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Models;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionAttributeModifiers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.WeaponTypeDefinitions;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Feats;

internal static class CraftyFeats
{
    internal static void CreateFeats([NotNull] List<FeatDefinition> feats)
    {
        var proficiencyCraftyArcana = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyArcana")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.SkillOrExpertise, DatabaseHelper.SkillDefinitions.Arcana.Name)
            .AddToDB();

        var proficiencyCraftyAnimalHandling = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyAnimalHandling")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.SkillOrExpertise, DatabaseHelper.SkillDefinitions.AnimalHandling.Name)
            .AddToDB();

        var proficiencyCraftyMedicine = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyMedicine")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.SkillOrExpertise, DatabaseHelper.SkillDefinitions.Medecine.Name)
            .AddToDB();

        var proficiencyCraftyNature = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyNature")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.SkillOrExpertise, DatabaseHelper.SkillDefinitions.Nature.Name)
            .AddToDB();

        var proficiencyCraftyHerbalismKit = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyHerbalismKit")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.ToolOrExpertise, ToolTypeDefinitions.HerbalismKitType.Name)
            .AddToDB();

        var proficiencyCraftyManacalonRosary = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyManacalonRosary")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.ToolOrExpertise, ToolTypeDefinitions.EnchantingToolType.Name)
            .AddToDB();

        var proficiencyCraftyPoisonersKit = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyPoisonersKit")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.ToolOrExpertise, ToolTypeDefinitions.PoisonersKitType.Name)
            .AddToDB();

        var proficiencyCraftyScrollKit = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyScrollKit")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.ToolOrExpertise, ToolTypeDefinitions.ScrollKitType.Name)
            .AddToDB();

        var proficiencyCraftySmithsTools = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftySmithsTools")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.ToolOrExpertise, ToolTypeDefinitions.ArtisanToolSmithToolsType.Name)
            .AddToDB();

        var proficiencyCraftyBows = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyCraftyBows")
            .SetGuiPresentationNoContent(true)
            .SetProficiencies(ProficiencyType.Weapon,
                CustomWeaponsContext.HandXbowWeaponType.Name,
                ShortbowType.Name,
                LongbowType.Name,
                LightCrossbowType.Name,
                HeavyCrossbowType.Name)
            .AddToDB();

        //
        // Aphotecary
        //

        var featApothecaryInt = FeatDefinitionBuilder
            .Create("FeatApothecaryInt")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Pakri, proficiencyCraftyHerbalismKit, proficiencyCraftyArcana)
            .AddToDB();

        var featApothecaryWis = FeatDefinitionBuilder
            .Create("FeatApothecaryWis")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Maraike, proficiencyCraftyHerbalismKit, proficiencyCraftyMedicine)
            .AddToDB();

        var featApothecaryCha = FeatDefinitionBuilder
            .Create("FeatApothecaryCha")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Solasta, proficiencyCraftyHerbalismKit, proficiencyCraftyMedicine)
            .AddToDB();

        GroupFeats.MakeGroup("FeatGroupApothecary", "Apothecary",
            featApothecaryInt,
            featApothecaryWis,
            featApothecaryCha);

        //
        // Toxicologist
        //

        var featToxicologistInt = FeatDefinitionBuilder
            .Create("FeatToxicologistInt")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Pakri, proficiencyCraftyPoisonersKit, proficiencyCraftyNature)
            .AddToDB();

        var featToxicologistWis = FeatDefinitionBuilder
            .Create("FeatToxicologistWis")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Maraike, proficiencyCraftyPoisonersKit, proficiencyCraftyMedicine)
            .AddToDB();

        var featToxicologistCha = FeatDefinitionBuilder
            .Create("FeatToxicologistCha")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Solasta, proficiencyCraftyPoisonersKit,
                proficiencyCraftyAnimalHandling)
            .AddToDB();

        GroupFeats.MakeGroup("FeatGroupToxicologist", "Toxicologist",
            featToxicologistInt,
            featToxicologistWis,
            featToxicologistCha);

        //
        // Others
        //

        var featManacalonCrafter = FeatDefinitionBuilder
            .Create("FeatManacalonCrafter")
            .SetGuiPresentation(Category.Feat)
            .SetMustCastSpellsPrerequisite()
            .SetFeatures(AttributeModifierCreed_Of_Pakri, proficiencyCraftyManacalonRosary, proficiencyCraftyArcana)
            .AddToDB();

        var featCraftyScribe = FeatDefinitionBuilder
            .Create("FeatCraftyScribe")
            .SetGuiPresentation(Category.Feat)
            .SetMustCastSpellsPrerequisite()
            .SetFeatures(AttributeModifierCreed_Of_Pakri, proficiencyCraftyScrollKit, proficiencyCraftyArcana)
            .AddToDB();

        var featCraftyFletcher = FeatDefinitionBuilder
            .Create("FeatCraftyFletcher")
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(AttributeModifierCreed_Of_Misaye, proficiencyCraftySmithsTools, proficiencyCraftyBows)
            .AddToDB();


        feats.AddRange(
            featApothecaryInt,
            featApothecaryWis,
            featApothecaryCha,
            featToxicologistInt,
            featToxicologistWis,
            featToxicologistCha,
            featManacalonCrafter,
            featCraftyScribe,
            featCraftyFletcher);
    }
}
