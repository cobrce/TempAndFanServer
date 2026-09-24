using System.ComponentModel;
using System.Diagnostics;

namespace ASCIIART
{
    public static class AsciiArt
    {
        // generated using https://patorjk.com/software/taag


        // value varries from 0 to 10 
        public static string ConvertRatioToGauge(float valueF, int max,bool vertical = true)
        {
            var value = (int)Math.Round(valueF);
            List<string> gauge = [];
            for (int i = 0; i < max; i++)
            {
                if (i < (max - value))
                    gauge.Add(vertical ? EmptyBar : EmptyBarH);
                else
                    gauge.Add(vertical ? OneBar : OneBarH);
            }
            if (!vertical)
                gauge.Reverse();
            return string.Join(vertical ? Environment.NewLine : "", gauge);
        }



        public static string ConvertFloatToArt(float value, int precision = 1)
        {
            int counter = 0;
            List<string> arts = [];
            
            if (value == 0)
            {
                counter = precision + 1;
                for (int i =0;i<counter;i++)
                    arts.Add(numbers[0]);
            }
            else
            {
                float upscaledF = value;
                for (int i = 0; i < precision; i++)
                {
                    upscaledF *= 10.0f;
                }

                int upscaled = (int)upscaledF;

                while (upscaled != 0)
                {
                    arts.Insert(0, ConvertSingleDigitToArt(upscaled % 10));
                    upscaled /= 10;
                    counter++;
                }
            }
            arts.Insert(counter - precision, dot);
            return ConcatArts([.. arts]);
        }
        public static string ConvertSingleDigitToArt(int digit)
        {
            return numbers[digit % 10];
        }
        public static string ConcatArts(string[] arts)
        {
            List<string[]> split = [];
            foreach (var art in arts)
                split.Add(art.Split(Environment.NewLine));


            List<string> concat = [];
            for (int i = 0; i < split[0].Length; i++)
            {
                string line = "";
                foreach(var art in split)
                {
                    if (i>= art.Length)
                        break;
                    line += art[i];
                }
                concat.Add(line);
            }
            return string.Join(Environment.NewLine,concat);
        }

        private static readonly string EmptyBar = "        ";
        private static readonly string OneBar = "■▄▄■";
        private static readonly string OneBarH = "▐";
        private static readonly string EmptyBarH = "░";
        private static readonly string dot =
"""
         
         
         
         
         
░▒▓██▓▒░ 
░▒▓██▓▒░ 
""";
        private static readonly string[] numbers =
        [
"""
░▒▓████████▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓████████▓▒░ 
""",
"""
   ░▒▓█▓▒░ 
░▒▓████▓▒░ 
   ░▒▓█▓▒░ 
   ░▒▓█▓▒░ 
   ░▒▓█▓▒░ 
   ░▒▓█▓▒░ 
   ░▒▓█▓▒░ 
""",
"""
░▒▓███████▓▒░  
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
 ░▒▓██████▓▒░  
░▒▓█▓▒░        
░▒▓█▓▒░        
░▒▓████████▓▒░ 
""",
"""
░▒▓███████▓▒░  
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
░▒▓███████▓▒░  
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
░▒▓███████▓▒░  
""",
"""
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓████████▓▒░ 
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
""",
"""
░▒▓████████▓▒░ 
░▒▓█▓▒░        
░▒▓█▓▒░        
░▒▓███████▓▒░  
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
░▒▓███████▓▒░  
""",
               
"""
 ░▒▓███████▓▒░ 
░▒▓█▓▒░        
░▒▓█▓▒░        
░▒▓███████▓▒░  
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
 ░▒▓██████▓▒░  
""",
"""
░▒▓████████▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
      ░▒▓█▓▒░  
      ░▒▓█▓▒░  
     ░▒▓█▓▒░   
     ░▒▓█▓▒░   
""",
"""
 ░▒▓██████▓▒░  
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
 ░▒▓██████▓▒░  
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
 ░▒▓██████▓▒░  
""",
"""
 ░▒▓██████▓▒░  
░▒▓█▓▒░░▒▓█▓▒░ 
░▒▓█▓▒░░▒▓█▓▒░ 
 ░▒▓███████▓▒░ 
       ░▒▓█▓▒░ 
       ░▒▓█▓▒░ 
 ░▒▓██████▓▒░  
"""
             
        ];

    }

}