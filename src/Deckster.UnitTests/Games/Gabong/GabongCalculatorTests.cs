using Deckster.Games.Gabong;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Gabong;

public class GabongCalculatorTests
{

    [Test]
    public void TestGabongCases()
    {
        TestIsGabong(true, 2, [2]);
        TestIsGabong(false, 2, [2,1]);
        TestIsGabong(true, 3, [2,1]);
        TestIsGabong(true, 3, [3,2,1]);
        TestIsGabong(false, 5, [3,2,1]);
        TestIsGabong(true, 6, [3,2,1]);
        TestIsGabong(true, 6, [3,2,1,6,5,1,4,2,1,1,1,3]);
        TestIsGabong(false, 12, [3,2,1,6,5,1,4,2,1,1,1,3]);
        TestIsGabong(true, 12, [3,2,1,6,5,1,4,2,1,1,1,3,3,3]);
        TestIsGabong(true, 14, [13,1,1]);
        TestIsGabong(true, 1, [13,1,1]);
    }
    
    
    public void TestIsGabong(bool expected, int target, List<int> hand)
    {
        Assert.That(GabongCalculator.IsGabong(target, hand) == expected, $"Expected {string.Join(",",hand)} gabong against {target} to be {expected} but got {GabongCalculator.IsGabong(target, hand)}");
        
    }
    
}