using System;
using System.Linq;
using System.Windows;

namespace SplashGenerator
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // var input = Console.ReadLine();
                // MessageBox.Show("Generator");
                
                // var parameters = JsonConvert.DeserializeObject<GeneratorParameters>(input);

                var stream = BitmapGenerator.Generate(args[0], args[1], args.Skip(2));

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
