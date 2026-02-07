using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using static Telemedicina.Modeli;

#pragma warning disable SYSLIB0011

namespace TCPClient
{
    internal class Client
    {
        static void Main()
        {
            Console.Title = "TCP Klijent";
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine("=================================");
            Console.WriteLine("   TELEMEDICINA - TCP KLIJENT");
            Console.WriteLine("=================================");
            Console.ResetColor();

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
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n--- UNOS PACIJENTA ---");
                Console.ResetColor();

                Console.Write("Ime: ");
                string ime = Console.ReadLine();

                Console.Write("Prezime: ");
                string prezime = Console.ReadLine();

                Console.Write("LBO: ");
                string lbo = Console.ReadLine();

                Console.Write("Tip usluge (Urgentna/Dijagnosticka/Terapeutska): ");
                TipUsluge tip = (TipUsluge)Enum.Parse(typeof(TipUsluge), Console.ReadLine(), true);

                Pacijent p = new Pacijent
                {
                    Ime = ime,
                    Prezime = prezime,
                    LBO = lbo,
                    VrstaZahteva = tip
                };

                using MemoryStream ms = new MemoryStream();
                formatter.Serialize(ms, p);
                socket.Send(ms.ToArray());

                int br = socket.Receive(buffer);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nSERVER: " + Encoding.UTF8.GetString(buffer, 0, br));
                Console.ResetColor();

                socket.Close();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n--- REGISTRACIJA JEDINICE ---");
                Console.ResetColor();

                Console.Write("ID jedinice: ");
                string id = Console.ReadLine();

                Console.Write("Tip usluge (Urgentna/Dijagnosticka/Terapeutska): ");
                TipUsluge tip = (TipUsluge)Enum.Parse(typeof(TipUsluge), Console.ReadLine(), true);

                Jedinica j = new Jedinica
                {
                    IDJedinice = id,
                    Tip = tip,
                    StatusJedinice = false
                };

                using MemoryStream ms = new MemoryStream();
                formatter.Serialize(ms, j);
                socket.Send(ms.ToArray());

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nJedinica registrovana. Čeka zahteve...\n");
                Console.ResetColor();

                while (true)
                {
                    int br = socket.Receive(buffer);
                    using MemoryStream ms2 = new MemoryStream(buffer, 0, br);
                    Zahtev z = (Zahtev)formatter.Deserialize(ms2);

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"▶ Obrada pacijenta {z.IDPacijenta} ({z.TipUsluge})");
                    Console.ResetColor();

                    System.Threading.Thread.Sleep(2000);

                    z.StatusZahteva = Status.Zavrsen;
                    z.VremeZavrsetka = DateTime.Now;

                    using MemoryStream ms3 = new MemoryStream();
                    formatter.Serialize(ms3, z);
                    socket.Send(ms3.ToArray());

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✔ Završeno u {z.VremeZavrsetka:T}\n");
                    Console.ResetColor();
                }
            }
        }
    }
}
