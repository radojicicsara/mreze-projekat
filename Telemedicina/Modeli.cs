namespace Telemedicina
{
    public class Modeli
    {

        // Tipovi usluga koji određuju kojoj jedinici ide pacijent
        [Serializable]
        public enum TipUsluge { Urgentna, Dijagnosticka, Terapeutska }

        // Statusi za praćenje napretka obrade
        [Serializable]
        public enum Status { Aktivan, U_Obradi, Zavrsen }

        [Serializable]
        public class Pacijent
        {
            public string LBO { get; set; } = string.Empty;
            public string Ime { get; set; } = string.Empty;
            public string Prezime { get; set; } = string.Empty;
            public string Adresa { get; set; } = string.Empty;
            public TipUsluge VrstaZahteva { get; set; }
            public Status StatusPacijenta { get; set; }
        }

        [Serializable]
        public class Jedinica
        {
            public TipUsluge Tip { get; set; }
            public string IDJedinice { get; set; } = string.Empty;
            public bool StatusJedinice { get; set; }
        }

        [Serializable]
        public class Zahtev
        {
            public string IDPacijenta { get; set; } = string.Empty;
            public string IDJedinice { get; set; } = string.Empty;
            public Status StatusZahteva { get; set; } = "Aktivan";

            public DateTime VremeZavrsetka { get; set; }

        }
    }


}

