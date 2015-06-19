using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

public struct Dane
{
    //B&B
    public double granica; //czas dla B&B

    //mrowka
    public int ilosc_tur; // ilosc tur w ktorych wypuszczamy mrowki
    public int ilosc_mrowek; //ilosc mrowek wypuszczana na trasy w danej turze
    public double bazowy_feromon; //bazowa wartosc feromonu na kazdej z drog
    public double mnoznik_feromonu; //wielkosc feromonu zostawianego przez mrowke
    public double wsp_parowania; // jak szybko paruje feromonna danej trasie [0-1]
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
            const int ILOSC_POWT = 3;
            String sciezka = "", zapis = "";
            char tryb, algorytm;
            
            Console.Write("Sciezka do instancji problemu: ");
            sciezka = Console.ReadLine();
            Console.Clear();
            Console.Write("Gdzie zapisac wyniki?: ");
            zapis = Console.ReadLine();
            StreamWriter plik = new StreamWriter(new FileStream(zapis, FileMode.Append, FileAccess.Write));

            Stopwatch sw = new Stopwatch();
            List<List<int>> trasy = new List<List<int>>();
            Marsz marsz = new Marsz(sciezka); //dla jakiej instancji pomiary
            Dane dane = new Dane(); //struktura na parametry algorytmow

            dane.bazowy_feromon = 10000.0; //podstawowe parametry
            dane.ilosc_mrowek = 10;
            dane.ilosc_tur = 100;
            dane.mnoznik_feromonu = 1.0;
            dane.wsp_parowania = 0.3F;
            dane.losowo = false;
            dane.min = 0.001;
            dane.wychlodzenie = 0.99;
            dane.granica = 10;

            Console.Clear();
            Console.WriteLine("Tryb:\n1. Wybor kolejnych przesylek zgodnie z zawartoscia pliku.\n2. Losowy wybor kolejnych przesylek.\n3. Przesylki wybierane zgodnie z odlegloscia od magazynu (rosnaco).\n4. Wybor kolejnej najblizszej przesylki.");
            ConsoleKeyInfo wybor = Console.ReadKey(true);
            tryb = wybor.KeyChar;

            Console.Clear();
            Console.WriteLine("Algorytm:\n1. B&B.\n2. Mrowkowy.\n3. Mrowkowy Rownolegle.\n4. Symulowane wyzarzanie.");
            wybor = Console.ReadKey(true);
            algorytm = wybor.KeyChar;

