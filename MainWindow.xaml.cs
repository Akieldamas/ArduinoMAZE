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

        //ObservableCollection est apparément mieux que List pour les bindings.
        ObservableCollection<string> Options = new ObservableCollection<string>(
            new string[] { "Manuel", "Aléatoire", "IA" }
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
        int[] previousLocation = { 0, 0 };
        int Score = 1000;
        bool isRunning;
        bool KeyPressed = false;

        double[,] weights_ih;
        double[,] weights_ho;
        int weights_Size;

        public MainWindow()
        {
            InitializeComponent();
            manualController = new ManualController();
            jsonFilter = new JsonFilter();
            DAO_api = new DAO_API();
            CB_Options.ItemsSource = Options;
            TB_Score.Text = $"Score: {Score}";
            InitializeCB_Models();
            Grid_Maze.Children.Clear();
        }

        private async void InitializeCB_Models()
        {
            CB_Models.ItemsSource = await DAO_api.GetNomsModeles();
        }

        private async Task RunAI()
        {
            while (isRunning)
            {
                // check tout les cotes + playerlocation et créer une table comme la table d'entrainement
                // faire la prediction et bouger l'IA
                
                // Remplir IntAISurroundings
                int[] IntAISurroundings = new int[7];

                if (previousLocation[1] == 0)
                    IntAISurroundings[0] = 0;
                else IntAISurroundings[0] = playerLocation[1] - previousLocation[1]; // { 1,2} = [Y,X]  si [X,Y] alors [2,1]
              
                if (previousLocation[0] == 0) 
                    IntAISurroundings[1] = 0;
                else IntAISurroundings[1] = playerLocation[0] - previousLocation[0];
                
                IntAISurroundings[2] = 0;

                string[] AISurroundings = new string[4];
                AISurroundings[0] = mazeMatrix[playerLocation[0] - 1, playerLocation[1]]; // Haut
                AISurroundings[1] = mazeMatrix[playerLocation[0] + 1, playerLocation[1]]; // Bas
                AISurroundings[2] = mazeMatrix[playerLocation[1] + 1, playerLocation[0]]; // Droite (RIGHT)
                AISurroundings[3] = mazeMatrix[playerLocation[1] - 1, playerLocation[0]]; // Gauche (LEFT)





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
                double output = AIController.AIPrediction(IntAISurroundings, IntAISurroundings.Length, weights_Size, weights_ih, weights_ho);
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
                else if (output > 0.45)
                {
                    playerDirection = new int[] { 1, 0 };
                }
                else if (output > 0.1)
                {
                    playerDirection = new int[] { 0, 1 };
                }
                else
                {
                    playerDirection = new int[] { 0, 0 };
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
                await Task.Delay(250);
            }
        }

        private async Task RunManual()
        {
            KeyPressed = false;
            KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            while (isRunning)
            {
                if (isRunning == false) break;
                Debug.WriteLine("{ " + playerLocation[0] + " , " + playerLocation[1] + " }");
                Debug.WriteLine("Running: "+ isRunning);
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

        private async Task RunAleatoire()
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
                while (surroundings[randomDirection] != 0)
                {
                    randomDirection = new System.Random().Next(0, 4);
                }

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
        public void InitializeMaze()
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    Rectangle rect = new Rectangle();
                    switch(mazeMatrix[row, col])
                    {
                        case "#":
                            rect.Fill = Brushes.Black;
                            break;
                        case ".":
                            rect.Fill = Brushes.White;
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
        
        public void UpdateMaze()
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
                        rect.Fill = Brushes.White;
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
        }
        private void BTN_Reset_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = true;

            playerLocation = new int[] { 1, 1 };
            playerDirection = new int[] { 0, 0 };
            previousLocation = new int[] { 1, 1 };

            Score = 1000;
            TB_Score.Text = $"Score: {Score}";
            mazeMatrix = (string[,])defaultMatrix.Clone();

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
                InitializeMaze();
                MessageBox.Show("Model loaded");
            }
        }

        private async void BTN_Start_Click(object sender, RoutedEventArgs e)
        {
            InitializeMaze();

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
                    await RunAI(); // Ajoute 'await' pour éviter que le code ne continue sans attendre la fin
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
    }
}