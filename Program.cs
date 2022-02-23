
using Google.OrTools.LinearSolver;

var datasets = new[] { "a_an_example", "b_basic", "c_coarse", "d_difficult", "e_elaborate" };

foreach (var name in datasets)
{
    System.Console.WriteLine($"Processing dataset {name}...");

    // Read input
    var dataset = new Dataset(Path.Combine("input", name + ".in.txt"));

    // Solve the problem
    var recipe = FindRecipeUsingLinearSolver(dataset);

    // Write output
    // Format: "[number of ingredients in recipe] [ingredient1] [ingredient2] [ingredient3] ..."
    var items = new List<string> { recipe.Count.ToString() };
    items.AddRange(recipe);

    Directory.CreateDirectory("output");

    var content = string.Join(" ", items);
    File.WriteAllText(Path.Combine("output", name + ".out.txt"), content);
}

System.Console.WriteLine("Done.");

HashSet<string> FindRecipeUsingLinearSolver(Dataset dataset)
{
    Solver solver = Solver.CreateSolver("GLOP");
    var zero = solver.MakeIntVar(0, 0, "zero");

    var ingredientVariables = new Dictionary<string, Variable>();

    foreach (var ingredient in dataset.Ingredients)
    {
        ingredientVariables[ingredient] = solver.MakeIntVar(0, 1, ingredient);
    }

    var clients = new List<LinearExpr>();

    foreach (var client in dataset.Clients)
    {
        var need = zero + zero;
        var hate = zero + zero;

        foreach (var ingredient in client.Need)
        {
            need += ingredientVariables[ingredient] * 1;
        }

        foreach (var ingredient in client.Hate)
        {
            hate += ingredientVariables[ingredient] * 100000;
        }

        clients.Add(need - hate);
    }

    var goal = zero + zero;

    foreach (var client in clients)
    {
        goal += client;
    }

    solver.Maximize(goal);

    Solver.ResultStatus resultStatus = solver.Solve();

    if (resultStatus != Solver.ResultStatus.OPTIMAL)
    {
        throw new InvalidOperationException("The problem does not have an optimal solution!");
    }

    Console.WriteLine("Solution:");
    Console.WriteLine("Objective value = " + solver.Objective().Value());

    // [START advanced]
    Console.WriteLine("\nAdvanced usage:");
    Console.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
    Console.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
    // [END advanced]

    return ingredientVariables.Where(kvp => kvp.Value.SolutionValue() == 1).Select(kvp => kvp.Key).ToHashSet();
}

HashSet<string> FindRecipeUsingHistogram(Dataset dataset)
{
    HashSet<string> bestRecipe = new HashSet<string>();
    int bestScore = 0;
    int bestThreshold = 0;

    for (int i = 0; i < 10; i++)
    {
        var recipe = FindRecipeWithThreshold(dataset, i);
        var score = Score(dataset, recipe);

        if (score > bestScore)
        {
            bestRecipe = recipe;
            bestScore = score;
            bestThreshold = i;
        }
    }

    System.Console.WriteLine($"Best score: {bestScore}. Best Threshold: {bestThreshold}");
    return bestRecipe;
}

HashSet<string> FindRecipeWithThreshold(Dataset dataset, int threshold)
{
    // The basic idea is we include all ingredients whose # of clients that need the ingredient
    // is greater than # of clients that hate the ingredient by calculating a histogram for the ingredients.

    var histogram = new Dictionary<string, int>();

    // Exclude the picky clients according to the threshold
    var rasonableClients = dataset.Clients.Where(c => c.Hate.Count <= threshold).ToArray();

    foreach (var ingredient in dataset.Ingredients)
    {
        var score = 0;

        foreach (var client in rasonableClients)
        {
            if (client.Need.Contains(ingredient))
            {
                score++;
            }

            if (client.Hate.Contains(ingredient))
            {
                score--;
            }
        }

        histogram[ingredient] = score;
    }

    return histogram.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToHashSet();
}

int Score(Dataset dataset, HashSet<string> recipe)
{
    var score = 0;

    foreach (var client in dataset.Clients)
    {
        var satisfied = true;
        foreach (var ingredient in client.Need)
        {
            if (!recipe.Contains(ingredient))
            {
                satisfied = false;
                break;
            }
        }

        if (!satisfied)
        {
            continue;
        }

        foreach (var ingredient in client.Hate)
        {
            if (recipe.Contains(ingredient))
            {
                satisfied = false;
                break;
            }
        }

        if (satisfied)
        {
            score++;
        }
    }

    return score;
}

public class Client
{
    public Client(string line1, string line2)
    {
        // Format: # of ingredents, ingredient1, ingredient2, ...
        Need = line1.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToHashSet();
        Hate = line2.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToHashSet();
    }

    public HashSet<string> Need { get; set; }
    public HashSet<string> Hate { get; set; }
}

public class Dataset
{
    public Dataset(string path)
    {
        var lines = File.ReadAllLines(path);

        var totalCount = int.Parse(lines[0]);
        var clients = new List<Client>();

        for (int i = 1; i < lines.Length; i += 2)
        {
            var client = new Client(lines[i], lines[i + 1]);
            Ingredients.UnionWith(client.Need);
            Ingredients.UnionWith(client.Hate);

            clients.Add(client);
        }

        Clients = clients;
        Path = path;
    }

    public IReadOnlyList<Client> Clients { get; }
    public HashSet<string> Ingredients { get; } = new();
    public string Path { get; }
}