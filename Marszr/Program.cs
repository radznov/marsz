using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Marszr
{
    class Program
    {
        static void Main(string[] args)
        {

          //  Console.WriteLine("\nFUNKCJE: \nSymulo:");
             Marsz marsz = new Marsz("ins.txt");
          //   marsz = new Marsz("ins.txt");
          //  //Marsz marsz = new Marsz("10b.txt");
          //   int[] przesylki2 = { 0, 1 , 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 , 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };//, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40 };//,41,42,43,44,45,46,47,48,49 };
          //  List<int> lista = new List<int>();
          //  lista = marsz.symulowane(przesylki2, 0.99999, 0.01);
          //  foreach (int liczba in lista)
          //  {
          //      Console.Write(liczba + " -> ");
          //  }
          //  Console.Write("\nOdleglosc: " + marsz.obliczOdleglosc(lista) + "\n");


          //  lista = marsz.branch(przesylki2, 10);
          //  Console.Write("\n\n");
          //  if(lista != null)
          //  foreach (int liczba in lista)
          //  {
          //      Console.Write(liczba + " -> ");
          //  }
          //  Console.Write("\nOdleglosc: " + marsz.obliczOdleglosc(lista) + "\n");

          ////  Marsz marsz = new Marsz("ins.txt");

            Stopwatch sw = new Stopwatch();

            

            
            List<List<int>> trasy = new List<List<int>>();
            //List<int> trasa;
           // while (true)
            {
                //for (int k = 0; k < 3; k++)
                {
                    //for (int j = 0; j < 4; j++)
                    {
                        sw.Start();

                        // ...

                        
                        trasy = marsz.sreMrowka(2, 1);
                        if (trasy != null)
                        {
                            
                            double wagi = 0, odleglosci = 0;
                            for (int i = 0; i < trasy.Count; i++)
                            {
                                Console.WriteLine(i + ".");
                                foreach (int przesylka in trasy[i])
                                {
                                    Console.Write(przesylka + " ->");
                                }
                                Console.WriteLine("\nSuma wag: " + marsz.obliczWage(trasy[i]));
                                wagi += marsz.obliczWage(trasy[i]);
                                Console.WriteLine("Odleglosc: " + marsz.obliczOdleglosc(trasy[i]));
                                odleglosci += marsz.obliczOdleglosc(trasy[i]);
                                Console.WriteLine(" ");
                            }
                        
                        Console.WriteLine("Suma wag: " + wagi);
                        Console.WriteLine("Suma Odleglosci: " + odleglosci);
                        }
                        sw.Stop();
                        Console.WriteLine("Elapsed={0}", sw.Elapsed);
                        Console.WriteLine("Elapsed={0}", (Double)sw.ElapsedMilliseconds/1000);
                    }
                }
            }
	        System.Console.In.Read();
        }
    }
}
