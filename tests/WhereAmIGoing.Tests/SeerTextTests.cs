using Xunit;

namespace WhereAmIGoing.Tests
{
    public class SeerTextTests
    {
        [Fact] public void DisplayName_AppendsStageInParens() =>
            Assert.Equal("Lunar Seer (Sky Meadow)", SeerText.AppendStage("Lunar Seer", "Sky Meadow"));

        [Fact] public void Context_WithCost_InsertsAfterLabel() =>
            Assert.Equal("Dream of Sky Meadow <nobr>(2 Lunar)</nobr>",
                SeerText.InsertStageIntoContext("Dream <nobr>(2 Lunar)</nobr>", "Dream", "Sky Meadow"));

        [Fact] public void Context_NoCost_InsertsAfterLabel() =>
            Assert.Equal("Dream of Sky Meadow", SeerText.InsertStageIntoContext("Dream", "Dream", "Sky Meadow"));

        [Fact] public void Context_LabelMismatch_AppendsBracket() =>
            Assert.Equal("Purchase <nobr>(2 Lunar)</nobr> [Sky Meadow]",
                SeerText.InsertStageIntoContext("Purchase <nobr>(2 Lunar)</nobr>", "Dream", "Sky Meadow"));

        [Fact] public void Context_EmptyLabel_AppendsBracket() =>
            Assert.Equal("Dream [Sky Meadow]", SeerText.InsertStageIntoContext("Dream", "", "Sky Meadow"));

        [Fact] public void Context_NonAsciiLabel_Inserts() =>
            Assert.Equal("Träumen of Himmelswiese <nobr>(2)</nobr>",
                SeerText.InsertStageIntoContext("Träumen <nobr>(2)</nobr>", "Träumen", "Himmelswiese"));

        [Fact] public void Context_StageWithBraces_IsLiteral() =>
            Assert.Equal("Dream of {0} Plains", SeerText.InsertStageIntoContext("Dream", "Dream", "{0} Plains"));
    }
}
