using ArduinoMAZE.Controller;
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

namespace ArduinoMAZE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ManualController manualController;
        
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
        int[] previousLocation = { 1, 1 };
        int Score = 1000;
        bool isRunning;
        bool KeyPressed = false;

        public MainWindow()
        {
            InitializeComponent();
            manualController = new ManualController();
            CB_Options.ItemsSource = Options;
            TB_Score.Text = $"Score: {Score}";
            Grid_Maze.Children.Clear();
        }
        public void Load_Model()
        {
            BTN_Start.IsEnabled = true;
            InitializeMaze();
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

            switch(selectedOption)
            {
                case "Manuel":
                    isRunning = true;
                    RunManual();
                    break;
                case "Aléatoire":
                    // Aléatoire
                    break;
                case "IA":
                    AIController aiController = new AIController();
                    // aiController.AILogic(mazeMatrix, playerRow = 1, playerCol = 1);
                    break;
                case "":
                    MessageBox.Show("Please choose a mode");
                    return;
                case null:
                    MessageBox.Show("Please choose a mode");
                    return;
            }
        }

        private async Task RunManual()
        {
            KeyPressed = false;
            KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            while (isRunning)
            {
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
                        }
                        UpdateMaze(playerLocation, previousLocation);
                    }
                }
                KeyPressed = false;
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

        public void UpdateMaze(int[] playerLocation, int[] previousLocation)
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

            MessageBox.Show(mazeMatrix[4,4].ToString());
            MessageBox.Show(defaultMatrix[4,4].ToString());
            InitializeMaze();
        }

        private void BTN_Load_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = false;

            DBConnection_Window db = new DBConnection_Window();
            db.Show();

        }
    }
}