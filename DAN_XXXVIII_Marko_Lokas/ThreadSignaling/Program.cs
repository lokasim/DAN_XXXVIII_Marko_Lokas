using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadSignaling
{
    class Program
    {
        public static readonly object locker = new object();
        public static int randomCounter = 0;
        public static List<int> TopRoutesList = new List<int>();
        public static List<int> TopTenRoutesList = new List<int>();
        public static List<int> ManagerTopTenRoutesList = new List<int>();
        public static EventWaitHandle route = new AutoResetEvent(false);
        public static EventWaitHandle manager = new AutoResetEvent(false);
        public static Thread sistemThread = new Thread(new ThreadStart(CreateRoutes));
        public static Thread managerThread = new Thread(new ThreadStart(ManagerMethod));

        static void Main(string[] args)
        {
            sistemThread.Start();
            Thread.Sleep(3000);
            managerThread.Start();



            Console.ReadKey();
        }

        public static void CreateRoutes()
        {
            Console.WriteLine("Random generate routes");
            Random randomRoutes = new Random();
            using (TextWriter tw = new StreamWriter(@"..\..\Routes.txt"))
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (i == 999)
                    {
                        tw.Write(randomRoutes.Next(1, 5001));
                        randomCounter++;
                    }
                    else
                    {
                        tw.WriteLine(randomRoutes.Next(1, 5001));
                        randomCounter++;
                    }
                }
            }
            Console.WriteLine("Completed random gnerated routes.");
            
            string[] routes = File.ReadAllLines(@"..\..\Routes.txt");
            foreach (var route in routes)
            {
                TopRoutesList.Add(Convert.ToInt32(route));
            }
            TopRoutesList.Sort();

            foreach (var item in TopRoutesList)
            {
                if (TopTenRoutesList.Count() == 10)
                {
                    break;
                }
                else if (TopTenRoutesList.Contains(item))
                {
                    continue;
                }
                else
                {
                    if (item % 3 == 0)
                    {
                        TopTenRoutesList.Add(item);
                    }
                }
            }

            Console.WriteLine("Manager informs the drivers.");
            Console.WriteLine("Unloading routes are selected");
            Console.Write("\nROutes:{\t");
            foreach (var topTen in TopTenRoutesList)
            {
                Console.Write(topTen + "\t");
            }
            Console.Write("}");
            Console.WriteLine();
            Console.WriteLine("Everyting is ready, loading can begin.");
        }

        public static void ManagerMethod()
        {
            if (TopTenRoutesList.Count != 10)
            {
                lock (locker)
                {
                    string[] routes = File.ReadAllLines(@"..\..\Routes.txt");
                    foreach (var route in routes)
                    {
                        ManagerTopTenRoutesList.Add(Convert.ToInt32(route));
                        if (ManagerTopTenRoutesList.Count == 10)
                        {
                            break;
                        }
                    }
                }
                Console.WriteLine("Manager informs the drivers.");
                Console.WriteLine("Unloading routes are selected");
                Console.Write("\nROutes:{\t");
                foreach (var topTen in ManagerTopTenRoutesList)
                {
                    Console.Write(topTen + "\t");
                }
                Console.Write("}");
                Console.WriteLine();
                Console.WriteLine("Everyting is ready, loading can begin.");
            }
        }
    }
}
