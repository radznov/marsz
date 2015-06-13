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

        private Double gorna;
        private int[] cykl;
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
                    odleglosc[x, y] = Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * ( y1 - y2)));
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

        public List<List<int>> sre(int algorytm, int tryb)
        {
            List<List<int>> trasy = new List<List<int>>(); //wyliczone trasy
            List<int> przesylki = new List<int>(); //pojedyncza trasa

            Double pojemnosc = 0, waga = 0, odleglosc = 0;
            int index = 0;
            przesylki.Add(0);
            for (int i = 0; i < this.przesylka.Count; i++)
            {
                waga = this.przesylka[i].getQ();
                if (pojemnosc + waga < pojemnoscPojazdu)
                {
                    przesylki.Add(i + 1);
                    odleglosc = obliczOdleglosc(branch(przesylki.ToArray()));

                    if(odleglosc > this.zasiegPojazdu)
                    {
                        przesylki.RemoveAt(i+1);
                    }
                    else
                    {
                    }
                }
            }

            return null;
        }

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
                    int miasto_startowe = 0;// rand.Next(0, ilosc_przesylek - 1); //losowanie miasta startowego
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
            lista.Reverse(); // zeby zaczac od zera
            return lista;
        }

        public List<int> branch(int [] przesylka)
        {
            int ilosc_przesylek = przesylka.Length;
            this.cykl = new int[ilosc_przesylek];
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
            // funkcja ustalajaca poczatkowa gorna granice - algorytm zachlanny - rozwiazanie optymalne na pewno nie gorsze niz wyznaczone

            bool[] miasto = new bool[ilosc_przesylek]; //miasto odwiedzone - true
            int aktualne = 0, kolejne = 0; //indeksy miast
            double poczatkowe_gorna = 0; // suma wag

            for (int i = 0; i < ilosc_przesylek; i++) // poczatkowe czyszczenie
            {
                miasto[i] = false;
            }

            double min;
            for (int i = 0; i < ilosc_przesylek - 1; i++) // rozpatruje wszystkie miasta
            {
                min = Double.MaxValue; //wartosc najkrotszego polaczenia
                for (int j = 1; j < ilosc_przesylek; j++) //wyszukanie najkrotszej sciezki (od 1, bo na razie do zerowego nie wracam, nie tworze cyklu)
                {
                    if ((!(miasto[j])) && j != aktualne) //miasto jeszcze nieodwiedzone(zeby sie nie cofac) i rozne od obecnego
                    {
                        if (odleglosc[aktualne, j] < min) // wyszukanie aktualnie najkorzystniejszej drogi
                        {
                            min = odleglosc[aktualne, j];
                            kolejne = j; //na razie ta droga wydaje sie najlepsza
                        }
                    }
                }
                poczatkowe_gorna += odleglosc[aktualne, kolejne]; // zsumowanie kosztu
                aktualne = kolejne; //przejscie
                miasto[aktualne] = true;  //juz odwiedzono   
            }

            poczatkowe_gorna += 1+odleglosc[aktualne, 0]; //zamykam cykl
            this.gorna = poczatkowe_gorna; //ustawienie poczatkowej gornej granicy!

            //..........................................................................................................
            int[] droga = new int[ilosc_przesylek]; //droga taka bazowa do funkcji
            for (int i = 0; i < ilosc_przesylek; i++)
            {
                droga[i] = -1; // hehe
            }
            
            double[,] tablica = new double[ilosc_przesylek + 1, ilosc_przesylek + 1]; //juz nie tab, a tablica(az o 1 wieksza w kazdym wymiarze) - ma dodatkowe pola na zapamietywane numery indeksow


            for (int i = 1; i < ilosc_przesylek + 1; i++) // ogolnie wpisanie wartosci w nowa tablice
            {
                tablica[0, i] = i; //indeksowanie
                tablica[i, 0] = i;

                for (int j = 1; j < ilosc_przesylek + 1; j++)
                {
                    tablica[i, j] = odleglosc[i - 1, j - 1]; //przepisywanie wartosci
                }
            }

            bool udalo_sie;
            udalo_sie = little(tablica, droga, ilosc_przesylek, 0, ilosc_przesylek);
            
            int dalej = 0;
            
            List<int> lista = new List<int>();

            lista.Add(0);

            for (int i = 0; i < ilosc_przesylek - 1; i++)
            {

                lista.Add(cykl[dalej]);
                dalej = cykl[dalej];

            }
            return lista;
        }

        //...........................................................................................................................................................................................

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

        public bool little(double [,] tab, int [] droga, int n, double dolna, int l_przesylek) //n to liczba rozpatrywanych miast
        {

		        if (n==2) //warunek zakonczenia rekurencji
		        {
				        droga[(int)(tab[1,0])-1] = (int)tab[0,2]-1; //pierwsza mozliwosc zamkniecia
				        droga[(int)tab[2,0]-1] = (int)tab[0,1]-1;

				        int tmp1 = droga[0], tmp2;
				
				        for(int i = 0 ; i < l_przesylek -1; i++)
                        {
					        if(tmp1 == 0) //jezeli cykl konczy sie wczesniej niz powinien - znaczy nie bardzo jest to interesujacy nas cykl po wszystkich miastach
					        {                             
						        tmp1 = -1;
						        break;	
					        }

					        tmp2 = droga[tmp1];
					        tmp1 = tmp2;
					
				        }

				        if(tmp1 == 0) //jezeli jest cykl
				        {
					       // if (tab[1,2] != double.MaxValue && tab[2,1] != double.MaxValue) //gdyby jednak cos takiego sie przytrafilo
						        if((dolna + tab[1,2] + tab[2,1]) < gorna)
						        {
						        gorna = dolna + tab[1,2] + tab[2,1];
						        for (int i=0; i < l_przesylek; i++) //kopia najlepszej trasy do tej pory do obiektu
						        {
							        cykl[i] = droga[i];
						        }
						        }
				        }

				        droga[(int)(tab[1,0])-1] = (int)(tab[0,1])-1; //i druga mozliwosc zamkniecia
				        droga[(int)(tab[2,0])-1] = (int)(tab[0,2])-1;
				        tmp1 = droga[0];
				        for(int i = 0 ; i < l_przesylek -1; i++)
				        {
					        if(tmp1 == 0) //jezeli cykl konczy sie wczesniej niz powinien - znaczy nie bardzo jest to interesujacy nas cykl po wszystkich miastach
					        {
						        tmp1 = -1;
						        break;	
					        }

					        tmp2 = droga[tmp1];
					        tmp1 = tmp2;
					
				        }
				        if(tmp1 == 0) //jezeli jest cykl
				        {
					       // if (tab[1,2] != double.MaxValue && tab[2,1] != double.MaxValue) //gdyby jednak cos takiego sie przytrafilo                          
						        if((dolna + tab[1,1] + tab[2,2]) < gorna)
						        {
						        gorna = dolna + tab[1,1] + tab[2,2];
                                
						        for (int i=0; i < l_przesylek; i++) //kopia najlepszej trasy do tej pory do obiektu
						        {
                                    
							        this.cykl[i] = droga[i];
						        }
						        }
				        }
				        return true;
			        } 
		        else // dla macierzy wiekszej niz 2 standardowy podzial
			        {
				        // redukcja macierzy kosztow
				        double [] minimalne_w = new double[n];
                        double [] minimalne_k = new double[n]; //tablice trzymajace znalezione minimalne wartosci wiersz_podzial odpowiednich wierszach i kolumnach
				        double min;
				        for (int i = 1; i <= n; i++) // i teraz znalezienie minimum dla kazdego wiersza - wiersz_podzial tym punkcie pamietam, ze moja macierz/tablice z odleglosciami poeiwkszylem o 1 wiersz_podzial kazdym wymiarze (wiersz_podzial celu pamietania indeksow danego wiersza, kolumny)! wiec wiersz_podzial wiekszosci petle beda zaczynac sie od 1 i konczyc na <=
				        { 
					        min = double.MaxValue; //dla kazdego wiersza od nowa szukanie minimum

					        for (int j = 1; j <= n; j++) // kolejne wartosci wiersz_podzial wierszu
					        {
						        if (tab[i,j] < min) // jezeli znaleziono najmniejsza do tej pory
						        {
							        min = tab[i,j];

							        if(min == 0) // mniejszej nie znajdziemy
							        {
								        break;
							        }
						        }
					        }
					        if(min < double.MaxValue)
					        {
					        minimalne_w[i-1] = min; // przechowanie najmniejszej dla danego wiersza (uwaga, wiersze numerowane od 0!)
					        }
					        else
					        {
						        minimalne_w[i-1] = 0;
					        }
				        }

				        for (int i = 1; i <= n; i++) //odjecie znalezionych minimum od kazdej wartosci wiersz_podzial zadanym wierszu
				        {
					        if(minimalne_w[i-1] > 0) // jezeli wiersz_podzial ogole odejmowanie ma sens - bedzie co odejmowac, wieksze od zera
					        {
					        for (int j = 1; j <= n; j++)
					        {
						        if(tab[i,j] != double.MaxValue)
							        tab[i,j] -= minimalne_w[i-1];
					        }
					        }
				        }


				        for (int i = 1; i <=n ; i++) //analogiczne dzialania dla kazdej kolumny
				        { 
					        min = double.MaxValue;

					        for (int j = 1; j <= n; j++)
					        {
						        if (tab[j,i] < min)
						        {
							        min = tab[j,i];

							        if(min == 0) // mniejszej nie znajdziemy
							        {
								        break;
							        }
						        }
					        }

					        if(min < double.MaxValue)
					        {
					        minimalne_k[i-1] = min; // przechowanie najmniejszej dla danego wiersza (uwaga, wiersze numerowane od 0!)
					        }
					        else
					        {
						        minimalne_k[i-1] = 0;
					        }
				        }

				        for (int i = 1; i <=n ; i++) //odejmowanie wiersz_podzial kolumnach
				        {
					        if(minimalne_k[i-1] > 0 ) // jezeli wiersz_podzial ogole odejmowanie ma sens
					        {
					        for (int j = 1; j <= n; j++)
					        {
						        if(tab[j,i] != double.MaxValue)
							        tab[j,i] -= minimalne_k[i-1];
					        }
					        }
				        }   

				        double dolna_przyrost = 0; //o ile zwiekszylo sie aktualnie dolne ograniczenie
				        for (int i = 0; i < n; i++)
				        {
					        dolna_przyrost += (minimalne_w[i] + minimalne_k[i]); // zsumowanie wszystkich wartosci
				        }
                        
				        double nowa_dolna = dolna + dolna_przyrost; // nowa dolna granica dla zadanej macierzy
                        
			        //  Wybor luku wg ktorego nastapi podzial drzewa (taki, ktory spowoduje najwieksy wzrost dolnego ograniczenia dla rozwiazan, ktore tego luku na pewno nie posiadaja)

				        int wiersz_podzial=0, kolumna_podzial=0;
                        double minimum_w_kolumnie, minimum_w_wierszu, max_tmp, max = -1.0, minimum_w_wierszuxD=0,  minimum_w_kolumniexD=0;
				        for (int i = 1; i <= n; i++) //przeszukujemy cala macierz
				        {
					        for (int j = 1; j <= n; j++)
					        {
						
						        if (tab[i, j] == 0) // az natrafimy na krawedz/sciezke z wartoscia zero
						        {	
							        minimum_w_wierszu = double.MaxValue;
							        for (int l = 1; l <= n; l++) //szukamy najmniejszej wartosci w wierszu
							        {
								        if ((l != j) && (tab[i,l] < minimum_w_wierszu))
								        {
									        minimum_w_wierszu = tab[i,l];
								        }
							        }

							        minimum_w_kolumnie = double.MaxValue;
							        for (int m = 1; m <= n; m++) 
							        {
								        if ((m != i) && (tab[m,j] < minimum_w_kolumnie)) //szukamy najmniejszej wartosci w wierszu
								        {
									        minimum_w_kolumnie = tab[m,j];
								        }
								
							        }
							
							        max_tmp = minimum_w_wierszu + minimum_w_kolumnie; //zeby nie dodawac dwa razy

							        if (max < max_tmp) //jezli znalezlismy najwieksza sume minimow
							        { 
								        max = max_tmp; //zapamietanie wartosci
								        wiersz_podzial = i; //zapamietanie pozycji wystapienia krawedzi
								        kolumna_podzial = j;
								        minimum_w_wierszuxD = minimum_w_wierszu; // zeby nie liczyc ponownie znalezionego juz minimum
								        minimum_w_kolumniexD = minimum_w_kolumnie;
							        }
						        }
					        }
				        }
				
			        //  Podzial zbioru wg wybranej krawedzi
			        //  Poddrzewo z wybrana krawedzia
				        double [,]tab_z_krawedzia = new double [n, n]; //utworzenie nowej macierzy - wszystkie rozwiazania z wybrana krawedzia - dlaczego n, a nie n -1? bo nasza tablica ma jeszcze numery indeksow (w zasadzie oryginalna ma rozmiar n+1)
				        
				        int i_mniejsze = 0 ,j_mniejsze;
				        for (int i = 0; i <= n; i++) //tym razem indeks od zera, gdyz chce przepisac rowniez zawartosc... indeksow
				        { 
					        if (i == wiersz_podzial)
					        {
						        i_mniejsze--; //jesli natrafiamy na wiersz, ktory chcemy pominac, indeks zatrzymuje sie w miejscu (poniewaz potem zostanie zwiekszony o 1)
					        }
					        else //a tu przepisujemy wiersz
					        {
						        j_mniejsze = 0;
						        for (int j = 0; j <= n; j++) 
						        {
							        if (j == kolumna_podzial) // bez zadanej kolumny
							        {
								        j_mniejsze--;
							        }
							        else
							        {
								        tab_z_krawedzia[i_mniejsze, j_mniejsze] = tab[i, j];
							        }
							        j_mniejsze++;
						        }
					        }
					        i_mniejsze++;
				        }

				        bool b1 = false, b2 = false; //zapobiezenie pojawieniu sie cykli, czyli sprawdzam, czy czasem go nie ma :D
				        int kol=0, wie=0;
				        for (int i = 1; i < n; i++) 
				        { 
					        if (tab_z_krawedzia[i, 0] == tab[0, kolumna_podzial]) 
					        {
						        b2 = true;
						        wie = i;
					        }
					        if (tab_z_krawedzia[0, i] == tab[wiersz_podzial, 0]) 
					        {
						        b1 = true;
						        kol = i;
					        }
					        if (b2 && b1) 
					        {
						        tab_z_krawedzia[wie, kol]=double.MaxValue; //jeżeli istnieje to blokujemy przejście
						        break;
					        }
				        }
                        
				        if (nowa_dolna < gorna) //jezeli dolne oszacowanie mniejsze niz znane gorne - mamy szanse na jakies lepsze rozwiazanie.. | a jak nie jest, to dalej nawet nie ma co sie zaglebiac
				        { 
					        int [] nowa_droga = new int[l_przesylek]; //kopia dotychczasowej drogi
					        for (int i = 0; i < l_przesylek; i++)
					        {
						        nowa_droga[i] = droga[i]; // tak - kopia
					        }
					        nowa_droga[(int)(tab[wiersz_podzial,0])-1] = (int)(tab[0,kolumna_podzial])-1; //dodajemy nową krawędź

						        little(tab_z_krawedzia, nowa_droga, n-1, nowa_dolna, l_przesylek); //wchodzimy glebiej w dany przypadek

				        }

				     

				        //podrzewo bez danej krawedzi

					        tab[wiersz_podzial,kolumna_podzial] = double.MaxValue; //nieskończoność wiersz_podzial miejsce wcześniej wybranego zera (czyli teraz bez tej krawędzi)
				
					        for (int i = 1; i <= n ; i++) // ponowne ograniczenie
					        { 
						        if (tab[wiersz_podzial,i] < double.MaxValue)
						        {
							        tab[wiersz_podzial,i] -= minimum_w_wierszuxD;
						        }
						        if (tab[i,kolumna_podzial] < double.MaxValue)
						        {
							        tab[i,kolumna_podzial] -= minimum_w_kolumniexD;
						        }
					        }
					
						         nowa_dolna += (minimum_w_wierszuxD+minimum_w_kolumniexD); //nowa dolna granica

						         if (nowa_dolna < gorna) // jezeli ma sens dalej isc w te wariant to idziemy wglab
							        {
								        little(tab, droga, n, nowa_dolna, l_przesylek); 
							        }
                                 return false;
			        }
	        
        }

   }
}
