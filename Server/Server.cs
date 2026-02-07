using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using static Telemedicina.Modeli;

#pragma warning disable SYSLIB0011

namespace TCPServer
{
    internal class Server
    {
        static List<Pacijent> pacijenti = new();
        static List<Jedinica> jedinice = new();
        static Dictionary<string, Socket> socketiJedinica = new();
        static Dictionary<string, Zahtev> aktivniZahtevi = new();
        static BinaryFormatter formatter = new BinaryFormatter();
        static object lockObj = new();

        static Queue<Zahtev> urgentni = new();
        static Queue<Zahtev> dijagnosticki = new();
        static Queue<Zahtev> terapeutski = new();

        static bool stanjePromenjeno = false;

        static void Main()
        {
            Console.Title = "Telemedicina Server";

            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Any, 50001));
            server.Listen(10);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server pokrenut...\n");
            Console.ResetColor();

            // Nit za prikaz tabela samo kada se promeni stanje
            new Thread(() =>
            {
                while (true)
                {
                    lock (lockObj)
                    {
                        if (stanjePromenjeno)
                        {
                            PrikaziTabele();
                            stanjePromenjeno = false;
                        }
                    }
                    Thread.Sleep(300);
                }
            })
            { IsBackground = true }.Start();

            while (true)
            {
                Socket klijent = server.Accept();
                new Thread(() => ObradiKlijenta(klijent)).Start();
            }
        }

        static void ObradiKlijenta(Socket socket)
        {
            byte[] buffer = new byte[4096];
            int br = socket.Receive(buffer);
            using MemoryStream ms = new MemoryStream(buffer, 0, br);
            object obj = formatter.Deserialize(ms);

            lock (lockObj)
            {
                // Registracija jedinice
                if (obj is Jedinica j)
                {
                    jedinice.Add(j);
                    socketiJedinica[j.IDJedinice] = socket;
                    j.Zauzeta = false;
                    stanjePromenjeno = true;

                    RaspodeliZahteve();

                    // Nit za praćenje jedinice
                    new Thread(() => ObradiJedinicu(j, socket)).Start();
                    return;
                }

                // Pacijent
                if (obj is Pacijent p)
                {
                    pacijenti.Add(p);

                    Zahtev z = new Zahtev
                    {
                        IDPacijenta = p.LBO,
                        TipUsluge = p.TipUsluge,
                        Status = Status.Ceka
                    };

                    switch (p.TipUsluge)
                    {
                        case TipUsluge.Urgentna: urgentni.Enqueue(z); break;
                        case TipUsluge.Dijagnosticka: dijagnosticki.Enqueue(z); break;
                        case TipUsluge.Terapeutska: terapeutski.Enqueue(z); break;
                    }

                    socket.Send(Encoding.UTF8.GetBytes("Zahtev primljen."));
                    socket.Close();

                    stanjePromenjeno = true;
                    RaspodeliZahteve();
                }
            }
        }

        static void ObradiJedinicu(Jedinica j, Socket sock)
        {
            byte[] buffer = new byte[4096];

            while (true)
            {
                try
                {
                    int br = sock.Receive(buffer);
                    if (br == 0) break;

                    using MemoryStream ms = new MemoryStream(buffer, 0, br);
                    Zahtev z = (Zahtev)formatter.Deserialize(ms);

                    // Ažuriraj pacijenta
                    Pacijent pac = pacijenti.FirstOrDefault(p => p.LBO == z.IDPacijenta);
                    if (pac != null) pac.StatusPacijenta = Status.Zavrsen;

                    // Jedinica više nije zauzeta
                    Jedinica jedinica = jedinice.FirstOrDefault(u => u.IDJedinice == z.IDJedinice);
                    if (jedinica != null) jedinica.Zauzeta = false;

                    // Ukloni aktivni zahtev
                    if (aktivniZahtevi.ContainsKey(z.IDJedinice))
                        aktivniZahtevi.Remove(z.IDJedinice);

                    stanjePromenjeno = true;
                    RaspodeliZahteve();
                }
                catch
                {
                    break;
                }
            }
        }

        static void RaspodeliZahteve()
        {
            ObradiRed(urgentni);
            ObradiRed(dijagnosticki);
            ObradiRed(terapeutski);
        }

        static void ObradiRed(Queue<Zahtev> red)
        {
            if (red.Count == 0) return;

            foreach (var z in red.ToArray())
            {
                if (PokusajDodele(z))
                    red.Dequeue();
            }
        }

        static bool PokusajDodele(Zahtev z)
        {
            Jedinica slobodna = jedinice.FirstOrDefault(x => x.Tip == z.TipUsluge && !x.Zauzeta);
            if (slobodna == null) return false;

            z.IDJedinice = slobodna.IDJedinice;
            z.Status = Status.UObradi;
            slobodna.Zauzeta = true;

            aktivniZahtevi[slobodna.IDJedinice] = z;

            Socket sock = socketiJedinica[slobodna.IDJedinice];
            sock.Send(Serialize(z));

            // Ažuriraj pacijenta
            Pacijent pac = pacijenti.FirstOrDefault(p => p.LBO == z.IDPacijenta);
            if (pac != null) pac.StatusPacijenta = Status.UObradi;

            stanjePromenjeno = true;
            return true;
        }

        static void PrikaziTabele()
        {
            Console.Clear();
            Console.WriteLine("===== PACIJENTI =====");
            Console.WriteLine("LBO\tIme\tPrezime\tTip\tStatus");
            foreach (var p in pacijenti)
            {
                Console.ForegroundColor = p.StatusPacijenta switch
                {
                    Status.Ceka => ConsoleColor.Yellow,
                    Status.UObradi => ConsoleColor.Cyan,
                    Status.Zavrsen => ConsoleColor.Green,
                    _ => ConsoleColor.White
                };
                Console.WriteLine($"{p.LBO}\t{p.Ime}\t{p.Prezime}\t{p.TipUsluge}\t{p.StatusPacijenta}");
            }
            Console.ResetColor();

            Console.WriteLine("\n===== JEDINICE =====");
            Console.WriteLine("ID\tTip\tZauzeta\tTrenutniPacijent");
            foreach (var j in jedinice)
            {
                string pac = aktivniZahtevi.ContainsKey(j.IDJedinice) ? aktivniZahtevi[j.IDJedinice].IDPacijenta : "-";
                Console.ForegroundColor = j.Zauzeta ? ConsoleColor.Cyan : ConsoleColor.Green;
                Console.WriteLine($"{j.IDJedinice}\t{j.Tip}\t{j.Zauzeta}\t{pac}");
            }
            Console.ResetColor();
        }

        static byte[] Serialize(object o)
        {
            using MemoryStream ms = new MemoryStream();
            formatter.Serialize(ms, o);
            return ms.ToArray();
        }
    }
}
