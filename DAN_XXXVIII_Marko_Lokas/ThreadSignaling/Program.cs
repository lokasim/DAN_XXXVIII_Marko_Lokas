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
        public static EventWaitHandle driver = new AutoResetEvent(false);
        public static EventWaitHandle destination = new AutoResetEvent(false);
        //First loading line
        public static EventWaitHandle firstLine = new AutoResetEvent(false);
        //Second loading line
        public static EventWaitHandle secondLine = new AutoResetEvent(false);
        public static List<string> TruckTimeDestinationList = new List<string>();

        public static CountdownEvent countdown = new CountdownEvent(10);
        public static CountdownEvent countdownRoute = new CountdownEvent(10);
        public static Barrier barrier = new Barrier(2);

        //Truck thread list
        public static List<Thread> TruckThreadList = new List<Thread>();
        //Destination thread list
        public static List<Thread> TruckThreadListDestination = new List<Thread>();
        //Name truck, loading time
        public static List<string> LoadingTimeListAllTrucks = new List<string>();
        //Name truck, loading time, routeID
        public static List<string> AllTrucksWithRoutesList = new List<string>();

        public static Thread sistemThread = new Thread(new ThreadStart(CreateRoutes));
        public static Thread managerThread = new Thread(new ThreadStart(ManagerMethod));

        static void Main(string[] args)
        {
            sistemThread.Start();
            Thread.Sleep(3000);
            managerThread.Start();
            managerThread.Join();

            //Created threads for trucks
            for (int i = 1; i < 11; i++)
            {
                if (i % 2 == 1)
                {
                    Thread thread = new Thread(new ThreadStart(FirstLoadingLine))
                    {
                        Name = "Truck-" + i
                    };
                    TruckThreadList.Add(thread);
                    firstLine.Set();
                }
                else if (i % 2 == 0)
                {
                    Thread thread = new Thread(new ThreadStart(SecondLoadingLine))
                    {
                        Name = "Truck-" + i
                    };
                    TruckThreadList.Add(thread);
                    secondLine.Set();
                }
            }

            //Start Trucks threads
            foreach (var thread in TruckThreadList)
            {
                thread.Start();
            }
            countdown.Wait();

            Console.WriteLine("\nAssigning routes\n");

            int countDestination = 0;
            foreach (var truck in LoadingTimeListAllTrucks)
            {
                if (TopTenRoutesList.Count == 10)
                {
                    for (int i = countDestination; i < TopTenRoutesList.Count;)
                    {
                        string[] truckName = truck.Split(',');
                        AllTrucksWithRoutesList.Add(truck + "," + TopTenRoutesList[i]);
                        Console.WriteLine("The " + truckName[0] + " has been assigned route ID: " + TopTenRoutesList[i]);
                        countDestination++;
                        break;
                    }

                }
                else if (ManagerTopTenRoutesList.Count == 10)
                {
                    for (int i = countDestination; i < ManagerTopTenRoutesList.Count;)
                    {
                        string[] truckName = truck.Split(',');
                        AllTrucksWithRoutesList.Add(truck + "," + ManagerTopTenRoutesList[i]);
                        Console.WriteLine("The " + truckName[0] + " has been assigned route ID: " + ManagerTopTenRoutesList[i]);
                        countDestination++;
                        break;
                    }
                }
            }

            //Delivery threads
            for (int i = 1; i < 11; i++)
            {
                Thread thread = new Thread(new ThreadStart(DestinationWaiting))
                {
                    Name = "Truck-" + i
                };
                TruckThreadListDestination.Add(thread);
                destination.Set();
            }
            //Start delivery threads
            foreach (var thread in TruckThreadListDestination)
            {
                thread.Start();
            }
            countdownRoute.Wait();
            
            Console.WriteLine("\nPress any key to exit app.");
            Console.ReadKey();
        }

        /// <summary>
        /// The method in charge of notifying the destination that 
        /// the goods have been delivered. prints a message as to 
        /// whether the goods have been delivered and the time of 
        /// unloading or that the delivery has been canceled
        /// </summary>
        public static void DestinationWaiting()
        {
            Thread thread = Thread.CurrentThread;
            destination.WaitOne();

            foreach (var item in AllTrucksWithRoutesList)
            {
                string[] trucks = item.Split(',');
                //prints data for the current thread
                if (thread.Name == trucks[0])
                {
                    Console.Write("\n" + new string('-', 50));
                    //To generate different delivery times
                    Thread.Sleep(10);
                    Console.WriteLine("\n" + trucks[0]);
                    Console.WriteLine("\nYou can expect delivery between 500 and 5000ms...");
                    Console.WriteLine("\nTransport to the destination... RouteID: " + trucks[2]);

                    //Randomly generated delivery time
                    int randomDeliveryTime = RandomTransportTime();
                    //If the delivery time is longer than 3 seconds, delivery is canceled
                    if (randomDeliveryTime > 3000)
                    {
                        Thread.Sleep(3000);
                    }
                    //If the delivery time is less than 3 seconds
                    else
                    {
                        Thread.Sleep(randomDeliveryTime);
                    }
                    //Print a message about the truck, loading time, route ID and estimated delivery time
                    Console.WriteLine("\n" + trucks[0] + "\nTime Loading: " + trucks[1] + "\nRoutes ID: " + trucks[2] + "\nDelivery time: " + randomDeliveryTime);
                    TruckTimeDestinationList.Add(trucks[0] + "," + trucks[1] + "," + trucks[2] + "," + randomDeliveryTime);
                    //When the order is canceled, due to delivery delay
                    if (randomDeliveryTime > 3000)
                    {
                        PrintTruckCanceled();
                        Console.WriteLine("The truck returns to base");
                        Thread.Sleep(3000);
                        Console.WriteLine("The truck is back");
                    }
                    //When the delivery is made on time
                    else
                    {
                        PrintTruckDelivered();
                        int unloading = Convert.ToInt32(trucks[1]);
                        Console.WriteLine("Unloading in progress...");
                        Thread.Sleep(Convert.ToInt16(unloading / 1.5));
                        //Print duration of unloading
                        Console.WriteLine("Time Unloading: " + String.Format("{0:0.00}", unloading / 1.5));
                    }
                    Console.Write("\n" + new string('-', 50));
                    countdownRoute.Signal();
                }
            }
            destination.Set();
        }

        /// <summary>
        /// Method that determines the duration of transport to the destination
        /// </summary>
        /// <returns></returns>
        public static int RandomTransportTime()
        {
            Random random = new Random();
            return random.Next(500, 5001);
        }

        /// <summary>
        /// Method for simulating loading on the first loading line
        /// </summary>
        public static void FirstLoadingLine()
        {
            firstLine.WaitOne();
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
            firstLine.Set();
            countdown.Signal();
        }

        /// <summary>
        /// Method for simulating loading on another loading line
        /// </summary>
        public static void SecondLoadingLine()
        {
            secondLine.WaitOne();
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
            secondLine.Set();
            countdown.Signal();
        }

        /// <summary>
        /// Method for creating and writing them to a txt file, 
        /// as well as selecting the 10 best routes
        /// </summary>
        public static void CreateRoutes()
        {
            //1000 randomly generated numbers and file entry
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

            //Read routes from a file and select the top 10
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

        /// <summary>
        /// If the system does not specify routes, the manager 
        /// determines when 3 seconds have elapsed
        /// </summary>
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

        /// <summary>
        /// Print that the delivery was successful
        /// </summary>
        public static void PrintTruckDelivered()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  _____________  |__\\");
            Console.WriteLine(" | Lokas-trans | |__|");
            Console.WriteLine(" |_____________|_|__|");
            Console.WriteLine("   * * *     * *   *");
            Console.WriteLine("Delivery status: Delivered");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Print that the delivery failed
        /// </summary>
        public static void PrintTruckCanceled()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  _____________  |__\\");
            Console.WriteLine(" | Lokas-trans | |__|");
            Console.WriteLine(" |_____________|_|__|");
            Console.WriteLine("   * * *     * *   *");
            Console.WriteLine("Delivery status: Canceled");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
