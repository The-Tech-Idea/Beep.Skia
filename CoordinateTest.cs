using System;

namespace Beep.Skia.Test
{
    class CoordinateTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing coordinate assignment...");

            // Test Button
            var button = new Beep.Skia.Components.Button();
            Console.WriteLine($"Button after creation: X={button.X}, Y={button.Y}");
            button.X = 100;
            button.Y = 150;
            Console.WriteLine($"Button after assignment: X={button.X}, Y={button.Y}");

            // Test Checkbox  
            var checkbox = new Beep.Skia.Components.Checkbox();
            Console.WriteLine($"Checkbox after creation: X={checkbox.X}, Y={checkbox.Y}");
            checkbox.X = 200;
            checkbox.Y = 250;
            Console.WriteLine($"Checkbox after assignment: X={checkbox.X}, Y={checkbox.Y}");

            Console.WriteLine("Test complete. Press any key...");
            Console.ReadKey();
        }
    }
}
