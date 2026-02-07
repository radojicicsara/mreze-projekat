using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using static Telemedicina.Modeli;

#pragma warning disable SYSLIB0011

namespace TCPClient
{
    internal class Client
    {
        static void Main()
        {
            Console.Title = "Telemedicina Klijent";

            Console.WriteLine("1) Pacijent");
            Console.WriteLine("2) Jedinica");
            Console.Write("Izbor: ");
            string izbor = Console.ReadLine();

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Loopback, 50001);

            BinaryFormatter formatter = new BinaryFormatter();
            byte[] buffer = new byte[4096];

            if (izbor == "1")
            {
                Console.Write("Ime: "); string ime = Console.ReadLine();
                Console.Write("Prezime: "); string prezime = Console.ReadLine();
                Console.Write("LBO: "); string lbo = Console.ReadLine();
                Console.Write("Tip usluge (Urgentna/Dijagnosticka/Terapeutska): ");
                TipUsluge tip = (TipUsluge)Enum.Parse(typeof(TipUsluge), Console.ReadLine(), true);

                Pacijent p = new Pacijent { Ime = ime, Prezime = prezime, LBO = lbo, TipUsluge = tip };
                socket.Send(Serialize(formatter, p));

                int br = socket.Receive(buffer);
                Console.WriteLine("SERVER: " + Encoding.UTF8.GetString(buffer, 0, br));
                socket.Close();
            }
            else
            {
                Console.Write("ID Jedinice: "); string id = Console.ReadLine();
                Console.Write("Tip usluge (Urgentna/Dijagnosticka/Terapeutska): ");
                TipUsluge tip = (TipUsluge)Enum.Parse(typeof(TipUsluge), Console.ReadLine(), true);

                Jedinica j = new Jedinica { IDJedinice = id, Tip = tip, Zauzeta = false };
                socket.Send(Serialize(formatter, j));

                Console.WriteLine("Jedinica registrovana. Čeka zahteve...");

                while (true)
                {
                    int br2 = socket.Receive(buffer);
                    using MemoryStream ms = new MemoryStream(buffer, 0, br2);
                    Zahtev z = (Zahtev)formatter.Deserialize(ms);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Primljen zahtev: Pacijent {z.IDPacijenta}, Tip {z.TipUsluge}");
                    Console.ResetColor();

                    Thread.Sleep(2000); // simulacija obrade

                    z.Status = Status.Zavrsen;
                    z.VremeZavrsetka = DateTime.Now;

                    socket.Send(Serialize(formatter, z));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Završeno: Pacijent {z.IDPacijenta}");
                    Console.ResetColor();
                }
            }
        }

        static byte[] Serialize(BinaryFormatter f, object o)
        {
            using MemoryStream ms = new MemoryStream();
            f.Serialize(ms, o);
            return ms.ToArray();
        }
    }
}
