using System;

namespace Telemedicina
{
    public class Modeli
    {
        [Serializable]
        public enum TipUsluge
        {
            Urgentna,
            Dijagnosticka,
            Terapeutska
        }

        [Serializable]
        public enum Status
        {
            Ceka,
            UObradi,
            Zavrsen
        }

        [Serializable]
        public class Pacijent
        {
            public string LBO { get; set; } = "";
            public string Ime { get; set; } = "";
            public string Prezime { get; set; } = "";
            public TipUsluge TipUsluge { get; set; }
            public Status StatusPacijenta { get; set; } = Status.Ceka;
        }

        [Serializable]
        public class Jedinica
        {
            public string IDJedinice { get; set; } = "";
            public TipUsluge Tip { get; set; }
            public bool Zauzeta { get; set; }
        }

        [Serializable]
        public class Zahtev
        {
            public string IDPacijenta { get; set; } = "";
            public string IDJedinice { get; set; } = "";
            public TipUsluge TipUsluge { get; set; }
            public Status Status { get; set; } = Status.Ceka;
            public DateTime VremeZavrsetka { get; set; }
        }
    }
}
