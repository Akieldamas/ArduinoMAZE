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
        public AIController()
        {}
        
        public static double AIPrediction(int[] matrixAIMovement, int INPUT_SIZE, int HIDDEN_SIZE, double[,] weights_ih, double[,] weights_ho) 
        {
            MessageBox.Show(weights_ih[0, 20].ToString());
            // Calculate hidden layer outputs
            double[] PredictHidden = new double[HIDDEN_SIZE];
            double PredictOutput = 0;
            for (int j = 0; j < HIDDEN_SIZE; j++)
            {
                double sum = 0;
                for (int k = 0; k < INPUT_SIZE; k++)
                {
                    sum += Convert.ToInt32(matrixAIMovement[k]) * weights_ih[j, k]; // Use 'row' instead of 'k'
                }

                PredictHidden[j] = Sigmoid(sum);
                PredictOutput += PredictHidden[j] * weights_ho[0, j];
            }

            PredictOutput = Sigmoid(PredictOutput);
            PredictOutput = Math.Round(PredictOutput, 2);

            return PredictOutput;
        }
        public static double Sigmoid(double sum)
        {
            return 1 / (1 + Math.Exp(-sum));
        }
    }
}
