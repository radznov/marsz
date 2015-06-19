using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

public struct Dane
{
    //B&B
    public double granica; //czas dla B&B

    //mrowka
    public int ilosc_tur; // ilosc tur w ktorych wypuszczamy mrowki
    public int ilosc_mrowek; //ilosc mrowek wypuszczana na trasy w danej turze
    public double bazowy_feromon; //bazowa wartosc feromonu na kazdej z drog
    public double mnoznik_feromonu;
    public float wsp_parowania; // jak szybko paruje feromonna danej trasie [0-1]
    public Boolean losowo; // czy losowo wybierac miasto startowe dla kazdej mrowki

    //symulowane
    public double wychlodzenie; //jaka bedzie kolejna temperatura w odniesieniu do aktualnej [0-1]
    public double min; //minimalna temperatura ukladu
}

namespace Marszr
{
    class Program
    {
        static void Main(string[] args)
        {

            Marsz marsz = new Marsz("ins.txt"); //dla jakiej instancji pomiary
            Dane dane = new Dane(); //struktura na parametry algorytmow


            Stopwatch sw = new Stopwatch();   
            List<List<int>> trasy = new List<List<int>>();
           //List<int> trasa;
           // while (true)
            {
                //for (int k = 0; k < 3; k++)
                {
                    //for (int j = 0; j < 4; j++)
                    {
                        dane.bazowy_feromon = 10000.0;
                        dane.ilosc_mrowek = 10;
                        dane.ilosc_tur = 100;
                        dane.mnoznik_feromonu = 1.0;
                        dane.wsp_parowania = 0.3F;
                        dane.losowo = false;
                        dane.min = 0.001;
                        dane.wychlodzenie = 0.99;
                        dane.granica = 10;
                        sw.Start();

                        trasy = marsz.sre(3, 1, dane); // Tutaj podajemy jakim trybem i jakim algorytmem liczymy

                        if (trasy != null) // dla B&B jesli nie przekroczymy czasu pojdeynczej operacji
                        {                           
                            double wagi = 0, odleglosci = 0;
                            for (int i = 0; i < trasy.Count; i++) // dla kazdej trasy
                            {
                                Console.WriteLine("Trasa {0}:", i+1);
                                foreach (int przesylka in trasy[i])
                                {
                                    Console.Write(przesylka + " ->");
                                }
                                Console.Write(" 0");
                                Console.WriteLine("\nSuma wag: " + marsz.obliczWage(trasy[i]));
                                wagi += marsz.obliczWage(trasy[i]);
                                Console.WriteLine("Suma odleglosci: " + marsz.obliczOdleglosc(trasy[i]));
                                odleglosci += marsz.obliczOdleglosc(trasy[i]);
                                Console.WriteLine("");
                            }
                        sw.Stop();
                        Console.WriteLine("\n\nWagi wszystkich przesylek: " + wagi);
                        Console.WriteLine("Sumaryczna odleglosc: " + odleglosci);
                        Console.WriteLine("Odleglosc optymalna: " + marsz.getRozwiazanieOptymalne());
                        Console.WriteLine("Ilosc tras/pojazdow: " + trasy.Count);
                        } 
                        Console.WriteLine("Czas obliczen = {0} [s]", (Double)sw.ElapsedMilliseconds/1000);
                    }
                }
            }
	        System.Console.In.Read();
        }
    }
}
