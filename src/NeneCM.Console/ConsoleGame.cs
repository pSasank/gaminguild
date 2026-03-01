using NeneCM.Core;
using NeneCM.Core.Data;
using NeneCM.Core.Models;
using NeneCM.Core.Tests.Mocks;

namespace NeneCM.Console;

public class ConsoleGame
{
    private GameManager? _gameManager;
    private StateContent? _content;
    private bool _running = true;

    public void Run()
    {
        ShowTitle();
        LoadContent();
        StartNewGame();
        MainLoop();
    }

    private void ShowTitle()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║     ███╗   ██╗███████╗███╗   ██╗███████╗                 ║
║     ████╗  ██║██╔════╝████╗  ██║██╔════╝                 ║
║     ██╔██╗ ██║█████╗  ██╔██╗ ██║█████╗                   ║
║     ██║╚██╗██║██╔══╝  ██║╚██╗██║██╔══╝                   ║
║     ██║ ╚████║███████╗██║ ╚████║███████╗                 ║
║     ╚═╝  ╚═══╝╚══════╝╚═╝  ╚═══╝╚══════╝                 ║
║                                                           ║
║              ██████╗███╗   ███╗                           ║
║             ██╔════╝████╗ ████║                           ║
║             ██║     ██╔████╔██║                           ║
║             ██║     ██║╚██╔╝██║                           ║
║             ╚██████╗██║ ╚═╝ ██║                           ║
║              ╚═════╝╚═╝     ╚═╝                           ║
║                                                           ║
║         A Political Strategy Game                         ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
");
        Console.ResetColor();
        Console.WriteLine("\nPress any key to start...");
        Console.ReadKey(true);
    }

    private void LoadContent()
    {
        Console.WriteLine("\nLoading content...");

        var loader = new StateContentLoader();
        var projectRoot = FindProjectRoot();
        var jsonPath = Path.Combine(projectRoot, "data", "policies", "telangana.json");

        if (File.Exists(jsonPath))
        {
            var json = File.ReadAllText(jsonPath);
            _content = loader.LoadFromJson(json);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Loaded {_content?.State.Name} content");
            Console.WriteLine($"  - {_content?.Policies.Count} policies");
            Console.WriteLine($"  - {_content?.Events.Count} events");
            Console.WriteLine($"  - {_content?.Factions.Count} factions");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("! Content file not found, using mock data");
            Console.ResetColor();
            _content = CreateMockContent();
        }

        Thread.Sleep(1000);
    }

    private void StartNewGame()
    {
        var repository = new ContentPolicyRepository(_content!);
        _gameManager = new GameManager(repository, _content!);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ New game started as CM of {_content!.State.Name}");
        Console.ResetColor();
        Thread.Sleep(500);
    }

    private void MainLoop()
    {
        while (_running && !_gameManager!.IsGameOver)
        {
            Console.Clear();
            ShowStatus();

            if (_gameManager.PendingEvent != null)
            {
                HandleEvent();
                continue;
            }

            ShowMainMenu();
            var choice = GetInput("Select option: ");

            switch (choice)
            {
                case "1": ShowPolicies(); break;
                case "2": ImplementPolicy(); break;
                case "3": RemovePolicy(); break;
                case "4": ShowDetailedStatus(); break;
                case "5": AdvanceTurn(); break;
                case "6": SaveAndQuit(); break;
                default:
                    ShowError("Invalid option");
                    break;
            }
        }

        if (_gameManager!.IsGameOver)
        {
            ShowGameOver();
        }
    }

    private void ShowStatus()
    {
        var state = _gameManager!.CurrentState;
        var approval = state.OverallApproval;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"═══════════════════════════════════════════════════════════");
        Console.WriteLine($"  {state.StateName.ToUpper()} | Turn {state.CurrentTurn} | {state.CurrentDateString}");
        Console.WriteLine($"═══════════════════════════════════════════════════════════");
        Console.ResetColor();

        // Budget
        Console.Write("  Budget: ");
        Console.ForegroundColor = state.Budget.IsInDeficit ? ConsoleColor.Red : ConsoleColor.Green;
        Console.WriteLine($"₹{state.Budget.AvailableBudget:N0} Cr available (₹{state.Budget.TotalBudget:N0} Cr total)");
        Console.ResetColor();

        // Approval
        Console.Write("  Approval: ");
        Console.ForegroundColor = approval >= 60 ? ConsoleColor.Green : approval >= 45 ? ConsoleColor.Yellow : ConsoleColor.Red;
        Console.Write($"{approval:F1}%");
        Console.ResetColor();
        Console.WriteLine($" | {GetApprovalBar(approval)}");

        // Election countdown
        var turnsLeft = state.TurnsUntilElection;
        Console.Write("  Next Election: ");
        Console.ForegroundColor = turnsLeft <= 12 ? ConsoleColor.Yellow : ConsoleColor.White;
        Console.WriteLine($"{turnsLeft} months ({turnsLeft / 12} years {turnsLeft % 12} months)");
        Console.ResetColor();

        // Active policies count
        Console.WriteLine($"  Active Policies: {state.ActivePolicies.Count}");

        // Debt warning
        if (state.Budget.Debt > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ⚠ Debt: ₹{state.Budget.Debt:N0} Cr");
            Console.ResetColor();
        }

        Console.WriteLine($"───────────────────────────────────────────────────────────");
    }

    private string GetApprovalBar(float approval)
    {
        int filled = (int)(approval / 5);
        int empty = 20 - filled;
        return $"[{"█".PadRight(filled, '█')}{"░".PadRight(empty, '░')}]";
    }

    private void ShowMainMenu()
    {
        Console.WriteLine("\n  ACTIONS:");
        Console.WriteLine("  1. View Available Policies");
        Console.WriteLine("  2. Implement Policy");
        Console.WriteLine("  3. Remove Policy");
        Console.WriteLine("  4. View Detailed Status");
        Console.WriteLine("  5. Advance Turn →");
        Console.WriteLine("  6. Save & Quit");
        Console.WriteLine();
    }

    private void ShowPolicies()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n═══ AVAILABLE POLICIES ═══\n");
        Console.ResetColor();

        var categories = _content!.Policies
            .GroupBy(p => p.Category)
            .OrderBy(g => g.Key);

        foreach (var category in categories)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  [{category.Key.ToUpper()}]");
            Console.ResetColor();

            foreach (var policy in category)
            {
                var isActive = _gameManager!.CurrentState.ActivePolicies.Any(p => p.PolicyId == policy.Id);
                var prefix = isActive ? "✓" : " ";
                var color = isActive ? ConsoleColor.Green : ConsoleColor.White;

                Console.ForegroundColor = color;
                Console.WriteLine($"  {prefix} {policy.Id}. {policy.Name} - ₹{policy.Cost_per_year:N0} Cr/year");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"      {policy.Description}");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private void ImplementPolicy()
    {
        Console.Clear();
        Console.WriteLine("\n═══ IMPLEMENT POLICY ═══\n");

        // Show affordable policies
        var affordable = _content!.Policies
            .Where(p => p.Cost_per_year <= _gameManager!.CurrentState.Budget.AvailableBudget)
            .Where(p => !_gameManager.CurrentState.ActivePolicies.Any(ap => ap.PolicyId == p.Id))
            .ToList();

        if (!affordable.Any())
        {
            ShowError("No affordable policies available!");
            return;
        }

        foreach (var policy in affordable)
        {
            Console.WriteLine($"  {policy.Id}. {policy.Name} - ₹{policy.Cost_per_year:N0} Cr/year");
        }

        Console.WriteLine("\n  0. Cancel");

        var input = GetInput("\nEnter policy ID: ");
        if (input == "0") return;

        if (int.TryParse(input, out int policyId))
        {
            var levelInput = GetInput("Enter level (1-5, default 1): ");
            int level = int.TryParse(levelInput, out int l) ? Math.Clamp(l, 1, 5) : 1;

            if (_gameManager!.ImplementPolicy(policyId, level))
            {
                var policy = _content.Policies.First(p => p.Id == policyId);
                ShowSuccess($"Implemented {policy.Name} at level {level}!");
            }
            else
            {
                ShowError("Failed to implement policy. Check budget or if already active.");
            }
        }
    }

    private void RemovePolicy()
    {
        Console.Clear();
        Console.WriteLine("\n═══ REMOVE POLICY ═══\n");

        var active = _gameManager!.CurrentState.ActivePolicies;
        if (!active.Any())
        {
            ShowError("No active policies to remove!");
            return;
        }

        foreach (var ap in active)
        {
            var policy = _content!.Policies.FirstOrDefault(p => p.Id == ap.PolicyId);
            if (policy != null)
            {
                Console.WriteLine($"  {policy.Id}. {policy.Name} (Level {ap.Level}, {ap.MonthsActive} months active)");
            }
        }

        Console.WriteLine("\n  0. Cancel");

        var input = GetInput("\nEnter policy ID to remove: ");
        if (input == "0") return;

        if (int.TryParse(input, out int policyId))
        {
            if (_gameManager.RemovePolicy(policyId))
            {
                ShowSuccess("Policy removed!");
            }
            else
            {
                ShowError("Failed to remove policy.");
            }
        }
    }

    private void ShowDetailedStatus()
    {
        Console.Clear();
        var state = _gameManager!.CurrentState;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n═══ DETAILED STATUS ═══\n");
        Console.ResetColor();

        // Metrics
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ECONOMIC INDICATORS:");
        Console.ResetColor();
        foreach (var metric in state.Metrics.OrderBy(m => m.Key))
        {
            var def = _content!.Metrics.FirstOrDefault(m => m.Name == metric.Key);
            var displayName = def?.Display_name ?? metric.Key;
            var unit = def?.Unit ?? "";
            Console.WriteLine($"    {displayName}: {metric.Value:F1}{unit}");
        }

        // Factions
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n  FACTION APPROVAL:");
        Console.ResetColor();
        foreach (var faction in state.FactionApprovals.OrderByDescending(f => f.Approval))
        {
            var def = _content!.Factions.FirstOrDefault(f => f.Id == faction.FactionId);
            var name = def?.Name ?? $"Faction {faction.FactionId}";
            var color = faction.Approval >= 60 ? ConsoleColor.Green : faction.Approval >= 40 ? ConsoleColor.Yellow : ConsoleColor.Red;

            Console.Write($"    {name} ({faction.PopulationPercent}%): ");
            Console.ForegroundColor = color;
            Console.WriteLine($"{faction.Approval:F1}%");
            Console.ResetColor();
        }

        // Active policies
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n  ACTIVE POLICIES:");
        Console.ResetColor();
        if (state.ActivePolicies.Any())
        {
            foreach (var ap in state.ActivePolicies)
            {
                var policy = _content!.Policies.FirstOrDefault(p => p.Id == ap.PolicyId);
                if (policy != null)
                {
                    Console.WriteLine($"    • {policy.Name} (Lvl {ap.Level}) - ₹{policy.Cost_per_year * ap.Level:N0} Cr/yr");
                }
            }
        }
        else
        {
            Console.WriteLine("    (none)");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }

    private void AdvanceTurn()
    {
        var result = _gameManager!.AdvanceTurn();

        if (!result.Success)
        {
            ShowError(result.Message);
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n  → Advanced to {_gameManager.CurrentState.CurrentDateString}");
        Console.ResetColor();

        if (result.IsElectionTurn && !result.IsGameOver)
        {
            ShowElectionResult();
        }

        Thread.Sleep(500);
    }

    private void HandleEvent()
    {
        var evt = _gameManager!.PendingEvent!;

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  EVENT: {evt.Name.PadRight(49)}║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine($"\n  {evt.Description}\n");

        for (int i = 0; i < evt.Choices.Count; i++)
        {
            var choice = evt.Choices[i];
            Console.Write($"  {i + 1}. {choice.Text}");
            if (choice.Cost > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" (Cost: ₹{choice.Cost:N0} Cr)");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        while (true)
        {
            var input = GetInput("\nYour choice: ");
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= evt.Choices.Count)
            {
                _gameManager.ResolveEvent(choice - 1);
                ShowSuccess("Decision made!");
                Thread.Sleep(500);
                break;
            }
            ShowError("Invalid choice");
        }
    }

    private void ShowElectionResult()
    {
        var state = _gameManager!.CurrentState;
        var won = state.TermsWon > 0;

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                     ELECTION RESULTS                       ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.WriteLine($"  Overall Approval: {state.OverallApproval:F1}%\n");

        Console.ForegroundColor = won ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(won
            ? "  ★ CONGRATULATIONS! You have been re-elected! ★"
            : "  ✗ You have lost the election. ✗");
        Console.ResetColor();

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(true);
    }

    private void ShowGameOver()
    {
        Console.Clear();
        var state = _gameManager!.CurrentState;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                       GAME OVER                            ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");
        Console.ResetColor();

        Console.WriteLine($"  Reason: {_gameManager.GameOverReason}");
        Console.WriteLine($"  Final Turn: {state.CurrentTurn}");
        Console.WriteLine($"  Final Approval: {state.OverallApproval:F1}%");
        Console.WriteLine($"  Terms Won: {state.TermsWon}");

        Console.WriteLine("\n  Thanks for playing Nene CM!");
        Console.WriteLine("\n  Press any key to exit...");
        Console.ReadKey(true);
    }

    private void SaveAndQuit()
    {
        Console.WriteLine("\n  Game state would be saved here.");
        Console.WriteLine("  Thanks for playing!\n");
        _running = false;
    }

    private string GetInput(string prompt)
    {
        Console.Write($"  {prompt}");
        return Console.ReadLine()?.Trim() ?? "";
    }

    private void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n  ✓ {message}");
        Console.ResetColor();
        Thread.Sleep(800);
    }

    private void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  ✗ {message}");
        Console.ResetColor();
        Thread.Sleep(800);
    }

    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null && !File.Exists(Path.Combine(dir, "NeneCM.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? Directory.GetCurrentDirectory();
    }

    private StateContent CreateMockContent()
    {
        return new StateContent
        {
            State = new StateInfo { Name = "MockState", Starting_budget = 100000, Starting_year = 2024 },
            Metrics = new List<MetricDefinition>
            {
                new() { Name = "gdp_growth", Display_name = "GDP Growth", Base_value = 5, Min = -10, Max = 20, Unit = "%" }
            },
            Factions = new List<FactionDefinition>
            {
                new() { Id = 1, Name = "Citizens", Population_percent = 100, Priorities = new List<string>() }
            },
            Policies = new List<PolicyDefinition>(),
            Events = new List<EventDefinition>()
        };
    }
}
