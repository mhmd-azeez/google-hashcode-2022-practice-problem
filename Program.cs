
// using Google.OrTools.LinearSolver;
using Google.OrTools.Sat;
using Google.OrTools.LinearSolver;

var model = new CpModel();
var solver = new CpSolver();

var cheese = model.NewBoolVar("cheese");
var peppers = model.NewBoolVar("peppers");
var basil = model.NewBoolVar("basil");
var pineapple = model.NewBoolVar("pineapple");
var mushrooms = model.NewBoolVar("mushrooms");
var tomatoes = model.NewBoolVar("tomatoes");

//var client1 = (cheese * 100) + (peppers * 100);
var client1 = model.NewBoolVar("client1");
model.Add(client1 == 1).OnlyEnforceIf(cheese);
model.Add(client1 == 0).OnlyEnforceIf(peppers.Not());
model.Add(client1 == 1).OnlyEnforceIf(peppers);
model.Add(client1 == 0).OnlyEnforceIf(cheese.Not());

var client2 = model.NewBoolVar("client2");
model.Add(client2 == 1).OnlyEnforceIf(basil);
model.Add(client2 == 0).OnlyEnforceIf(basil.Not());

model.Add(client2 == 0).OnlyEnforceIf(pineapple);
model.Add(client2 == 1).OnlyEnforceIf(pineapple.Not());

//var client2 = (basil * 100) - (pineapple * 1000);
//var client3 = ((mushrooms * 100) + (tomatoes * 100)) - (basil * 1000);

var client3 = model.NewBoolVar("client3");
model.Add(client3 == 1).OnlyEnforceIf(mushrooms);
model.Add(client3 == 0).OnlyEnforceIf(mushrooms.Not());
model.Add(client3 == 1).OnlyEnforceIf(tomatoes);
model.Add(client3 == 0).OnlyEnforceIf(tomatoes.Not());

model.Add(client3 == 0).OnlyEnforceIf(basil);
model.Add(client3 == 1).OnlyEnforceIf(basil.Not());

model.Maximize(client1 + client2 + client3);

var resultStatus = solver.Solve(model);

Console.WriteLine("Solution:");
Console.WriteLine("Objective value = " + solver.ObjectiveValue);
Console.WriteLine("cheese = " + solver.Value(cheese));
Console.WriteLine("peppers = " + solver.Value(peppers));
Console.WriteLine("basil = " + solver.Value(basil));
Console.WriteLine("pineapple = " + solver.Value(pineapple));
Console.WriteLine("mushrooms = " + solver.Value(mushrooms));
Console.WriteLine("tomatoes = " + solver.Value(tomatoes));
// [END print_solution]

// [START advanced]
Console.WriteLine("\nAdvanced usage:");
Console.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
// [END advanced]
return;



// var datasets = new[] { "a_an_example", "b_basic", "c_coarse", "d_difficult", "e_elaborate" };

// foreach (var name in datasets)
// {
//     System.Console.WriteLine($"Processing dataset {name}...");

//     // Read input
//     var dataset = new Dataset(Path.Combine("input", name + ".in.txt"));

//     // Solve the problem
//     var recipe = FindRecipeUsingLinearSolver(dataset);
//     var score = Score(dataset, recipe);
//     System.Console.WriteLine($"Score: {score}");

//     // Write output
//     // Format: "[number of ingredients in recipe] [ingredient1] [ingredient2] [ingredient3] ..."
//     var items = new List<string> { recipe.Count.ToString() };
//     items.AddRange(recipe);

//     Directory.CreateDirectory("output");

//     var content = string.Join(" ", items);
//     File.WriteAllText(Path.Combine("output", name + ".out.txt"), content);
// }

// System.Console.WriteLine("Done.");

// HashSet<string> FindRecipeUsingLinearSolver(Dataset dataset)
// {
//     var model = new CpModel();
//     var solver = new CpSolver();

//     var zero = model.NewIntVar(0, 0, "zero");

//     var ingredientVariables = new Dictionary<string, IntVar>();
//     var evilIngredientVariables = new Dictionary<string, IntVar>();
//     foreach (var ingredient in dataset.Ingredients)
//     {
//         var ingredientVariable = ingredientVariables[ingredient] = model.NewBoolVar(ingredient);
//         var evilIngredientVariable = evilIngredientVariables[ingredient] = model.NewBoolVar(ingredient);
//         model.Add(evilIngredientVariable == 0).OnlyEnforceIf(ingredientVariable);
//         model.Add(evilIngredientVariable == 1).OnlyEnforceIf(ingredientVariable.Not());
//     }

//     var clients = new List<LinearExpr>();

