using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArduinoMAZE.Controller
{
    public class JsonFilter
    {
        public JsonFilter()
        { }

        int GLOBALweightSize = 0;

        public double[,] FilterMatrixString(string ReceivedArray)
        {
            string[] StringArray = ReceivedArray.Split("],");

            int rows = StringArray.Length; // Determine number of rows
            int cols = StringArray[0].Split(',').Length; // Assume all rows have the same number of columns

            double[,] array = new double[rows, cols]; // Initialize matrix

            for (int i = 0; i < StringArray.Count(); i++)
            {
                StringArray[i] = StringArray[i].Replace("[", "");

                if (StringArray[i].Contains("]"))
                {
                    StringArray[i] = StringArray[i].Replace("]", "");
                }

                string[] values = StringArray[i].Split(','); // Split into individual numbers
                GLOBALweightSize = values.Length;


                for (int j = 0; j < values.Length; j++)
                {
                    array[i, j] = double.Parse(values[j], CultureInfo.InvariantCulture); // Convert to double and store
                }
            }
            return array;
        }
        public int GetWeightSize()
        {
            return GLOBALweightSize;
        }
    }
}
