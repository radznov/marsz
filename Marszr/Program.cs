using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marszr
{
    class Program
    {

        int wczytaj(string nazwa_pliku, int [,] odleglosc) // wczytanie pliku z danymi
        {
            string[] lines = System.IO.File.ReadAllLines(nazwa_pliku);

            int ilosc_miast = Int32.Parse(lines[0]);
            odleglosc = new int[ilosc_miast, ilosc_miast]; //utworzenie dwuwymiarowej dynamicznej tablicy(wiersze), na podstawie odczytanych danych


            for (int i = 1; i < lines.Length; ++i)
            {
                var array = lines[i].Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < array.Length; j++)
                {
                    //Console.WriteLine("I: " + i + " J: " + j + " wartosc: " + Int32.Parse(array[j]));
                    odleglosc[i-1,j] = Int32.Parse(array[j]); // wczytywanie kolejnych wartosci do odpowiednich komorek tablicy
                   
                    if(odleglosc[i-1,j] == 0) // taak.. oszukanie, dla mrowkowego, zeby przez zero nie dzielic ;/ ale co zrobic.. bedzie wiekszy blad i tyle
                    {
                        odleglosc[i-1,j] = 1;
                    }
			
                }
            }
            Console.WriteLine("\n\nPomyslnie odczytano dane z pliku.");
            return ilosc_miast;
        } 

        static void Main(string[] args)
        {
            Program program = new Program();
            Marsz marsz = new Marsz("ins.txt");
	        string nazwa_pliku, wiersz, out_s; 
	        int ile_razy; // ile razy kazdy z testow bedzie wykonywany
            System.Console.In.ReadLine();
	        System.Console.Clear();

            System.Console.Out.WriteLine("nazwa pliku: ");
            nazwa_pliku = System.Console.In.ReadLine();
	        System.Console.Clear();

	        System.Console.Out.WriteLine("Do jakiego pliku zapisac wynik? (bez rozszerzenia): ");
            out_s = System.Console.In.ReadLine();
	        System.Console.Clear();

	        System.Console.Out.WriteLine("Ile razy wykonywac pomiary dla zadanej konfiguracji? ");
            ile_razy =Convert.ToInt32(System.Console.In.ReadLine());
	        System.Console.Clear();
	


		        int ilosc_miast = 0;

		        int [] ilosc_tur = new int [1]; // dodaj pozniej bo brak
                ilosc_tur[0] = 100;
		        int bazowa_ilosc_feromonu = 1000;
		        int mnoznik_dodatkowego_feromonu = 1;
		        float [] wsp_parowania = new float [1];
                wsp_parowania[0] = 0.4F;
		        float [] wsp_mrowek = new float [1];
                wsp_mrowek[0] = 0.4F;

		        double mx = (wsp_mrowek.Length) * (wsp_parowania.Length) * ((double)ilosc_tur.Length) * ile_razy , sredni_czas = 0.0, sredni_wynik = 0.0, najlepszy = 9999999999.999, najgorszy = 0.0;

                string[] lines = System.IO.File.ReadAllLines(nazwa_pliku);

                ilosc_miast = Int32.Parse(lines[0]);
                int[,] odleglosc = new int[ilosc_miast, ilosc_miast]; //utworzenie dwuwymiarowej dynamicznej tablicy(wiersze), na podstawie odczytanych danych


                for (int i = 1; i < lines.Length; ++i)
                {
                    var array = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < array.Length; j++)
                    {
                        //Console.WriteLine("I: " + i + " J: " + j + " wartosc: " + Int32.Parse(array[j]));
                        odleglosc[i - 1, j] = Int32.Parse(array[j]); // wczytywanie kolejnych wartosci do odpowiednich komorek tablicy

                        if (odleglosc[i - 1, j] == 0) // taak.. oszukanie, dla mrowkowego, zeby przez zero nie dzielic ;/ ale co zrobic.. bedzie wiekszy blad i tyle
                        {
                            odleglosc[i - 1, j] = 1;
                        }

                    }
                }
                Console.WriteLine("\n\nPomyslnie odczytano dane z pliku.");
                
		        
			        System.IO.StreamWriter wynik = new System.IO.StreamWriter(out_s + ".txt");
			        

			        //printf("Postep algorytmu: 00%%");


			        for (int i = 0; i < ilosc_tur.Length ; i++)
			        {

				        wynik.WriteLine("===================================================================================================");
                        wynik.WriteLine("Ilosc tur: ");
                        wynik.WriteLine(ilosc_tur[i]);

				        for(int k = 0 ; k < (wsp_parowania.Length) ; k++)
				        {
			              wynik.WriteLine("___________________________________________________________________________________________________");
				          wynik.WriteLine("Wspolczynnik szybkosci parowania: ");
				          wynik.WriteLine(wsp_parowania[k]);

				        for(int l = 0 ; l < (wsp_mrowek.Length) ; l++)
				        {
			              wynik.WriteLine("....................................................................................................");
				          wynik.WriteLine("Wspolczynnik ilosci mrowek do ilosci miast: ");
				          wynik.WriteLine(wsp_mrowek[l]);
				        for (int j = 0; j < ile_razy; j++)
				        {
					        int rozwiazanie = 9999999; //raczej nie bedzie gorsze od tego

 					        rozwiazanie = marsz.mrowka(odleglosc, ilosc_miast, ilosc_tur[i], bazowa_ilosc_feromonu, mnoznik_dodatkowego_feromonu, wsp_parowania[k], wsp_mrowek[l]);

					        if(rozwiazanie < najlepszy)
					        {
						        najlepszy = rozwiazanie;
					        }
					        if(rozwiazanie > najgorszy)
					        {
						        najgorszy = rozwiazanie;
					        }
				        }


				        System.Console.Out.WriteLine("\n\nIlosc miast: " + ilosc_miast);
				        System.Console.Out.WriteLine("Bazowa wartosc feromonu: " + bazowa_ilosc_feromonu);
				        System.Console.Out.WriteLine("Ilosc powtorzen kazdego z pomiarow: " + ile_razy);
				        System.Console.Out.WriteLine("Najkrotsza odleglosc: " + najlepszy);
				        System.Console.Out.WriteLine("Najgorsza odleglosc: " + najgorszy);

                        wynik.WriteLine("\n\nIlosc miast: " + ilosc_miast);
                        wynik.WriteLine("Bazowa wartosc feromonu: " + bazowa_ilosc_feromonu);
                        wynik.WriteLine("Ilosc powtorzen kazdego z pomiarow: " + ile_razy);

                        wynik.WriteLine("Najkrotsza odleglosc: " + najlepszy);
                        wynik.WriteLine("Najgorsza odleglosc: " + najgorszy + "\n\n");

				        najlepszy =  9999999999.999;
				        najgorszy = 0.0;
			        }
			        }
			        }
                    System.Console.Out.WriteLine("\n\nZakonczono obliczenia!\n\n");
			        wynik.Close(); // zamkniecie otwartego pliku


		
	        System.Console.In.Read();
        }
    }
}
