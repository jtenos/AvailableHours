namespace AvailableHoursGenerator;

internal class HalfHourBlock
{
    private static readonly List<HalfHourBlock> _blocks;

    public required DateTime StartDateUTC { get; init; }
    public required DateTime EndDateUTC { get; init; }
    public bool IsFree { get; private set; } = true;

    static HalfHourBlock()
    {
        Console.WriteLine("Building blocks...");

        DateTime dt = Program.Start;
        _blocks = [];
        while (dt < Program.End)
        {
            _blocks.Add(new HalfHourBlock
            {
                StartDateUTC = dt,
                EndDateUTC = dt.AddMinutes(30),
                IsFree = true
            });
            dt = dt.AddMinutes(30);
        }
        Console.WriteLine("Done");
    }

    public static IEnumerable<HalfHourBlock> GetMatchingBlocks(DateTime startUTC, DateTime endUTC)
    {
        return _blocks.Where(b => startUTC <= b.EndDateUTC && endUTC >= b.StartDateUTC);
    }

    public static void ReserveBlock(DateTime startUTC, DateTime endUTC)
    {
        foreach (HalfHourBlock block in GetMatchingBlocks(startUTC, endUTC))
        {
            block.IsFree = false;
        }
    }

    public static IEnumerable<HalfHourBlock> GetAllBlocks() => _blocks;
    public static IEnumerable<HalfHourBlock> GetFreeBlocks() => _blocks.Where(b => b.IsFree);
}
