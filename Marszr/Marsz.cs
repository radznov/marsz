using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//instancje
//https://code.google.com/p/metavrp/source/browse/trunk/metavrp/instances/vrp/Golden,+Wasil,+Kelly+and+Chao+-+1998?spec=svn18&r=18#Golden%2C%20Wasil%2C%20Kelly%20and%20Chao%20-%201998%2FInstances

namespace Marszr
{
    class Marsz
    {
        private int liczbaKlientow;
        private int pojemnoscPojazdu;
        private int zasiegPojazdu;
        private Double rozwiazanieOptymalne; //sumaryczna trasa
        private class Punkt //punkt na mapie - oznacza przesyłkę do dostarczenia
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
           public Double getX(){return this.x;} //wsp. x
           public Double getY(){return this.y;} //wsp. y
           public Double getQ(){return this.q;} //waga
        }
        private List<Punkt> przesylka; //lista przesyłek
        private Punkt wspMagazynu; // położenie magazynu - punktu startowego i końcowego
        private Double[,] odleglosc; //macierz odleglosci - wygodniejsze (razem z magazynem)
        private Double[] waga; //inaczej wielkosc

 //.........................................................................................................................

        public Marsz(String nazwaPliku) //konstruktor tworzy nam instancje - wczytuje parametry, na których możemy pracować
        {
            String[] linie = System.IO.File.ReadAllLines(nazwaPliku);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var tabInf = linie[0].Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            this.liczbaKlientow = Int32.Parse(tabInf[0]);
            this.pojemnoscPojazdu = Int32.Parse(tabInf[1]);
            this.zasiegPojazdu = Int32.Parse(tabInf[2]);
            this.rozwiazanieOptymalne = Double.Parse(tabInf[4]);

            this.przesylka = new List<Punkt>();

            tabInf = linie[1].Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);  
            this.wspMagazynu = new Punkt(Double.Parse(tabInf[0]), Double.Parse(tabInf[1]), Double.Parse(tabInf[2]));

            for (int i = 2; i < linie.Length; ++i)
            {
               var tab = linie[i].Split(new String[] { " " }, StringSplitOptions.RemoveEmptyEntries);

               this.przesylka.Add(new Punkt(Double.Parse(tab[0]), Double.Parse(tab[1]), Double.Parse(tab[2])));
            }

            this.odleglosc = new Double[this.liczbaKlientow +1, this.liczbaKlientow +1]; // +magazyn

