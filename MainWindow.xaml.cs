using ArduinoMAZE.Controller;
using ArduinoMAZE.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json;
using System.IO;

namespace ArduinoMAZE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ManualController manualController;
        DAO_API DAO_api;
        JsonFilter jsonFilter;
        AIController aiController;
        private readonly Random rand;

        //ObservableCollection est apparément mieux que List pour les bindings.
        ObservableCollection<string> Options = new ObservableCollection<string>(
            new string[] { "Manuel", "Aléatoire", "IA", "Reinforcement (Q-Learning)" }
        );


       

        string[,] defaultMatrix =
        {
            { "#", "#", "#", "#", "#", "#", "#", "#", "#", "#" },
            { "#", "P", ".", ".", ".", ".", ".", ".", ".", "#" },
            { "#", ".", "#", "#", ".", "#", "#", "#", ".", "#" },
            { "#", ".", "#", ".", ".", ".", ".", "#", ".", "#" },
            { "#", ".", ".", ".", "G", "#", ".", "#", ".", "#" },
            { "#", ".", "#", ".", "#", "#", ".", "#", ".", "#" },
            { "#", ".", "#", ".", ".", ".", ".", "#", ".", "#" },
            { "#", ".", "#", "#", "#", "#", "#", "#", ".", "#" },
            { "#", ".", ".", ".", ".", ".", ".", ".", ".", "#" },
            { "#", "#", "#", "#", "#", "#", "#", "#", "#", "#" },

        };

        string[,] mazeMatrix =
        {
            { "#", "#", "#", "#", "#", "#", "#", "#", "#", "#" },
            { "#", "P", ".", ".", ".", ".", ".", ".", ".", "#" },
            { "#", ".", "#", "#", ".", "#", "#", "#", ".", "#" },
            { "#", ".", "#", ".", ".", ".", ".", "#", ".", "#" },
            { "#", ".", ".", ".", "G", "#", ".", "#", ".", "#" },
            { "#", ".", "#", ".", "#", "#", ".", "#", ".", "#" },
            { "#", ".", "#", ".", ".", ".", ".", "#", ".", "#" },
            { "#", ".", "#", "#", "#", "#", "#", "#", ".", "#" },
            { "#", ".", ".", ".", ".", ".", ".", ".", ".", "#" },
            { "#", "#", "#", "#", "#", "#", "#", "#", "#", "#" },
        };

        int[] playerLocation = { 1, 1 };
        int[] playerDirection = { 0, 0 };
        int[] previousLocation = { 1, 1 };
        int Score = 0;
        bool isRunning;
        bool KeyPressed = false;

        double[,] weights_ih;
        double[,] weights_ho;
        int weights_Size;

        // parameters for reinforcement learning
        double alpha = 0.1; // learning rate
        double discount_factor = 0.9; // gamma
        
        double epsilon = 0.9;
        int generation = 0;

        int reward = -50;

        int games_count = 1;
        int max_games = 1000;
        int[] currentState = new int[6];
        List<int> validActions = new List<int>();
        Dictionary<string, double[]> QTable = new Dictionary<string, double[]>();
        Dictionary<string, int> visitCounts = new Dictionary<string, int>();

        public MainWindow()
        {
            InitializeComponent();
            manualController = new ManualController();
            jsonFilter = new JsonFilter();
            DAO_api = new DAO_API();
            aiController = new AIController();
            rand = new Random();
            CB_Options.ItemsSource = Options;
            TB_Score.Text = $"Score: {Score}";
            InitializeCB_Models();
            Grid_Maze.Children.Clear();
            InitializeMaze();
        }

        private async void InitializeCB_Models()
        {
            CB_Models.Items.Clear();
            CB_Models.ItemsSource = null;
            CB_Models.ItemsSource = await DAO_api.GetNomsModeles();
        }

        private async Task RunReinforcement()
        {
            generation += 1;
            TB_Generation.Text = $"Generation: {generation}";

            while (games_count <= max_games && isRunning)
            {
                await Task.Delay(100);

                currentState = aiController.GetState(mazeMatrix, playerLocation, previousLocation);
                string stateKey = string.Join(",", currentState);
                Debug.WriteLine("Current State: " + stateKey);
                validActions.Clear();
                for (int i = 2; i <= 5; i++)
                {
                    Debug.WriteLine("Current State[" + i + "]: " + currentState[i]);
                    if (currentState[i] == 0)
                    {
                        validActions.Add(i - 2);
                        Debug.WriteLine("Valid Action: " + i);

                    }
                }

               // initialise q values
                if (!QTable.ContainsKey(stateKey))
                    QTable[stateKey] = new double[] { 0, 0, 0, 0 };

                // choose action
                double randNumber = rand.NextDouble();
                int action = 0;

                if (randNumber <= epsilon)
                {
                    // Exploration: Choose random action from valid actions
                    //show valid actions
                    Debug.WriteLine("Valid Actions: " + string.Join(", ", validActions));
                    action = validActions[rand.Next(validActions.Count)];

                    Debug.WriteLine("Random Action: " + action);
                }
                else
                {
                    // Exploitation: Choose best action from Q-table
                    double[] qValues = QTable[stateKey];
                   
                    action = validActions.OrderByDescending(a => qValues[a]).First();
                    Debug.WriteLine("Best Action: " + action);
                }

                switch (action)
                {
                    case 0: // UP
                        playerDirection = new int[] { -1, 0 };
                        break;
                    case 1: // DOWN
                        playerDirection = new int[] { 1, 0 };
                        break;
                    case 2: // RIGHT
                        playerDirection = new int[] { 0, 1 };
                        break;
                    case 3: // LEFT
                        playerDirection = new int[] { 0, -1 };
                        break;
                }

                // show player direction
                Debug.WriteLine("Player Direction: " + string.Join(", ", playerDirection));

                string posKey = $"{playerLocation[0]},{playerLocation[1]}";
                if (!visitCounts.ContainsKey(posKey))
                    visitCounts[posKey] = 0;

                visitCounts[posKey]++;

                bool Decision = manualController.ManualLogic(mazeMatrix, playerLocation, playerDirection);
                if (!Decision)
                {
                    // Failed move = wall bump = heavy penalty
                    reward = -10;

                    // Still update Q-table to teach the agent that this was a bad idea
                    // la formule est differente, theres no S' (future state)
                    double oldQ = QTable[stateKey][action];
                    QTable[stateKey][action] = oldQ + alpha * (reward - oldQ);

                    Score += reward;
                    TB_Score.Text = $"Score: {Score}";
                    continue;
                }
                if (visitCounts[posKey] > 5)
                {
                    // If the player has visited this position too many times, reset the game
                    reward = -20; // heavy penalty for getting stuck

                    double oldQ = QTable[stateKey][action];
                    QTable[stateKey][action] = oldQ + alpha * (reward - oldQ);
                    Score += reward;
                    TB_Score.Text = $"Score: {Score}";
                    ResetMaze();
                    visitCounts.Clear();
                    generation += 1;
                    TB_Generation.Text = $"Generation: {generation}";
                    continue;
                }
                else if (visitCounts[posKey] == 1)
                {
                    reward = 2;
                }


                previousLocation = new int[] { playerLocation[0], playerLocation[1] };
                playerLocation = new int[] { playerLocation[0] + playerDirection[0], playerLocation[1] + playerDirection[1] };

                if (mazeMatrix[playerLocation[0], playerLocation[1]] == ".")
                {
                    reward = -1; // penalty for moving to an empty space
                }
                else
                {
                    reward = 50; // reward for reaching the goal
                    // Task.Run() // is used to avoid messageobx blocking the code (var task fixed the issue)
                    generation += 1;
                    TB_Generation.Text = $"Generation: {generation}";
                    visitCounts.Clear();
                    ResetMaze();
                }

                UpdateQValue(stateKey, action, playerLocation);

                // epsilon decay
                epsilon = Math.Max(0.01, epsilon * 0.995);

                games_count++;
                Debug.WriteLine("Games Count: " + games_count);

                Score += reward;
                TB_Score.Text = $"Score: {Score}";
                UpdateMaze();
            }
        }
        private void UpdateQValue(string stateKey, int action, int[] nextPlayerLocation)
        {
            int[] nextState = aiController.GetState(mazeMatrix, nextPlayerLocation, previousLocation);
            string nextKey = string.Join(",", nextState);

            if (!QTable.ContainsKey(nextKey))
                QTable[nextKey] = new double[] { 0, 0, 0, 0 };

            double maxFutureQ = QTable[nextKey].Max();
            double currentQ = QTable[stateKey][action];
            QTable[stateKey][action] += alpha * (reward + discount_factor * maxFutureQ - currentQ);
        }

        private async Task RunAI() // AI mode
        {
            while (isRunning)
            {
                // get the surroundings
                int[] IntAISurroundings = new int[7];

                if (previousLocation[1] == 0)
                    IntAISurroundings[0] = 0;
                else IntAISurroundings[0] = playerLocation[1] - previousLocation[1]; // {1,2} = [Y,X]  if [X,Y] then [2,1] cus coding logic is inversed

                if (previousLocation[0] == 0)
                    IntAISurroundings[1] = 0;
                else IntAISurroundings[1] = playerLocation[0] - previousLocation[0];

                IntAISurroundings[2] = 0; // ESC (always zero)

                string[] AISurroundings = new string[4];
                AISurroundings[0] = mazeMatrix[playerLocation[0] - 1, playerLocation[1]]; // UP (ordre)
                AISurroundings[1] = mazeMatrix[playerLocation[0] + 1, playerLocation[1]]; // DOWN
                AISurroundings[2] = mazeMatrix[playerLocation[0], playerLocation[1] + 1]; // RIGHT
                AISurroundings[3] = mazeMatrix[playerLocation[0], playerLocation[1] - 1]; // LEFT

                for (int i = 3; i < 7; i++)
                {
                    if (AISurroundings[i - 3] == "#")
                    {
                        IntAISurroundings[i] = 1;
                    }
                    else
                    {
                        IntAISurroundings[i] = 0;
                    }
                }
                Debug.WriteLine("IntAISurroundings: " + string.Join(", ", IntAISurroundings));
                double output = aiController.AIPrediction(IntAISurroundings, IntAISurroundings.Length, weights_Size, weights_ih, weights_ho);
                Debug.WriteLine("Output: " + output);

                if (output > 0.9)
                {
                    playerDirection = new int[] { -1, 0 };

                }
                else if (output > 0.7)
                {
                    // gauche
                    playerDirection = new int[] { 0, -1 };
                }
                else if (output > 0.4)
                {
                    playerDirection = new int[] { 1, 0 };
                }
                else if (output > 0.15)
                {
                    playerDirection = new int[] { 0, 1 };
                }
                else
                {
                    Random rand = new Random();
                    int[,] directions = { { -1, 0 }, { 1, 0 }, { 0, 1 }, { 0, -1 } };
                    int randomDirection = rand.Next(0, 4);
                    playerDirection = new int[] { directions[randomDirection, 0], directions[randomDirection, 1] };
                }

                bool Decision = manualController.ManualLogic(mazeMatrix, playerLocation, playerDirection);

                if (Decision)
                {
                    Score -= 50;
                    TB_Score.Text = $"Score: {Score}";

                    previousLocation = new int[] { playerLocation[0], playerLocation[1] };
                    playerLocation = new int[] { playerLocation[0] + playerDirection[0], playerLocation[1] + playerDirection[1] };
                    if (mazeMatrix[playerLocation[0], playerLocation[1]] == "G")
                    {
                        // Task.Run() // is used to avoid messageobx blocking the code (var task fixed the issue)
                        var task = Task.Run(() => MessageBox.Show("You won!"));
                        isRunning = false;

                        BTN_Start.IsEnabled = false;
                        BTN_Stop.IsEnabled = false;
                        BTN_Reset.IsEnabled = true;
                        BTN_Load.IsEnabled = false;
                    }
                    UpdateMaze();
                }
                await Task.Delay(250);
            }
        }

        private async Task RunAleatoire() // Random mode, the square moves randomly on its own
        {
            while (isRunning)
            {
                await Task.Delay(150);
                // get the surroundings
                int[] surroundings = new int[4];
                int[,] directions = { { -1, 0 }, { 1, 0 }, { 0, 1 }, { 0, -1 } }; // up down right left

                // loops through all 4 possible movement
                for (int i = 0; i < 4; i++)
                {
                    // check if the player can move in that direction
                    int newX = playerLocation[0] + directions[i, 0];
                    int newY = playerLocation[1] + directions[i, 1];

                    // check if the player is in the maze's bounds
                    if (newX >= 0 && newX < mazeMatrix.GetLength(0) && newY >= 0 && newY < mazeMatrix.GetLength(1))
                    {
                        if (mazeMatrix[newX, newY] == "#")
                            surroundings[i] = 1;
                        else
                            surroundings[i] = 0;
                    }
                    else
                        surroundings[i] = 1;
                }

                // get a random direction based on the surroundings and available paths
                int randomDirection = 0;

                do
                { randomDirection = new Random().Next(0, 4); }
                while (surroundings[randomDirection] != 0);

                Score -= 50;
                TB_Score.Text = $"Score: {Score}";

                previousLocation = new int[] { playerLocation[0], playerLocation[1] };
                playerLocation = new int[] { playerLocation[0] + directions[randomDirection, 0], playerLocation[1] + directions[randomDirection, 1] };
                if (mazeMatrix[playerLocation[0], playerLocation[1]] == "G")
                {
                    // Task.Run() est utilisé pour eviter que messagebox bloque le code (var task a fix le fait que ca marchait pas)
                    var task = Task.Run(() => MessageBox.Show("You won!"));
                    isRunning = false;

                    BTN_Start.IsEnabled = false;
                    BTN_Stop.IsEnabled = false;
                    BTN_Reset.IsEnabled = true;
                    BTN_Load.IsEnabled = false;
                }

                UpdateMaze();
            }
        }
        private async Task RunManual() // Manual mode, played with the arrow keys
        {
            KeyPressed = false;
            KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            while (isRunning)
            {
                if (isRunning == false) break;
                Debug.WriteLine("{ " + playerLocation[0] + " , " + playerLocation[1] + " }");
                Debug.WriteLine("Running: " + isRunning);
                await Task.Delay(100);
                if (KeyPressed)
                {
                    bool Decision = manualController.ManualLogic(mazeMatrix, playerLocation, playerDirection);
                    if (Decision)
                    {
                        Score -= 50;
                        TB_Score.Text = $"Score: {Score}";

                        previousLocation = new int[] { playerLocation[0], playerLocation[1] };
                        playerLocation = new int[] { playerLocation[0] + playerDirection[0], playerLocation[1] + playerDirection[1] };
                        if (mazeMatrix[playerLocation[0], playerLocation[1]] == "G")
                        {
                            // Task.Run() est utilisé pour eviter que messagebox bloque le code (var task a fix le fait que ca marchait pas)
                            var task = Task.Run(() => MessageBox.Show("You won!"));
                            isRunning = false;

                            BTN_Start.IsEnabled = false;
                            BTN_Stop.IsEnabled = false;
                            BTN_Reset.IsEnabled = true;
                            BTN_Load.IsEnabled = false;
                        }
                        UpdateMaze();
                    }
                }
                KeyPressed = false;
            }
        }

        public void InitializeMaze() // Redraws the entire maze
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    Rectangle rect = new Rectangle();
                    switch (mazeMatrix[row, col])
                    {
                        case "#":
                            rect.Fill = Brushes.Black;
                            break;
                        case ".":
                            rect.Fill = Brushes.Gray;
                            break;
                        case "P":
                            rect.Fill = Brushes.Blue;
                            break;
                        case "G":
                            rect.Fill = Brushes.Red;
                            break;
                    }

                    Grid.SetRow(rect, row);
                    Grid.SetColumn(rect, col);
                    Grid_Maze.Children.Add(rect);
                }
            }
        }

        public void UpdateMaze() // REDRAW THE MAZE based on changes in the player's location
        {
            mazeMatrix[playerLocation[0], playerLocation[1]] = "P";
            mazeMatrix[previousLocation[0], previousLocation[1]] = ".";

            foreach (UIElement child in Grid_Maze.Children)
            {
                if (child is Rectangle rect)
                {
                    int row = Grid.GetRow(rect);
                    int col = Grid.GetColumn(rect);

                    if (row == playerLocation[0] && col == playerLocation[1])
                    {
                        rect.Fill = Brushes.Blue;
                    }
                    else if (row == previousLocation[0] && col == previousLocation[1])
                    {
                        rect.Fill = Brushes.Gray;
                    }
                }
            }
        }
        private void BTN_Stop_Click(object sender, RoutedEventArgs e)
        {
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = false;
            BTN_Reset.IsEnabled = true;
            BTN_Load.IsEnabled = true;

            isRunning = false;
            Console.WriteLine("Saving to: " + Directory.GetCurrentDirectory());

            if (QTable.Count > 0)
            {
                try
                {
                    // Save QTable as JSON
                    string json = JsonSerializer.Serialize(QTable, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText("QTable.json", json);

                    // Save QTable as CSV
                    var sb = new StringBuilder();
                    foreach (var entry in QTable)
                    {
                        string key = entry.Key;
                        string values = string.Join(",", entry.Value);
                        sb.AppendLine($"{key},{values}");
                    }

                    File.WriteAllText("QTable.csv", sb.ToString());
                    Console.WriteLine("QTable.csv successfully saved!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to save CSV: " + ex.Message);
                }
            }

        }
        private void BTN_Reset_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = true;

            ResetMaze();
        }

        private void ResetMaze()
        {
            playerLocation = new int[] { 1, 1 };
            playerDirection = new int[] { 0, 0 };
            previousLocation = new int[] { 1, 1 };
            Score = 1000;
            TB_Score.Text = $"Score: {Score}";
            mazeMatrix = (string[,])defaultMatrix.Clone();
            games_count = 1;
            visitCounts.Clear();
            currentState = new int[6];
            validActions.Clear();
            reward = 0;
            InitializeMaze();
        }

        private async void BTN_Load_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = false;

            if (CB_Models.SelectedItem == null)
            {
                MessageBox.Show("Please choose a model");
                return;
            }

            var reponse = await DAO_api.getModelByName(CB_Models.SelectedItem.ToString());

            if (reponse != null)
            {
                weights_ih = jsonFilter.FilterMatrixString(reponse.weights_ih);
                weights_ho = jsonFilter.FilterMatrixString(reponse.weights_ho);

                weights_Size = jsonFilter.GetWeightSize();
                MessageBox.Show("Model loaded");
            }
        }

        private async void BTN_Start_Click(object sender, RoutedEventArgs e)
        {
            if (CB_Options.SelectedItem == null)
            {
                MessageBox.Show("Please choose a mode");
                return;
            }

            BTN_Start.IsEnabled = false;
            BTN_Stop.IsEnabled = true;
            BTN_Reset.IsEnabled = false;
            BTN_Load.IsEnabled = false;

            string selectedOption = CB_Options.SelectedItem.ToString();

            switch (selectedOption)
            {
                case "Manuel":
                    isRunning = true;
                    RunManual();
                    break;
                case "Aléatoire":
                    isRunning = true;
                    RunAleatoire();
                    break;
                case "IA":
                    isRunning = true;
                    await RunAI(); // added await so the code waits for the function to finish
                    break;
                case "Reinforcement (Q-Learning)":
                    isRunning = true;
                    await RunReinforcement();
                    break;
                case "":
                    MessageBox.Show("Please choose a mode");
                    return;
                case null:
                    MessageBox.Show("Please choose a mode");
                    return;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    playerDirection = new int[] { -1, 0 };
                    KeyPressed = true;
                    break;
                case Key.Down:
                    playerDirection = new int[] { 1, 0 };
                    KeyPressed = true;
                    break;
                case Key.Left:
                    playerDirection = new int[] { 0, -1 };
                    KeyPressed = true;
                    break;
                case Key.Right:
                    playerDirection = new int[] { 0, 1 };
                    KeyPressed = true;
                    break;
            }
        }

        private void CB_Models_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            InitializeCB_Models();


        }
        private void BorderTop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BTN_ResetReinforcement_Click(object sender, RoutedEventArgs e)
        {
            alpha = 0.1; // learning rate  
            discount_factor = 0.9; // gamma  

            epsilon = 0.9;
            generation = 0;

            reward = -50;

            games_count = 1;
            max_games = 1000;
            currentState = new int[6];
            validActions = new List<int>();
            QTable = new Dictionary<string, double[]>();
            visitCounts = new Dictionary<string, int>();

            generation = 1;
            TB_Generation.Text = $"Generation: {generation}";
        }
    }
}
