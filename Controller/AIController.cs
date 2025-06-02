using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;

namespace ArduinoMAZE.Controller
{
    public class AIController
    {
        int cameFrom = -1;
        public AIController()
        {}
        
        public double AIPrediction(int[] matrixAIMovement, int INPUT_SIZE, int HIDDEN_SIZE, double[,] weights_ih, double[,] weights_ho) 
        {
         //   MessageBox.Show(weights_ih[0, 20].ToString());
            // Calculate hidden layer outputs
            double[] PredictHidden = new double[HIDDEN_SIZE];
            double PredictOutput = 0;
            for (int j = 0; j < HIDDEN_SIZE; j++)
            {
                double sum = 0;
                for (int k = 0; k < INPUT_SIZE; k++)
                {
                    sum += matrixAIMovement[k] * weights_ih[j, k]; // Use 'row' instead of 'k'
                }

                PredictHidden[j] = Sigmoid(sum);
                PredictOutput += PredictHidden[j] * weights_ho[0, j];
            }

            PredictOutput = Sigmoid(PredictOutput);
        //    PredictOutput = Math.Round(PredictOutput, 2);

            return PredictOutput;
        }
        public double Sigmoid(double sum)
        {
            return 1 / (1 + Math.Exp(-sum));
        }

        public int[] GetState(string[,] mazeMatrix, int[] playerLocation, int[] previousLocation)
        {
            int[] state = new int[7];

            state[0] = playerLocation[1];
            state[1] = playerLocation[0];

            string[] AISurroundings = new string[4];
            AISurroundings[0] = mazeMatrix[playerLocation[0] - 1, playerLocation[1]]; // UP (ordre)
            AISurroundings[1] = mazeMatrix[playerLocation[0] + 1, playerLocation[1]]; // DOWN
            AISurroundings[2] = mazeMatrix[playerLocation[0], playerLocation[1] + 1]; // RIGHT
            AISurroundings[3] = mazeMatrix[playerLocation[0], playerLocation[1] - 1];

            for (int i = 3; i < 6; i++)
            {
                if (AISurroundings[i - 3] == "#")
                {
                    state[i] = 1;
                }
                else
                {
                    state[i] = 0;
                }
            }
            // Determine the direction the player came from
            int dy = playerLocation[0] - previousLocation[0]; // Y
            int dx = playerLocation[1] - previousLocation[1]; // X

            cameFrom = -1; // Default to undefined

            if (dy == -1 && dx == 0) cameFrom = 0; // Came from UP
            else if (dy == 1 && dx == 0) cameFrom = 1; // DOWN
            else if (dy == 0 && dx == 1) cameFrom = 2; // RIGHT
            else if (dy == 0 && dx == -1) cameFrom = 3; // LEFT


            state[6] = cameFrom;

            return state;
        }

    }
}
