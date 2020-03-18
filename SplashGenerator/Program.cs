using System;
using System.Diagnostics;
using System.Windows;
using Newtonsoft.Json;

namespace SplashGenerator
{
    static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var input = Console.ReadLine();
                //MessageBox.Show(input);
                
                var parameters = JsonConvert.DeserializeObject<GeneratorParameters>(input);

                var stream = BitmapGenerator.Generate(parameters);

                ///MessageBox.Show("1");

                Console.Write(stream ?? @"|| error generating stream.");
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());

                Console.Write(@"|| " + ex);
            }

            //MessageBox.Show("2");
        }
    }
}
