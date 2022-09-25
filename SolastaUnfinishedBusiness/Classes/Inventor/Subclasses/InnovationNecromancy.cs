using SolastaUnfinishedBusiness.Builders;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Classes.Inventor.Subclasses;

public static class InnovationNecromancy
{
    public static CharacterSubclassDefinition Build()
    {
        return CharacterSubclassDefinitionBuilder
            .Create("InventorInnovationNecromancy")
            .SetGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.SorcerousHauntedSoul)
            .AddToDB();
    }
}