//     for (int i = 0; i < dataset.Clients.Count; i++)
//     {
//         var client = dataset.Clients[i];

//         var expressions = new List<IntVar>();
//         var clientExpression = model.NewBoolVar($"client{i}");

//         foreach (var ingredient in client.Need)
//         {
//             expressions.Add(ingredientVariables[ingredient]);
//         }

//         foreach (var ingredient in client.Hate)
//         {
//             expressions.Add(evilIngredientVariables[ingredient]);
//         }

//         model.AddMultiplicationEquality(clientExpression, expressions);
//         clients.Add(clientExpression);
//     }

//     var goal = zero + zero;

//     foreach (var client in clients)
//     {
//         goal += client;
//     }

//     model.Maximize(goal);

//     var resultStatus = solver.Solve(model);

//     // if (resultStatus != CpSolverStatus.Optimal)
//     // {
//     //     throw new InvalidOperationException("The problem does not have an optimal solution!");
//     // }

//     System.Console.WriteLine($"Solution status: {resultStatus}");

//     // Console.WriteLine("\nAdvanced usage:");
//     // Console.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
//     // Console.WriteLine("Problem solved in " + solver.Iterations() + " iterations");

//     return ingredientVariables.Where(kvp => solver.Value(kvp.Value) == 1).Select(kvp => kvp.Key).ToHashSet();
// }

// HashSet<string> FindRecipeUsingHistogram(Dataset dataset)
// {
//     HashSet<string> bestRecipe = new HashSet<string>();
//     int bestScore = 0;
//     int bestThreshold = 0;

//     for (int i = 0; i < 10; i++)
//     {
//         var recipe = FindRecipeWithThreshold(dataset, i);
//         var score = Score(dataset, recipe);

//         if (score > bestScore)
//         {
//             bestRecipe = recipe;
//             bestScore = score;
//             bestThreshold = i;
//         }
//     }

//     System.Console.WriteLine($"Best score: {bestScore}. Best Threshold: {bestThreshold}");
//     return bestRecipe;
// }

// HashSet<string> FindRecipeWithThreshold(Dataset dataset, int threshold)
// {
//     // The basic idea is we include all ingredients whose # of clients that need the ingredient
//     // is greater than # of clients that hate the ingredient by calculating a histogram for the ingredients.

//     var histogram = new Dictionary<string, int>();

//     // Exclude the picky clients according to the threshold
//     var rasonableClients = dataset.Clients.Where(c => c.Hate.Count <= threshold).ToArray();

//     foreach (var ingredient in dataset.Ingredients)
//     {
//         var score = 0;

//         foreach (var client in rasonableClients)
//         {
//             if (client.Need.Contains(ingredient))
//             {
//                 score++;
//             }

//             if (client.Hate.Contains(ingredient))
//             {
//                 score--;
//             }
//         }

//         histogram[ingredient] = score;
//     }

//     return histogram.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToHashSet();
// }

// int Score(Dataset dataset, HashSet<string> recipe)
// {
//     var score = 0;

//     foreach (var client in dataset.Clients)
//     {
//         var satisfied = true;
//         foreach (var ingredient in client.Need)
//         {
//             if (!recipe.Contains(ingredient))
//             {
//                 satisfied = false;
//                 break;
//             }
//         }

//         if (!satisfied)
//         {
//             continue;
//         }

//         foreach (var ingredient in client.Hate)
//         {
//             if (recipe.Contains(ingredient))
//             {
//                 satisfied = false;
//                 break;
//             }
//         }

//         if (satisfied)
//         {
//             score++;
//         }
//     }

//     return score;
// }

// public class Client
// {
//     public Client(string line1, string line2)
//     {
//         // Format: # of ingredents, ingredient1, ingredient2, ...
//         Need = line1.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToHashSet();
//         Hate = line2.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToHashSet();
//     }

//     public HashSet<string> Need { get; set; }
//     public HashSet<string> Hate { get; set; }
// }

// public class Dataset
// {
//     public Dataset(string path)
//     {
//         var lines = File.ReadAllLines(path);

//         var totalCount = int.Parse(lines[0]);
//         var clients = new List<Client>();

//         for (int i = 1; i < lines.Length; i += 2)
//         {
//             var client = new Client(lines[i], lines[i + 1]);
//             Ingredients.UnionWith(client.Need);
//             Ingredients.UnionWith(client.Hate);

//             clients.Add(client);
//         }

//         Clients = clients;
//         Path = path;
//     }

//     public IReadOnlyList<Client> Clients { get; }
//     public HashSet<string> Ingredients { get; } = new();
//     public string Path { get; }
// }