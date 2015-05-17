using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Marszr
{
    class Marsz
    {
        private int liczbaKlientow;
        private int pojemnoscPojazdu;
        private int zasiegPojazdu;
        private Double rozwiazanieOptymalne;
        private class Punkt
        {
            private Double x;
            private Double y;
            private Double q; //waga

           public Punkt(Double x, Double y, Double q)
            {
                this.x = x;
                this.y = y;
                this.q = q;
            }

           public Double getX()
           {
               return this.x;
           }
           public Double getY()
           {
               return this.y;
           }
           public Double getQ()
           {
               return this.q;
           }

        }
        List<Punkt> przesylka;
        Punkt wspMagazynu;
 //.........................................................................................................................

        public Marsz(String nazwaPliku)
        {
            String[] linie = System.IO.File.ReadAllLines(nazwaPliku);

            var tabInf = linie[0].Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            this.liczbaKlientow = Int32.Parse(tabInf[0]);
            this.pojemnoscPojazdu = Int32.Parse(tabInf[1]);
            this.zasiegPojazdu = Int32.Parse(tabInf[2]);
            //this.rozwiazanieOptymalne = Double.Parse(tabInf[4].Replace(".",","));
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            this.rozwiazanieOptymalne = Double.Parse(tabInf[4]);

            this.przesylka = new List<Punkt>();

           // System.Console.Out.WriteLine(this.liczbaKlientow + " " + this.pojemnoscPojazdu + " " + this.zasiegPojazdu + " " + this.rozwiazanieOptymalne );

            tabInf = linie[1].Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            this.wspMagazynu = new Punkt(Double.Parse(tabInf[0]), Double.Parse(tabInf[1]), Double.Parse(tabInf[2]));

            for (int i = 2; i < linie.Length; ++i)
            {
                var tab = linie[i].Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

               this.przesylka.Add(new Punkt(Double.Parse(tab[0]), Double.Parse(tab[1]), Double.Parse(tab[2])));
               System.Console.Out.WriteLine(Double.Parse(tab[0]) + " " + Double.Parse(tab[1]) + " " + Double.Parse(tab[2]));

            }
        }


        public int mrowka(int[,] odleglosc, int ilosc_miast, int ilosc_tur, double bazowy_feromon, double mnoznik_feromonu, float wsp_parowania, float wsp_mrowek) // odleglosc[,] - macierz sasiedztwa grafu, ilosc_miast - ilosc miast :D, rozwiazanie zwracane jako wynik wywolania
        {
            //ilosc tur w ktorych "kazda z mrowek wraca po jedzenie", czyli ile razy wypuszczamy mrowki
            const float alfa = 1.0F, beta = 3.0F; // alfa/beta (okreslaja parametry wyboru kolejnego miasta na trasie, "podobno" alfa najlepiej  = 1, beta <2,5>
            const int wartosc_pewna = 10000; // ogolnie 1 stanowi wartosc pewna, ze wzgledu na losowanie wartosci calkowitych - u nas ta jedynka bedzie 10 000 (mozliwa zmiana)
            int rozwiazanie = 99999999;
            double[,] feromon = new double[ilosc_miast, ilosc_miast]; //utworzenie tablicy przechowujacej ilosci feromonu na danej sciezce
            double[,] feromon_delta = new double[ilosc_miast, ilosc_miast]; //tablica z iloscia nowego feromonu, ktora dodajemy po powrocie wszystkich mroewk (dajmy te same warunki kazdej mrowce!)

            int wartosc_odniesienia_feromonu = 0; //wartosc odniesienia, aby wiedziec mniej wiecej ile feromonu dodawac

            for (int i = 0; i < ilosc_miast; i++)  //inicjalizacja wszystkich sciezek bazowa iloscia feromonu
            {
                for (int j = 0; j < ilosc_miast; j++)
                {
                    feromon[i, j] = bazowy_feromon; //a tu wartosc feromonu
                    //Console.WriteLine("I: " + i + " J: " + j + " wartosc: " + odleglosc[i, j]);
                    if (wartosc_odniesienia_feromonu < odleglosc[i, j] && i != j)
                    {
                        wartosc_odniesienia_feromonu = odleglosc[i, j];
                    }
                }
            }

            wartosc_odniesienia_feromonu *= ilosc_miast; //w zasadzie najgorsza mozliwa (w sumie to i prawie niemozliwa) dlogosc trasy

            int[] dostepne_miasta = new int[ilosc_miast]; // tablica w ktorej bedziemy trzymac miasta dostepne do odwiedzenia dla mrowki
            int[] najlepsza_trasa = new int[ilosc_miast]; //i zapamietanie najlepszej trasy
            int ilosc_mrowek = (int)((float)ilosc_miast * wsp_mrowek); // domyslnie liczba mrowek rowna ilosci miast (podobno jest to optymalne)

            //###########################################################################################################################################
            for (int x = 0; x < ilosc_tur; x++) //glowna petla programowa
            {
                for (int i = 0; i < ilosc_miast; i++)  //za kazdym razem czyscimy tablice z nowa iloscia feromonu do dodania
                {
                    for (int j = 0; j < ilosc_miast; j++)
                    {
                        feromon_delta[i, j] = 0;
                    }
                }
                //==========================================================================================================================================
                Random rand = new Random();
                for (int y = 0; y < ilosc_mrowek; y++) //dajemy kazdej mrowce przejsc sie po miastach
                {
                    int miasto_startowe = rand.Next(0, ilosc_miast - 1); //losowanie miasta startowe
                    int IDmiast = ilosc_miast; //TO NIE ID tylko IloscDostepnychmiast! :)
                    int obecne_miasto = miasto_startowe, kolejne_miasto = -1;
                    for (int i = 0; i < ilosc_miast; i++) //wstepna inicjalizacja dostepnych miast
                    {
                        dostepne_miasta[i] = i;
                    }
                    int tmp; // zmienna do ustalenia kolejnosci (w sensie zamieniamy przy jej pomocy wartosci w tablicy - zwykle swap)
                    tmp = dostepne_miasta[miasto_startowe];
                    dostepne_miasta[miasto_startowe] = IDmiast - 1; // wyrzucenie miasta, do ktorego i tak wrocimy z listy (pod wyrzuceniem, rozumiem przeniesienia na koniec tablicy, do indeksow, do ktorych sie nie bedziemy odwolywac)
                    dostepne_miasta[IDmiast - 1] = tmp;
                    IDmiast--; // jedno miasto pooszloooo (krok wyzej)

                    int dlugosc_trasy = 0; //dlugosc trasy wyliczona dla tej mrowki
                    //...........................................................................................................................................
                    for (int z = 0; z < ilosc_miast - 1; z++) // tutaj patrzymy jaka trase sobie wybrala 
                    {
                        double suma_we_wzorze = 0; //nazwa mowi za siebie
                        int wybrana_droga_prd = 0, suma_prawdopodobienstwa = 0; //wybrana wartosc prawdopodobienstwa
                        double[] prawdopodobienstwo = new double[IDmiast]; //tablica z prawdopodobienstwami wyboru kolejnego miasta

                        for (int i = 0; i < IDmiast; i++) //obliczenie jednej ze zmiennych potrzebnej do wybrania kolejnego miasta
                        {
                            suma_we_wzorze += (Math.Pow(feromon[obecne_miasto, dostepne_miasta[i]], alfa) * (1.0 / Math.Pow(odleglosc[obecne_miasto, dostepne_miasta[i]], beta)));
                        }

                        for (int i = 0; i < IDmiast; i++) //liczenie prawdopodobienstwa wyboru dla kazdego z miast (taki ladny wzor)
                        {
                            prawdopodobienstwo[i] = ((Math.Pow(feromon[obecne_miasto, dostepne_miasta[i]], alfa) * ((double)1 / (double)Math.Pow(odleglosc[obecne_miasto, dostepne_miasta[i]], beta))) / (double)(suma_we_wzorze)) * wartosc_pewna;
                            suma_prawdopodobienstwa += (int)prawdopodobienstwo[i];
                        }

                        wybrana_droga_prd = rand.Next(0, suma_prawdopodobienstwa - 1); // wybieramy liczbe z naszego przedzialu wartosci	

                        suma_prawdopodobienstwa = 0; //wykorzystamy juz istniejaca zmienna
                        for (int i = 0; i < IDmiast; i++)
                        {
                            suma_prawdopodobienstwa += (int)prawdopodobienstwo[i]; //dodajemy i czekamy, az przekroczymy
                            if (suma_prawdopodobienstwa >= wybrana_droga_prd) //jezeli doszlismy do poszukiwnego prawdopodobienstwa
                            {
                                kolejne_miasto = dostepne_miasta[i];
                                tmp = dostepne_miasta[i];
                                dostepne_miasta[i] = dostepne_miasta[IDmiast - 1]; //zmniejszenie ilosc miast
                                dostepne_miasta[IDmiast - 1] = tmp;
                                break;
                            }
                        }

                        if (kolejne_miasto == -1)
                        {
                            System.Console.Out.WriteLine("Nie wybrano kolejnego miasta!!");
                            System.Console.In.Read();
                        }
                        else
                        {
                            dlugosc_trasy += odleglosc[obecne_miasto, kolejne_miasto];
                        }
                        obecne_miasto = kolejne_miasto;

                        IDmiast--;
                        kolejne_miasto = -1;
                    }

                    dlugosc_trasy += odleglosc[dostepne_miasta[0], miasto_startowe]; // i dodanie dlugosci drogi powrotnej do punktu

                    if (rozwiazanie > dlugosc_trasy) //zapamietanie najlepszej trasy do tej pory
                    {
                        rozwiazanie = dlugosc_trasy;

                        for (int i = ilosc_miast - 1; i >= 0; i--)
                        {
                            najlepsza_trasa[i] = dostepne_miasta[i];
                        }
                    }
                    //...........................................................................................................................................
                    float dodatek_feromonu = (int)((1.0 / dlugosc_trasy) * wartosc_odniesienia_feromonu * mnoznik_feromonu); //okreslamy ile feromonu dodajemy

                    for (int i = ilosc_miast - 1; i > 0; i--) // dodanie feromonu do tablicy
                    {
                        feromon_delta[dostepne_miasta[i], dostepne_miasta[i - 1]] += dodatek_feromonu;
                    }
                    feromon_delta[dostepne_miasta[0], miasto_startowe] += dodatek_feromonu;

                    for (int i = ilosc_miast - 1; i > 0; i--) //wyroznienie dodatkowo najlepszej trasy
                    {
                        feromon_delta[najlepsza_trasa[i], najlepsza_trasa[i - 1]] += dodatek_feromonu;
                    }
                    feromon_delta[najlepsza_trasa[0], najlepsza_trasa[ilosc_miast - 1]] += dodatek_feromonu;

                }
                //============================================================================================================================================
                for (int i = 0; i < ilosc_miast; i++)  //parowanie feromonu i dodanie nowego od mrowek
                {
                    for (int j = 0; j < ilosc_miast; j++)
                    {
                        feromon[i, j] *= (1 - wsp_parowania); //szybkosc parowania
                        feromon[i, j] += feromon_delta[i, j];
                    }
                }
            }

            return rozwiazanie;
        }


    }
}
