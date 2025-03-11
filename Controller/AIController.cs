using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ArduinoMAZE.Controller
{
    public class AIController
    {
        string[,] GLOBALmazeMatrix;
        
        public AIController()
        {}
        
        public void AILogic(string[,] mazeMatrix, int[] playerLocation) 
        {
            GLOBALmazeMatrix = mazeMatrix;
        }
    }
}
