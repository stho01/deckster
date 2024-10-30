using Deckster.Client.Protocol;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

public static class Asserts
{
    public static void Success(DecksterResponse response)
    {
        switch (response)
        {
            case { HasError: true }:
                Assert.Fail($"Expeced success, but got '{response.Error}'");
                break;
        }
    }

    public static void Fail(DecksterResponse response, string message)
    {
        switch (response)
        {
            case { HasError: true }:
            {
                Assert.That(response.Error, Is.EqualTo(message));
                break;
            }
            default:
                Assert.Fail($"Expected error, but got {response.GetType().Name}");
                break;
        }
    }
}