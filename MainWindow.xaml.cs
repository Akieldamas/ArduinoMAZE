using ArduinoMAZE.Controller;
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

        List<string> Options = new List<string>(
            new string[] { "Manuel", "Aléatoire", "IA" }
        );

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

        public MainWindow()
        {
            InitializeComponent();
            manualController = new ManualController();
            CB_Options.ItemsSource = Options;
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

            if (selectedOption == "Manuel")
            {
                RunManual();
            }
            else if (selectedOption == "Aléatoire")
            {
                // Aléatoire
            }
            else if (selectedOption == "IA")
            {
                AIController aiController = new AIController();
                //  aiController.AILogic(mazeMatrix, playerRow = 1, playerCol = 1);
            }
            else
            {
                MessageBox.Show("Please choose a mode");
                return;
            }
        }

        bool KeyPressed = false;

        private async Task RunManual()
        {
            int[] Goal = { 4, 4 };
            KeyPressed = false;
            KeyDown += new KeyEventHandler(MainWindow_KeyDown);
            while (playerLocation != Goal)
            {
                await Task.Delay(250);
                if (KeyPressed)
                {
                    bool Decision = manualController.ManualLogic(mazeMatrix, playerLocation, playerDirection);
                    if (Decision)
                    {
                        previousLocation = new int[] { playerLocation[0], playerLocation[1] };
                        playerLocation = new int[] { playerLocation[0] + playerDirection[0], playerLocation[1] + playerDirection[1] };
                        // stop maze when reached goal location etc
                        UpdateMaze(playerLocation, previousLocation);
                    }
                }
                KeyPressed = false;

            }
            MessageBox.Show("Congratulations");

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

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    Rectangle rect = new Rectangle();
                    if (row == playerLocation[0] && col == playerLocation[1])
                    {
                        rect.Fill = Brushes.Blue;
                    }
                    if (row == previousLocation[0] && col == previousLocation[1])
                    {
                        rect.Fill = Brushes.White;
                    }
                    if (mazeMatrix[row, col] == "#")
                    {
                        rect.Fill = Brushes.Black;
                    }
                    if (mazeMatrix[row, col] == ".")
                    {
                        rect.Fill = Brushes.White;
                    }
                    if (mazeMatrix[row, col] == "P")
                    {
                        rect.Fill = Brushes.Blue;
                    }
                    if (mazeMatrix[row, col] == "G")
                    {
                        rect.Fill = Brushes.Red;
                    }
                    Grid.SetRow(rect, row);
                    Grid.SetColumn(rect, col);
                    Grid_Maze.Children.Add(rect);
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
                    if (mazeMatrix[row, col] == "#")
                    {
                        rect.Fill = Brushes.Black;
                    }
                    if (mazeMatrix[row, col] == ".")
                    {
                        rect.Fill = Brushes.White;
                    }
                    if (mazeMatrix[row, col] == "P")
                    {
                        rect.Fill = Brushes.Blue;
                    }
                    if (mazeMatrix[row, col] == "G")
                    {
                        rect.Fill = Brushes.Red;
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

        }

        private void BTN_Reset_Click(object sender, RoutedEventArgs e)
        {
            BTN_Reset.IsEnabled = false;
            BTN_Start.IsEnabled = true;
            BTN_Stop.IsEnabled = true;

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