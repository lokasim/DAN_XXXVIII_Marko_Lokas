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
        public static readonly object lockerSecond = new object();
        public static int randomCounter = 0;
        public static List<int> TopRoutesList = new List<int>();
        public static List<int> TopTenRoutesList = new List<int>();
        public static List<int> ManagerTopTenRoutesList = new List<int>();
        public static EventWaitHandle route = new AutoResetEvent(false);
        public static EventWaitHandle manager = new AutoResetEvent(false);

        public static CountdownEvent countdown = new CountdownEvent(10);
        public static Barrier barrier = new Barrier(2);

        public static List<Thread> TruckThreadList = new List<Thread>();
        public static List<string> LoadingTimeListAllTrucks = new List<string>();
        public static List<string> AllTrucksWithRoutesList = new List<string>();

        public static Thread sistemThread = new Thread(new ThreadStart(CreateRoutes));
        public static Thread managerThread = new Thread(new ThreadStart(ManagerMethod));

        static void Main(string[] args)
        {
            sistemThread.Start();
            Thread.Sleep(3000);
            managerThread.Start();
            managerThread.Join();

            for (int i = 1; i < 11; i++)
            {
                if (i % 2 == 1)
                {
                    Thread thread = new Thread(new ThreadStart(FirstLoadingLine))
                    {
                        Name = "Truck-" + i
                    };
                    TruckThreadList.Add(thread);
                }
                else if (i % 2 == 0)
                {
                    Thread thread = new Thread(new ThreadStart(SecondLoadingLine))
                    {
                        Name = "Truck-" + i
                    };
                    TruckThreadList.Add(thread);
                }
            }

            foreach (var thread in TruckThreadList)
            {
                thread.Start();
            }
            countdown.Wait();

            Console.WriteLine("Assigning routes");

            int countDestination = 0;
            foreach (var truck in LoadingTimeListAllTrucks)
            {
                if (TopTenRoutesList.Count == 10)
                {
                    for (int i = 0; i < TopTenRoutesList.Count; i++)
                    {
                        string[] truckName = truck.Split(',');
                        AllTrucksWithRoutesList.Add(truck + "," + TopTenRoutesList[i]);
                        Console.WriteLine("The " + truckName[0] + " has been assigned route ID: " + TopTenRoutesList[i]);
                        countDestination++;
                    }
                    break;
                } 
                else if(ManagerTopTenRoutesList.Count == 10)
                {
                    for (int i = 0; i < ManagerTopTenRoutesList.Count; i++)
                    {
                        string[] truckName = truck.Split(',');
                        AllTrucksWithRoutesList.Add(truck + "," + ManagerTopTenRoutesList[i]);
                        Console.WriteLine("The " + truckName[0] + " has been assigned route ID: " + ManagerTopTenRoutesList[i]);
                        countDestination++;
                    }
                    break;
                }
            }



            Console.WriteLine("\nPress any key to exit app.");
            Console.ReadKey();
        }

        public static void FirstLoadingLine()
        {
            lock (locker)
            {
                Thread thread = Thread.CurrentThread;
                Console.WriteLine(thread.Name + " is being loaded");
                //Random loading time
                Random randomLoadingTime = new Random();
                int FirstTruckLoadingTime = randomLoadingTime.Next(500, 5001);
                Thread.Sleep(FirstTruckLoadingTime);
                //Loading duration message
                Console.WriteLine(thread.Name + " is loaded " + FirstTruckLoadingTime + " ms");
                LoadingTimeListAllTrucks.Add(thread.Name + "," + FirstTruckLoadingTime);
                barrier.SignalAndWait();
            }
            countdown.Signal();

        }

        public static void SecondLoadingLine()
        {
            lock (lockerSecond)
            {
                Thread thread = Thread.CurrentThread;
                Console.WriteLine(thread.Name + " is being loaded");
                //Random loading time
                Random randomLoadingTime = new Random();
                int SecondTruckLoadingTime = randomLoadingTime.Next(500, 5001);
                Thread.Sleep(SecondTruckLoadingTime);
                //Loading duration message
                Console.WriteLine(thread.Name + " is loaded " + SecondTruckLoadingTime + " ms");
                LoadingTimeListAllTrucks.Add(thread.Name + "," + SecondTruckLoadingTime);
                barrier.SignalAndWait();
            }
            countdown.Signal();

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
            Console.WriteLine("Please wait...");
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
