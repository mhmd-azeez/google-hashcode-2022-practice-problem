
using Google.OrTools.Sat;

var datasets = new[] { "a_an_example", "b_basic", "c_coarse", "d_difficult", "e_elaborate" };

foreach (var name in datasets)
{
    System.Console.WriteLine($"Processing dataset {name}...");

    // Read input
    var dataset = new Dataset(Path.Combine("input", name + ".in.txt"));

    // Solve the problem
    var recipe = FindRecipeUsingLinearSolver(dataset);
    var score = Score(dataset, recipe);
    System.Console.WriteLine($"Score: {score}");

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
    var model = new CpModel();
    var solver = new CpSolver();
    var zero = model.NewBoolVar("zero");
    var one = model.NewIntVar(1, 1, "one");

    var ingredientVariables = new Dictionary<string, IntVar>();

    foreach (var ingredient in dataset.Ingredients)
    {
        ingredientVariables[ingredient] = model.NewBoolVar(ingredient);
    }

    var clients = new List<LinearExpr>();

    for (int i = 0; i < dataset.Clients.Count; i++)
    {
        var client = dataset.Clients[i];
        var needs = client.Need.ToArray();
        var hates = client.Hate.ToArray();

        var clientVariable = model.NewBoolVar($"client{i}");
        IntVar? need = null;
        IntVar? hate = null;

        if (needs.Length > 2)
        {
            for (int n = 0; n < needs.Length; n += 1)
            {
                var intermediate = model.NewBoolVar($"client{i}_need{n}");

                if (n == 0)
                {
                    model.AddMultiplicationEquality(intermediate, new[] { ingredientVariables[needs[n]], ingredientVariables[needs[n + 1]] });
                    n++;
                }
                else
                {
                    model.AddMultiplicationEquality(intermediate, new[] { need, ingredientVariables[needs[n]] });
                }

                need = intermediate;
            }
        }
        else if (needs.Length == 2)
        {
            need = model.NewBoolVar($"client{i}_need");
            model.AddMultiplicationEquality(need, new[] { ingredientVariables[needs[0]], ingredientVariables[needs[1]] });
        }
        else if (needs.Length == 1)
        {
            need = ingredientVariables[needs[0]];
        }
        else
        {
            need = one;
        }

        if (hates.Length > 2)
        {
            for (int h = 0; h < hates.Length; h += 1)
            {
                var ingredient = ingredientVariables[hates[h]];
                var intermediate = model.NewBoolVar($"client{i}_hate{h}");
                var notInclude = model.NewBoolVar($"client{i}_hate{h}_not");
                model.Add(notInclude != ingredient);

                if (h == 0)
                {
                    var ingredient2 = ingredientVariables[hates[h + 1]];
                    var notInclude2 = model.NewBoolVar($"client{i}_hate{1}_not");
                    model.Add(notInclude2 != ingredient2);

                    model.AddMultiplicationEquality(intermediate, new[] { notInclude, notInclude2 });
                    h++;
                }
                else
                {
                    model.AddMultiplicationEquality(intermediate, new[] { hate, ingredientVariables[hates[h]] });
                }

                hate = intermediate;
            }
        }
        else if (hates.Length == 2)
        {
            var notInclude = model.NewBoolVar($"client{i}_hate{0}_not");
            var ingredient = ingredientVariables[hates[0]];
            model.Add(notInclude != ingredient);

            var ingredient2 = ingredientVariables[hates[1]];
            var notInclude2 = model.NewBoolVar($"client{i}_hate{1}_not2");
            model.Add(notInclude2 != ingredient2);

            hate = model.NewBoolVar($"client{i}_hate");
            model.AddMultiplicationEquality(hate, new[] { notInclude, notInclude2 });
        }
        else if (hates.Length == 1)
        {
            var notInclude = model.NewBoolVar($"client{i}_hate{0}_not");
            var ingredient = ingredientVariables[hates[0]];
            model.Add(notInclude != ingredient);

            hate = notInclude;
        }
        else
        {
            hate = one;
        }

        model.AddMultiplicationEquality(clientVariable, new[] { need, hate });
        clients.Add(clientVariable);
    }

    model.Maximize(LinearExpr.Sum(clients));

    solver.StringParameters = "max_time_in_seconds:10000";
    solver.SetLogCallback(message => Console.WriteLine(message));
    var resultStatus = solver.Solve(model, new VarArraySolutionPrinterWithLimit(10000));

    //if (resultStatus != CpSolverStatus.Optimal)
    //{
    //    throw new InvalidOperationException($"The problem does not have an optimal solution: {resultStatus}. {solver.SolutionInfo()}.");
    //}

    // Console.WriteLine("Solution:");
    Console.WriteLine($"Objective value: {solver.ObjectiveValue} in {solver.WallTime()} ms" );

    return ingredientVariables.Where(kvp => solver.Value(kvp.Value) == 1).Select(kvp => kvp.Key).ToHashSet();
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

public class VarArraySolutionPrinterWithLimit : CpSolverSolutionCallback
{
    public VarArraySolutionPrinterWithLimit(int solution_limit)
    {
        solution_limit_ = solution_limit;
    }

    public override void OnSolutionCallback()
    {
        Console.WriteLine($"Solution #{solution_count_}: time = {WallTime():F2} s. Objective Value: {ObjectiveValue()}");
        //foreach (IntVar v in variables_)
        //{
        //    Console.WriteLine(String.Format("  {0} = {1}", v.ShortString(), Value(v)));
        //}
        solution_count_++;
        if (solution_count_ >= solution_limit_)
        {
            Console.WriteLine(String.Format("Stopping search after {0} solutions", solution_limit_));
            StopSearch();
        }
    }

    public int SolutionCount()
    {
        return solution_count_;
    }

    private int solution_count_;
    private int solution_limit_;
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