namespace Deckster.Games.CodeGeneration.Meta;

public class MethodMeta
{
    public string Name { get; init; }
    public List<ParameterMeta> Parameters { get; init; }
    public MessageMeta ReturnType { get; init; }
}