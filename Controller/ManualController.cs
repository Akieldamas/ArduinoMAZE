using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArduinoMAZE.Controller
{
    public class ManualController
    {
        public ManualController()
        {}

        public bool ManualLogic(string[,] mazeMatrix, int[] playerLocation, int[] playerDirection)
        {
            int goalPlayerRow = playerLocation[0] + playerDirection[0];
            int goalPlayerCol = playerLocation[1] + playerDirection[1];

            string goalPlayer = mazeMatrix[goalPlayerRow, goalPlayerCol];

            switch (goalPlayer)
            {
                case ".":
                    return true;
                case "#":
                    return false;
                case "G":
                    return true;
                default:
                    return false;
            }

        }
    }
}
