using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using static Telemedicina.Modeli;

#pragma warning disable SYSLIB0011

namespace Server
{
    internal class Server
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Console.ReadKey();

          
                #region

                Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 18010);

                serverSocket.Bind(serverEP);
                serverSocket.Blocking = false; // Postavljanje servera u neblokirajući režim
                serverSocket.Listen(126);

                Console.WriteLine($"Telemedicina Server pokrenut na adresi: {serverEP}");

                // Liste za praćenje, slično tvom acceptedSockets, ali razdvojeno radi projekta
                List<Socket> klijentskiSoketi = new List<Socket>();
                List<Jedinica> listaJedinica = new List<Jedinica>();
                List<Pacijent> listaPacijenata = new List<Pacijent>();

                #endregion

                #region Komunikacija (Polling model)

                byte[] buffer = new byte[4096];

                while (true)
                {
                    // 1. Prihvatanje novih klijenta (Pacijent aplikacija ili Jedinica aplikacija)
                    if (serverSocket.Poll(2000 * 1000, SelectMode.SelectRead))
                    {
                        Socket noviKlijent = serverSocket.Accept();
                        noviKlijent.Blocking = false; // Svaka nova utičnica mora biti neblokirajuća
                        klijentskiSoketi.Add(noviKlijent);
                        Console.WriteLine($"Novi učesnik povezan sa adrese: {noviKlijent.RemoteEndPoint}");
                    }

                    // 2. Prolazak kroz sve povezane klijente (Polling za poruke)
                    for (int i = 0; i < klijentskiSoketi.Count; i++)
                    {
                        // Sačekati do narednog pokušaja prijema poruke 1 s 
                        if (klijentskiSoketi[i].Poll(1000 * 1000, SelectMode.SelectRead))
                        {
                            try
                            {
                                int brBajta = klijentskiSoketi[i].Receive(buffer);

                                if (brBajta > 0)
                                {
                                    object primljeno;
                                    using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                                    {
                                        BinaryFormatter bf = new BinaryFormatter();
                                        primljeno = bf.Deserialize(ms);
                                    }

                                    // Provjera šta je klijent poslao 
                                    if (primljeno is Pacijent p)
                                    {
                                        Console.WriteLine($"STIGAO ZAHTEV: Pacijent {p.Ime} {p.Prezime}, LBO: {p.LBO}");
                                        Console.WriteLine($"Tip usluge: {p.VrstaZahteva}");

                                        listaPacijenata.Add(p);

                                        // Slanje potvrde pacijentu 
                                        string odgovor = "Vase podaci su primljeni. Sacekajte dodelu jedinice.";
                                        klijentskiSoketi[i].Send(Encoding.UTF8.GetBytes(odgovor));
                                    }
                                    else if (primljeno is Jedinica j)
                                    {
                                        // Ako se jedinica prvi put povezuje, dodajemo je u evidenciju
                                        Console.WriteLine($"REGISTROVANA JEDINICA: {j.IDJedinice} (Tip: {j.Tip})");
                                        Console.WriteLine($"Status: {(j.StatusJedinice ? "Zauzeta" : "Slobodna")}");

                                        listaJedinica.Add(j);
                                    }
                                }
                                else if (brBajta == 0) // Klijent se zatvorio
                                {
                                    klijentskiSoketi[i].Close();
                                    klijentskiSoketi.RemoveAt(i);
                                    i--;
                                }
                            }
                            catch (SocketException)
                            {
                                Console.WriteLine("Doslo je do prekida veze sa jednim klijentom.");
                                klijentskiSoketi[i].Close();
                                klijentskiSoketi.RemoveAt(i);
                                i--;
                            }
                        }
                    }

                    // Ovde bi kasnije išla vizuelizacija (Zadatak 6)
                    // Console.Clear(); // Pa ispis tabela...
                }

                #endregion
            }
        }
    }


