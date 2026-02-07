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
        static List<Pacijent> pacijenti = new();
        static List<Jedinica> jedinice = new();
        static List<Zahtev> zavrseniZahtevi = new();
        static Dictionary<string, Socket> socketiJedinica = new();
        static BinaryFormatter formatter = new();

        static void Main()
        {
            Console.Title = "TCP Server";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=================================");
            Console.WriteLine("   TELEMEDICINA - TCP SERVER");
            Console.WriteLine("=================================");
            Console.ResetColor();

            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Any, 50001));
            server.Listen(10);

            Console.WriteLine("Server pokrenut...\n");

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

            // JEDINICA
            if (obj is Jedinica j)
            {
                jedinice.Add(j);
                socketiJedinica[j.IDJedinice] = socket;

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[JEDINICA] {j.IDJedinice} ({j.Tip}) registrovana");
                Console.ResetColor();

                while (true)
                {
                    try
                    {
                        int br2 = socket.Receive(buffer);
                        if (br2 == 0) break;

                        using MemoryStream ms2 = new MemoryStream(buffer, 0, br2);
                        Zahtev z = (Zahtev)formatter.Deserialize(ms2);

                        z.StatusZahteva = Status.Zavrsen;
                        zavrseniZahtevi.Add(z);

                        Pacijent p = pacijenti.Find(x => x.LBO == z.IDPacijenta);
                        if (p != null) p.StatusPacijenta = Status.Zavrsen;

                        j.StatusJedinice = false;

                        PrikaziIzvestaj();
                    }
                    catch
                    {
                        break;
                    }
                }

                socket.Close();
                return;
            }

            // PACIJENT
            if (obj is Pacijent p2)
            {
                pacijenti.Add(p2);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[PACIJENT] {p2.Ime} {p2.Prezime} ({p2.VrstaZahteva})");
                Console.ResetColor();

                Jedinica slobodna = jedinice.Find(x => x.Tip == p2.VrstaZahteva && !x.StatusJedinice);

                if (slobodna == null)
                {
                    socket.Send(Encoding.UTF8.GetBytes("Nema slobodne jedinice."));
                    socket.Close();
                    return;
                }

                Zahtev z = new Zahtev
                {
                    IDPacijenta = p2.LBO,
                    IDJedinice = slobodna.IDJedinice,
                    TipUsluge = p2.VrstaZahteva,
                    StatusZahteva = Status.U_Obradi
                };

                slobodna.StatusJedinice = true;
                socketiJedinica[slobodna.IDJedinice].Send(Serialize(z));

                socket.Send(Encoding.UTF8.GetBytes("Zahtev uspešno prosleđen."));
                socket.Close();
            }
        }

        static byte[] Serialize(object o)
        {
            using MemoryStream ms = new();
            formatter.Serialize(ms, o);
            return ms.ToArray();
        }

        static void PrikaziIzvestaj()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n===== IZVEŠTAJ ZAVRŠENIH USLUGA =====");
            foreach (var z in zavrseniZahtevi)
            {
                Console.WriteLine(
                    $"Pacijent: {z.IDPacijenta} | " +
                    $"Usluga: {z.TipUsluge} | " +
                    $"Vreme: {z.VremeZavrsetka:T}"
                );
            }
            Console.WriteLine("===================================\n");
            Console.ResetColor();
        }
    }
}
