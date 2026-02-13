using Grovetracks.Etl.Operations;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var operation = args.Length > 0 ? args[0] : null;

switch (operation)
{
    case "download-simple-drawings":
        await DownloadSimpleDrawingsOperation.RunAsync(config);
        break;

    case "seed-simple-database":
        var truncateSimple = args.Skip(1).Any(a => a == "--truncate");
        await SeedSimpleDatabaseOperation.RunAsync(config, truncateSimple);
        break;

    case "curate-simple-doodles":
        var truncateSeed = args.Skip(1).Any(a => a == "--truncate");
        var perCategory = args.Skip(1)
            .Where(a => a.StartsWith("--per-category="))
            .Select(a => int.TryParse(a["--per-category=".Length..], out var v) ? v : 50)
            .FirstOrDefault(50);
        await CurateSimpleDoodlesOperation.RunAsync(config, perCategory, truncateSeed);
        break;

    case "generate-compositions":
        var truncateGenerated = args.Skip(1).Any(a => a == "--truncate-generated");
        var dryRun = args.Skip(1).Any(a => a == "--dry-run");
        var genPerCategory = args.Skip(1)
            .Where(a => a.StartsWith("--per-category="))
            .Select(a => int.TryParse(a["--per-category=".Length..], out var v) ? v : 5)
            .FirstOrDefault(5);
        var scenes = args.Skip(1)
            .Where(a => a.StartsWith("--scenes="))
            .Select(a => int.TryParse(a["--scenes=".Length..], out var v) ? v : 100)
            .FirstOrDefault(100);
        int? genSeed = args.Skip(1)
            .Where(a => a.StartsWith("--seed="))
            .Select(a => int.TryParse(a["--seed=".Length..], out var v) ? (int?)v : null)
            .FirstOrDefault();
        await GenerateCompositionsOperation.RunAsync(config, genPerCategory, scenes, genSeed, truncateGenerated, dryRun);
        break;

    case "generate-ai-compositions":
        var truncateAi = args.Skip(1).Any(a => a == "--truncate");
        var dryRunAi = args.Skip(1).Any(a => a == "--dry-run");
        var resumeAi = args.Skip(1).Any(a => a == "--resume");
        var aiPerSubject = args.Skip(1)
            .Where(a => a.StartsWith("--per-subject="))
            .Select(a => int.TryParse(a["--per-subject=".Length..], out var v) ? v : 5)
            .FirstOrDefault(5);
        await GenerateAiCompositionsOperation.RunAsync(config, aiPerSubject, truncateAi, dryRunAi, resumeAi);
        break;

    case "generate-local-ai-compositions":
        var truncateLocal = args.Skip(1).Any(a => a == "--truncate");
        var dryRunLocal = args.Skip(1).Any(a => a == "--dry-run");
        var resumeLocal = args.Skip(1).Any(a => a == "--resume");
        var localPerSubject = args.Skip(1)
            .Where(a => a.StartsWith("--per-subject="))
            .Select(a => int.TryParse(a["--per-subject=".Length..], out var v) ? v : 5)
            .FirstOrDefault(5);
        var localModel = args.Skip(1)
            .Where(a => a.StartsWith("--model="))
            .Select(a => a["--model=".Length..])
            .FirstOrDefault(config["Ollama:DefaultModel"] ?? "qwen2.5:14b");
        await GenerateLocalAiCompositionsOperation.RunAsync(config, localModel, localPerSubject, truncateLocal, dryRunLocal, resumeLocal);
        break;

    case "generate-focused-ai-compositions":
        var focusedSubject = args.Skip(1)
            .Where(a => a.StartsWith("--subject="))
            .Select(a => a["--subject=".Length..])
            .FirstOrDefault()
            ?? throw new InvalidOperationException("--subject=NAME is required");
        var focusedModel = args.Skip(1)
            .Where(a => a.StartsWith("--model="))
            .Select(a => a["--model=".Length..])
            .FirstOrDefault(config["Ollama:DefaultModel"] ?? "qwen2.5:14b");
        var focusedPerCall = args.Skip(1)
            .Where(a => a.StartsWith("--per-call="))
            .Select(a => int.TryParse(a["--per-call=".Length..], out var v) ? v : 3)
            .FirstOrDefault(3);
        var focusedIterations = args.Skip(1)
            .Where(a => a.StartsWith("--iterations="))
            .Select(a => int.TryParse(a["--iterations=".Length..], out var v) ? v : 5)
            .FirstOrDefault(5);
        var focusedFewShot = args.Skip(1)
            .Where(a => a.StartsWith("--few-shot="))
            .Select(a => int.TryParse(a["--few-shot=".Length..], out var v) ? v : 10)
            .FirstOrDefault(10);
        var focusedTruncate = args.Skip(1).Any(a => a == "--truncate");
        var focusedDryRun = args.Skip(1).Any(a => a == "--dry-run");
        await GenerateFocusedAiCompositionsOperation.RunAsync(
            config, focusedSubject, focusedModel, focusedPerCall,
            focusedIterations, focusedFewShot, focusedTruncate, focusedDryRun);
        break;

    default:
        Console.WriteLine("Grovetracks ETL");
        Console.WriteLine();
        Console.WriteLine("Usage: dotnet run -- <operation>");
        Console.WriteLine();
        Console.WriteLine("Operations:");
        Console.WriteLine("  download-simple-drawings Download simplified NDJSON files from S3 to local directory");
        Console.WriteLine("  seed-simple-database     Seed PostgreSQL with simplified doodle data (inline drawing) from NDJSON files");
        Console.WriteLine("    --truncate               Truncate table before seeding");
        Console.WriteLine("  curate-simple-doodles    Filter and score simple doodles into seed_compositions table");
        Console.WriteLine("    --truncate               Truncate seed_compositions table before curating");
        Console.WriteLine("    --per-category=N         Number of top doodles to keep per category (default: 50)");
        Console.WriteLine("  generate-compositions    Generate augmented and scene compositions from curated seeds");
        Console.WriteLine("    --truncate-generated     Remove previously generated compositions (keeps curated)");
        Console.WriteLine("    --per-category=N         Augmented variations per curated seed (default: 5)");
        Console.WriteLine("    --scenes=N               Number of multi-fragment scene compositions (default: 100)");
        Console.WriteLine("    --seed=N                 Random seed for reproducibility");
        Console.WriteLine("    --dry-run                Report what would be generated without writing to DB");
        Console.WriteLine("  generate-ai-compositions Generate novel compositions using Claude Sonnet AI");
        Console.WriteLine("    --truncate               Remove previously AI-generated compositions");
        Console.WriteLine("    --per-subject=N          Compositions per subject per API call (default: 5)");
        Console.WriteLine("    --dry-run                Report what would be generated without calling API or writing to DB");
        Console.WriteLine("    --resume                 Skip subjects that already have AI-generated compositions");
        Console.WriteLine("  generate-local-ai-compositions  Generate compositions using local Ollama model");
        Console.WriteLine("    --model=NAME             Ollama model name (default: from config or qwen2.5:14b)");
        Console.WriteLine("    --truncate               Remove previously AI-generated compositions");
        Console.WriteLine("    --per-subject=N          Compositions per subject per API call (default: 5)");
        Console.WriteLine("    --dry-run                Check Ollama connectivity without generating");
        Console.WriteLine("    --resume                 Skip subjects already generated by this model");
        Console.WriteLine("  generate-focused-ai-compositions  Focused single-subject generation with rich few-shot examples");
        Console.WriteLine("    --subject=NAME           Subject to generate (required, e.g. --subject=angel)");
        Console.WriteLine("    --model=NAME             Ollama model name (default: from config or qwen2.5:14b)");
        Console.WriteLine("    --per-call=N             Compositions per API call (default: 3)");
        Console.WriteLine("    --iterations=N           Number of generation rounds (default: 5)");
        Console.WriteLine("    --few-shot=N             Curated examples for few-shot context (default: 10)");
        Console.WriteLine("    --truncate               Remove previous focused generations for this subject");
        Console.WriteLine("    --dry-run                Check config and connectivity without generating");
        break;
}