            Console.Clear();
            Console.WriteLine("Parametry algorytmu:\n");
            if (algorytm == '1')
            {
                Console.Write("Maks. czas [s]: ");
                dane.granica = Convert.ToDouble(Console.ReadLine());
            }
            else if (algorytm == '2')
            {
                Console.Write("Ilosc tur: ");
                dane.ilosc_tur = Convert.ToInt32(Console.ReadLine());
                Console.Write("Ilosc mrowek: ");
                dane.ilosc_mrowek = Convert.ToInt32(Console.ReadLine());
                Console.Write("Bazowy feromon: ");
                dane.bazowy_feromon = Convert.ToDouble(Console.ReadLine());
                Console.Write("Mnoznik feromonu: ");
                dane.mnoznik_feromonu = Convert.ToDouble(Console.ReadLine());
                Console.Write("Wsp. parowania feromonu: ");
                dane.wsp_parowania = Convert.ToDouble(Console.ReadLine());
                Console.Write("Losowosc wyboru miasta startowego: ");
                dane.losowo = Convert.ToBoolean(Convert.ToInt32(Console.ReadLine()));
            }
            else if (algorytm == '3')
            {
                Console.Write("Ilosc tur: ");
                dane.ilosc_tur = Convert.ToInt32(Console.ReadLine());
                Console.Write("Ilosc mrowek: ");
                dane.ilosc_mrowek = Convert.ToInt32(Console.ReadLine());
                Console.Write("Bazowy feromon: ");
                dane.bazowy_feromon = Convert.ToDouble(Console.ReadLine());
                Console.Write("Mnoznik feromonu: ");
                dane.mnoznik_feromonu = Convert.ToDouble(Console.ReadLine());
                Console.Write("Wsp. parowania feromonu: ");
                dane.wsp_parowania = Convert.ToDouble(Console.ReadLine());
            }
            else if (algorytm == '4')
            {
                Console.Write("Wsp. wychladzania: ");
                dane.wychlodzenie = Convert.ToDouble(Console.ReadLine());
                Console.Write("Min. temperatura: ");
                dane.min = Convert.ToDouble(Console.ReadLine());
            }
            else //secret place
            {
                for (int i = 0; i < 4; i++) // dla kazdego trybu
                {
                    for (int j = 3; j >= 0; j--) // dla kazdego algorytmu
                    {
                        if (j == 3) //symulowane //.....................................................................................................
                        {
                            plik.WriteLine("Tryb" + ";" + "Algorytm" + ";" + "Sumaryczna odleglosc" + ";" + "Ilosc tras" + ";" + "Suma Wag" + ";" + "Odleglosc optymalna" + ";" + "Czas" + ";" + "Wsp. ochl" + ";" + "Temp. min");
                            double[] szybkosc_ochladzania = { 0.9, 0.99, 0.999, 0.9999 };
                            double[] temp_minimalne = { 0.001, 0.01, 0.1};
                            foreach (double a in szybkosc_ochladzania)
                            {
                                foreach (double b in temp_minimalne)
                                {
                                    sw.Reset();
                                    double wagi = 0, odleglosci = 0;
                                    for (int l = 0; l < ILOSC_POWT; l++)
                                    {
                                        
                                        sw.Start();
                                        trasy = marsz.sre(i, j, dane);
                                        sw.Stop();
                                        Console.Write(".");
                                        if (trasy != null)
                                        {
                                            
                                            for (int k = 0; k < trasy.Count; k++) // dla kazdej trasy
                                            {
                                                wagi += marsz.obliczWage(trasy[k]);
                                                odleglosci += marsz.obliczOdleglosc(trasy[k]);
                                            }
                                        }           
                                    }
                                    wagi /= ILOSC_POWT;
                                    odleglosci /= ILOSC_POWT;
                                    
                                    plik.Write(i + ";" + j + ";" + odleglosci + ";" + trasy.Count + ";" + wagi + ";" + marsz.getRozwiazanieOptymalne() + ";" + ((Double)sw.ElapsedMilliseconds / (1000 * ILOSC_POWT)) + ";" + a + ";" + b);
                                    plik.WriteLine("");
                                    plik.Flush();
                                }
                            }
                            
                        }
                        else if (j == 2) // mrowka P //.....................................................................................................
                        {
                            plik.Write("Tryb" + ";" + "Algorytm" + ";" + "Sumaryczna odleglosc" + ";" + "Ilosc tras" + ";" + "Suma Wag" + ";" + "Odleglosc optymalna" + ";" + "Czas");
                        }
                        else if (j == 1) // mrowka //.....................................................................................................
                        {

                        }
                        else if (j == 0) // B&B //.....................................................................................................
                        {

                        }
                    }
                }
                
            }
            if (algorytm != '5')
            {
                sw.Start();
                trasy = marsz.sre(tryb - '1', algorytm - '1', dane); // Tutaj podajemy jakim trybem i jakim algorytmem liczymy
                sw.Stop();

                if (trasy != null) // dla B&B jesli nie przekroczymy czasu pojdeynczej operacji
                {
                    double wagi = 0, odleglosci = 0;
                    for (int i = 0; i < trasy.Count; i++) // dla kazdej trasy
                    {
                        Console.WriteLine("Trasa {0}:", i + 1);
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

                    Console.WriteLine("\n\nWagi wszystkich przesylek: " + wagi);
                    plik.WriteLine("\n\nWagi wszystkich przesylek: " + wagi);
                    Console.WriteLine("Sumaryczna odleglosc: " + odleglosci);
                    plik.WriteLine("Sumaryczna odleglosc: " + odleglosci);
                    Console.WriteLine("Odleglosc optymalna: " + marsz.getRozwiazanieOptymalne());
                    plik.WriteLine("Odleglosc optymalna: " + marsz.getRozwiazanieOptymalne());
                    Console.WriteLine("Ilosc tras/pojazdow: " + trasy.Count);
                    plik.WriteLine("Ilosc tras/pojazdow: " + trasy.Count);
                    Console.WriteLine("Czas obliczen = {0} [s]", (Double)sw.ElapsedMilliseconds / 1000);
                    plik.WriteLine("Czas obliczen = {0} [s]", (Double)sw.ElapsedMilliseconds / 1000);
                    plik.WriteLine("");
                    plik.Flush();
                }
                else
                {
                    Console.WriteLine("Przekroczono czas obliczen!");
                }
                sw.Reset();
            }
	        System.Console.In.Read();
        }
    }
}