            List <Punkt> tablica = new List<Punkt>();
            tablica.Add(this.wspMagazynu);
            foreach (Punkt przesylka in this.przesylka)
            {
                tablica.Add(przesylka);
            }
            waga = new double[tablica.Count];
            int x = 0, y;
            foreach (Punkt przesylka1 in tablica)
            {
                y = 0;
                foreach (Punkt przesylka2 in tablica)
                {
                    double x1 = przesylka1.getX(),
                           x2 = przesylka2.getX(),
                           y1 = przesylka1.getY(),
                           y2 = przesylka2.getY();
                    
                    if(x != y)
                    {
                    odleglosc[x, y] = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * ( y1 - y2));
                    }
                    else
                    {
                        odleglosc[x, y] = double.MaxValue;
                    }
                    y++;
                }
                waga[x] = przesylka1.getQ();
                x++;
            }
        }

       // public  List<int[]> plecak(); //zwraca liste tablic przesylek(kazda tablica zawiera dodatkowo magazyn)

        public List<int> mrowka(int [] przesylka, int ilosc_tur, double bazowy_feromon, double mnoznik_feromonu, float wsp_parowania, int ilosc_mrowek) //dla zadanej tablicy przesylek (z magazynem) oraz parametrow zwraca kolejnosc dostarczenia przesylek
        {
            int ilosc_przesylek = przesylka.Length;

            double[,] odleglosc = new double[ilosc_przesylek, ilosc_przesylek]; // stworzenie lokalnej macierzy odleglosci
            int ind1 = 0, ind2;
            foreach (int przesylka1 in przesylka)
            {
                ind2 = 0;
                foreach (int przesylka2 in przesylka)
                {
                    odleglosc[ind1, ind2] = this.odleglosc[przesylka1, przesylka2];
                    ind2++;
                }
                ind1++;
            }
            
            const float alfa = 1.0F, beta = 3.0F; // alfa/beta (okreslaja parametry wyboru kolejnego miasta na trasie, "podobno" alfa najlepiej  = 1, beta <2,5>
            const int wartosc_pewna = 10000; // ogolnie 1 stanowi wartosc pewna, ze wzgledu na losowanie wartosci calkowitych - u nas ta jedynka bedzie 10 000 (mozliwa zmiana)
            double rozwiazanie = 99999999;
            double[,] feromon = new double[ilosc_przesylek, ilosc_przesylek]; //tablica przechowujaca ilosc feromonu na danej sciezce
            double[,] feromon_delta = new double[ilosc_przesylek, ilosc_przesylek]; //tablica z iloscia nowego feromonu, ktora dodajemy po powrocie wszystkich mrowek
            int[] dostepne_miasta = new int[ilosc_przesylek]; // tablica w ktorej bedziemy trzymac miasta dostepne do odwiedzenia dla mrowki
            int[] najlepsza_trasa = new int[ilosc_przesylek]; //najlepsza trasa

            double wartosc_odniesienia_feromonu = 0; //wartosc odniesienia, aby wiedziec mniej wiecej ile feromonu dodawac

            for (int i = 0; i < ilosc_przesylek; i++)  //inicjalizacja wszystkich sciezek bazowa iloscia feromonu
            {
                for (int j = 0; j < ilosc_przesylek; j++)
                {
                    feromon[i, j] = bazowy_feromon; //a tu wartosc feromonu

                    if (wartosc_odniesienia_feromonu < odleglosc[i, j] && i != j)
                    {
                        wartosc_odniesienia_feromonu = odleglosc[i, j];
                    }
                }
            }

            wartosc_odniesienia_feromonu *= ilosc_przesylek; // najgorsza (niemozliwa) odleglosc

            //###########################################################################################################################################
            for (int x = 0; x < ilosc_tur; x++) //glowna petla programowa
            {
                for (int i = 0; i < ilosc_przesylek; i++)  //reset tablicy z dodatkowym feromonem
                {
                    for (int j = 0; j < ilosc_przesylek; j++)
                    {
                        feromon_delta[i, j] = 0;
                    }
                }
                
                Random rand = new Random();
                //==========================================================================================================================================
                for (int y = 0; y < ilosc_mrowek; y++) //dajemy kazdej mrowce przejsc sie po miastach
                {
                    int miasto_startowe = rand.Next(0, ilosc_przesylek - 1); //losowanie miasta startowe
                    int IDmiast = ilosc_przesylek; //TO NIE ID tylko IloscDostepnychmiast! :)
                    int obecne_miasto = miasto_startowe, kolejne_miasto = -1;
                    for (int i = 0; i < ilosc_przesylek; i++) //wstepna inicjalizacja dostepnych miast
                    {
                        dostepne_miasta[i] = i;
                    }
                    int tmp; // zmienna do ustalenia kolejnosci (w sensie zamieniamy przy jej pomocy wartosci w tablicy - zwykle swap)
                    tmp = dostepne_miasta[miasto_startowe];
                    dostepne_miasta[miasto_startowe] = IDmiast - 1; // wyrzucenie miasta, do ktorego i tak wrocimy z listy (pod wyrzuceniem, rozumiem przeniesienia na koniec tablicy, do indeksow, do ktorych sie nie bedziemy odwolywac)
                    dostepne_miasta[IDmiast - 1] = tmp;
                    IDmiast--; // jedno miasto pooszloooo (krok wyzej)

                    double dlugosc_trasy = 0; //dlugosc trasy wyliczona dla tej mrowki
                    //...........................................................................................................................................
                    for (int z = 0; z < ilosc_przesylek - 1; z++) // tutaj patrzymy jaka trase sobie wybrala 
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

                        for (int i = ilosc_przesylek - 1; i >= 0; i--)
                        {
                            najlepsza_trasa[i] = dostepne_miasta[i];
                        }
                    }
                    //...........................................................................................................................................
                    float dodatek_feromonu = (int)((1.0 / dlugosc_trasy) * wartosc_odniesienia_feromonu * mnoznik_feromonu); //okreslamy ile feromonu dodajemy

                    for (int i = ilosc_przesylek - 1; i > 0; i--) // dodanie feromonu do tablicy
                    {
                        feromon_delta[dostepne_miasta[i], dostepne_miasta[i - 1]] += dodatek_feromonu;
                    }
                    feromon_delta[dostepne_miasta[0], miasto_startowe] += dodatek_feromonu;

                    for (int i = ilosc_przesylek - 1; i > 0; i--) //wyroznienie dodatkowo najlepszej trasy
                    {
                        feromon_delta[najlepsza_trasa[i], najlepsza_trasa[i - 1]] += dodatek_feromonu;
                    }
                    feromon_delta[najlepsza_trasa[0], najlepsza_trasa[ilosc_przesylek - 1]] += dodatek_feromonu;

                }
                //==========================================================================================================================================
                for (int i = 0; i < ilosc_przesylek; i++)  //parowanie feromonu i dodanie nowego od mrowek
                {
                    for (int j = 0; j < ilosc_przesylek; j++)
                    {
                        feromon[i, j] *= (1 - wsp_parowania); //szybkosc parowania
                        feromon[i, j] += feromon_delta[i, j];
                    }
                }
            }
            //###########################################################################################################################################

            List<int> lista = new List<int>();
            foreach (int element in najlepsza_trasa)
            {
                lista.Add(przesylka[element]);
            }
            return lista;
        }

        public double obliczOdleglosc(List <int> trasa) //odleglosc dla zadanej trasy
        {
            double suma = 0;
            for(int i = 0 ; i < trasa.Count - 1 ; i++)
            {
                suma += this.odleglosc[trasa[i], trasa[i + 1]];
            }
            suma += this.odleglosc[trasa[trasa.Count - 1], trasa[0]];

            return suma;
        }

        public double obliczWage(List<int> przesylka) // suma wag dla zadanych przesylek
        {
            double suma = 0;
            for (int i = 0; i < przesylka.Count; i++)
            {
                suma += this.waga[przesylka[i]];
            }
            return suma;
        }

        public double[,] przesylkiTablica(List<int> przesylka) //dla zadanej listy przesylek tworzy macierz odleglosci (kolejnosc determinuje indeksy)
        {
            double[,] tablica = new double[przesylka.Count, przesylka.Count];
            int x = 0, y;
            foreach (int przesylka1 in przesylka)
            {
                y = 0;
                foreach (int przesylka2 in przesylka)
                {
                    tablica[x, y] = this.odleglosc[przesylka1, przesylka2];
                    y++;
                }
                x++;
            }

            return tablica;
        }

        public List<int> przesylkiTrasa(List<int> indeksy, List<int> przesylka) //dla zadanej listy indeksów, zwraca przesortowaną w kolejności dostarczenia listę przesyłek
        {
            List<int> lista = new List<int>();
            foreach (int element in indeksy)
            {
                lista.Add(przesylka[element]);
            }
            return lista;
        }

    }
}
