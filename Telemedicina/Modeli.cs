using System;

namespace Telemedicina
{
    public class Modeli
    {
        [Serializable]
        public enum TipUsluge { Urgentna, Dijagnosticka, Terapeutska }

        [Serializable]
        public enum Status { Aktivan, U_Obradi, Zavrsen }

        [Serializable]
        public class Pacijent
        {
            public string LBO { get; set; } = "";
            public string Ime { get; set; } = "";
            public string Prezime { get; set; } = "";
            public TipUsluge VrstaZahteva { get; set; }
            public Status StatusPacijenta { get; set; } = Status.Aktivan;
        }

        [Serializable]
        public class Jedinica
        {
            public string IDJedinice { get; set; } = "";
            public TipUsluge Tip { get; set; }
            public bool StatusJedinice { get; set; }
        }

        [Serializable]
        public class Zahtev
        {
            public string IDPacijenta { get; set; } = "";
            public string IDJedinice { get; set; } = "";
            public TipUsluge TipUsluge { get; set; }
            public Status StatusZahteva { get; set; }
            public DateTime VremeZavrsetka { get; set; }
        }
    }
}
