using System.Text;

namespace Deckster.Games.Gabong;

public class GabongCalculator
{
    private static List<GabongTarget> _targets;

    static GabongCalculator()
    {
        _targets = GenerateTargets();
    }

    public static bool IsGabong(int target, IEnumerable<int> numbers)
    {
        if (target == 1) target = 14;
        if (target < 1 || target > 14) return false;
        if (!numbers.Any()) return false;
        
        var source = numbers.ToList();
        var trySomeMore = true;
        var targetToShootFor = _targets.First(x => x.TargetValue == target);
        while (trySomeMore && source.Any())
        {
            trySomeMore = false;
            foreach (var combo in targetToShootFor.GetCombosInPreferredOrder())
            {
                var clone = source.ToList();
                var comboFound = true;
                foreach (var number in combo.Numbers)
                {
                    if (clone.Contains(number))
                    {
                        clone.Remove(number);
                    }
                    else
                    {
                        comboFound = false;
                        break;
                    }
                }
                if(comboFound)
                {
                    trySomeMore = true;
                    source = clone;
                    break;
                }
            } 
        }

        return !source.Any();
    }


    // Generate all possible int combinations that make up the target value
    public static List<GabongTarget> GenerateTargets()
    {
        var sb = new StringBuilder();
        List<GabongTarget> targets = new List<GabongTarget>();
        for (int i = 1; i <= 14; i++)
        {
            var target = new GabongTarget { TargetValue = i };
            target.Combos.Add(new GabongCombo(){Numbers = [i]}); //self is a valid combo
            for (int j = i - 1; j > 0; j--)
            {
                target.Combos.AddRange(ExtractAndAdd(targets, i, j));
                target.Dedupe();
            }
            targets.Add(target);
        }
        targets[13].Combos.Add(new GabongCombo { Numbers = [1] }); // special case, an ace is a 14 all by itself
        return targets;
    }

    private static List<GabongCombo> ExtractAndAdd(List<GabongTarget> targets, int targetToShootFor,
        int targetToExtractFrom)
    {
        var ret = new List<GabongCombo>();
        var combosToStealFrom = targets.First(x => x.TargetValue == targetToExtractFrom).Combos;
        var difference = targetToShootFor - targetToExtractFrom;
        foreach (var comboToStealFrom in combosToStealFrom)
        {
            var newCombo = comboToStealFrom.Numbers.ToList();
            newCombo.Add(difference);
            ret.Add(new GabongCombo { Numbers = newCombo });
        }

        return ret;
    }


    public class GabongTarget
    {
        public int TargetValue { get; set; }
        public List<GabongCombo> Combos { get; set; } = [];
        public void Dedupe()
        {
            Combos = Combos.DistinctBy(c => c.StringRepresentation).ToList();
        }
        public List<GabongCombo> GetCombosInPreferredOrder()
        {
            return Combos.OrderByDescending(x => x.StringRepresentation).ToList();
        }

        public override string ToString()
        {
            return $"Target: {TargetValue}, Combos: {string.Join("\n", GetCombosInPreferredOrder().Select(x => x.ToString()))}";
        }
    }


    public class GabongCombo
    {
        public override string ToString()
        {
            return $"[{string.Join(",", Numbers.OrderByDescending(x => x))}]";
        }

        public List<int> Numbers { get; set; }
        public string StringRepresentation => string.Join(",", Numbers.OrderByDescending(x => x).Select(i=>i.ToString("00")));
    }
}