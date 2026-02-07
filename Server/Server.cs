using System;
using System.Collections.Generic;
using System.IO;
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
        static List<Jedinica> jedinice = new();
        static List<Zahtev> zavrseniZahtevi = new();
        static Dictionary<string, Socket> socketiJedinica = new();

        // redovi cekanja
        static List<Zahtev> urgentni = new();
        static List<Zahtev> dijagnosticki = new();
        static List<Zahtev> terapeutski = new();

        static BinaryFormatter formatter = new BinaryFormatter();

        static void Main()
        {
            Console.Title = "Telemedicina Server";

            // Start server socket
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Any, 50001));
            server.Listen(10);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server pokrenut...\n");
            Console.ResetColor();

            // Start pozadinska nit za stalnu raspodelu
            new Thread(() =>
            {
                while (true)
                {
                    RaspodeliZahteve();
                    Thread.Sleep(500);
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

            // Jedinica
            if (obj is Jedinica j)
            {
                jedinice.Add(j);
                socketiJedinica[j.IDJedinice] = socket;
                j.Zauzeta = false;

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[JEDINICA] {j.IDJedinice} ({j.Tip}) registrovana");
                Console.ResetColor();

                // ✅ ODMAH PROVERI REDOVE
                RaspodeliZahteve();

                while (true)
                {
                    try
                    {
                        int br2 = socket.Receive(buffer);
                        if (br2 == 0) break;

                        using MemoryStream ms2 = new MemoryStream(buffer, 0, br2);
                        Zahtev z = (Zahtev)formatter.Deserialize(ms2);

                        z.Status = Status.Zavrsen;
                        z.VremeZavrsetka = DateTime.Now;
                        zavrseniZahtevi.Add(z);

                        Jedinica jedinica = jedinice.Find(x => x.IDJedinice == z.IDJedinice);
                        if (jedinica != null)
                            jedinica.Zauzeta = false;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($" ZAVRŠENO: Pacijent {z.IDPacijenta}, Jedinica {z.IDJedinice}");
                        Console.ResetColor();

                        // Ponovo probaj da raspodeli ostale zahteve
                        RaspodeliZahteve();
                    }
                    catch
                    {
                        break;
                    }
                }

                socket.Close();
                return;
            }

            // Pacijent
            if (obj is Pacijent p)
            {
                Zahtev z = new Zahtev
                {
                    IDPacijenta = p.LBO,
                    TipUsluge = p.TipUsluge,
                    Status = Status.Ceka
                };

                switch (p.TipUsluge)
                {
                    case TipUsluge.Urgentna: urgentni.Add(z); break;
                    case TipUsluge.Dijagnosticka: dijagnosticki.Add(z); break;
                    case TipUsluge.Terapeutska: terapeutski.Add(z); break;
                }

                socket.Send(Encoding.UTF8.GetBytes("Zahtev primljen i stavljen u red."));
                socket.Close();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($" Pacijent {p.LBO} → {p.TipUsluge} (u red)");
                Console.ResetColor();
            }
        }

        static void RaspodeliZahteve()
        {
            ObradiRed(urgentni);
            ObradiRed(dijagnosticki);
            ObradiRed(terapeutski);
        }

        static void ObradiRed(List<Zahtev> red)
        {
            foreach (var z in red.ToArray())
            {
                if (PokusajDodele(z))
                    red.Remove(z);
            }
        }

        static bool PokusajDodele(Zahtev z)
        {
            Jedinica slobodna = jedinice.Find(x => x.Tip == z.TipUsluge && !x.Zauzeta);
            if (slobodna == null) return false;

            z.IDJedinice = slobodna.IDJedinice;
            z.Status = Status.UObradi;
            slobodna.Zauzeta = true;

            Socket sock = socketiJedinica[slobodna.IDJedinice];
            sock.Send(Serialize(z));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"DODELA: Pacijent {z.IDPacijenta} = Jedinica {slobodna.IDJedinice} ({slobodna.Tip})");
            Console.ResetColor();

            return true;
        }

        static byte[] Serialize(object o)
        {
            using MemoryStream ms = new MemoryStream();
            formatter.Serialize(ms, o);
            return ms.ToArray();
        }
    }
}
