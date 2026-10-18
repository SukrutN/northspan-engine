using System.Globalization;

using Northspan.Application;
using Northspan.Domain;

if (args.Length == 0 || args is ["--help"] || args is ["-h"])
{
    PrintUsage();
    return 0;
}

if (args[0] != "walk")
{
    Console.Error.WriteLine($"Unknown command '{args[0]}'.");
    PrintUsage();
    return 2;
}

try
{
    var flags = ReadFlags(args.AsSpan(1));
    if (!flags.TryGetValue("--floor", out var floorPath) || !flags.TryGetValue("--ticket", out var ticketPath))
    {
        Console.Error.WriteLine("walk needs --floor and --ticket.");
        PrintUsage();
        return 2;
    }

    if (!File.Exists(floorPath))
    {
        Console.Error.WriteLine($"No floor file at {floorPath}.");
        return 2;
    }

    if (!File.Exists(ticketPath))
    {
        Console.Error.WriteLine($"No ticket file at {ticketPath}.");
        return 2;
    }

    var rules = WalkRules.Demo;
    if (flags.TryGetValue("--rules", out var rulesPath))
    {
        if (!File.Exists(rulesPath))
        {
            Console.Error.WriteLine($"No rules file at {rulesPath}.");
            return 2;
        }

        rules = RulesFile.Read(rulesPath);
    }

    var floor = FloorFile.Read(floorPath);
    var ticket = TicketFile.Read(ticketPath);
    var planned = new PlanWalk().Execute(floor, ticket, rules);
    PrintWalk(planned);
    return 0;
}
catch (PlanningException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

static Dictionary<string, string> ReadFlags(ReadOnlySpan<string> args)
{
    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i++)
    {
        var token = args[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{token}'.");
        }

        if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Missing value for {token}.");
        }

        map[token] = args[++i];
    }

    return map;
}

static void PrintWalk(PlannedWalk planned)
{
    var floor = planned.Floor;
    var walk = planned.Walk;
    var title = planned.TicketId is null ? floor.Name : $"{floor.Name} — {planned.TicketId}";
    if (!string.IsNullOrWhiteSpace(planned.House))
    {
        title = $"{title} {planned.House}";
    }

    Console.WriteLine(title);
    if (!string.IsNullOrWhiteSpace(planned.Note))
    {
        Console.WriteLine(planned.Note);
    }

    Console.WriteLine();
    Console.WriteLine($"{"#",-4}{"Location",-12}{"Run",-10}{"Metres",8}  Via");
    foreach (var stop in walk.Stops)
    {
        var run = floor.RequireRun(stop.RunCode);
        var location = string.Join(",", stop.Locations);
        Console.WriteLine(
            $"{stop.Order,-4}{location,-12}{run.Name,-10}{MetresText.Format(stop.MetresFromPrevious),8}  {stop.Via}");
    }

    Console.WriteLine();
    Console.WriteLine($"{walk.Stops.Count} stops");
    Console.WriteLine($"Planned   {MetresText.Format(walk.DistanceM)} m   {planned.DurationLabel}");
    Console.WriteLine($"Sweep     {MetresText.Format(walk.SweepDistanceM)} m   before improvement");
    var comparison = planned.ShorterPercent == 0
        ? "same length as the sweep"
        : $"{planned.ShorterPercent.ToString(CultureInfo.InvariantCulture)}%";
    Console.WriteLine($"Shorter   {MetresText.Format(planned.SavedM)} m   {comparison}");

    if (!walk.ReturnedToGate)
    {
        Console.WriteLine("The walk does not return to the gate.");
    }
}

static void PrintUsage()
{
    Console.WriteLine("northspan walk --floor <file> --ticket <file> [--rules <file>]");
    Console.WriteLine();
    Console.WriteLine("Plans a cart walk on the floor and prints it beside the unimproved aisle sweep.");
}
