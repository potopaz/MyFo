using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCurrenciesIso4217 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = "NOW() AT TIME ZONE 'UTC'";

            var currencies = new (string Code, string Name, string Symbol, int Decimals)[]
            {
                // A
                ("AED", "Dirham de los Emiratos Árabes", "د.إ", 2),
                ("AFN", "Afgani afgano", "؋", 2),
                ("ALL", "Lek albanés", "L", 2),
                ("AMD", "Dram armenio", "֏", 2),
                ("ANG", "Florín antillano neerlandés", "ƒ", 2),
                ("AOA", "Kwanza angoleño", "Kz", 2),
                ("ARS", "Peso argentino", "$", 2),
                ("AUD", "Dólar australiano", "A$", 2),
                ("AWG", "Florín arubeño", "ƒ", 2),
                ("AZN", "Manat azerbaiyano", "₼", 2),
                // B
                ("BAM", "Marco convertible de Bosnia", "KM", 2),
                ("BBD", "Dólar de Barbados", "Bds$", 2),
                ("BDT", "Taka bangladesí", "৳", 2),
                ("BGN", "Lev búlgaro", "лв", 2),
                ("BHD", "Dinar bareiní", ".د.ب", 3),
                ("BIF", "Franco burundés", "FBu", 0),
                ("BMD", "Dólar bermudeño", "BD$", 2),
                ("BND", "Dólar de Brunéi", "B$", 2),
                ("BOB", "Boliviano", "Bs.", 2),
                ("BRL", "Real brasileño", "R$", 2),
                ("BSD", "Dólar bahameño", "B$", 2),
                ("BTN", "Ngultrum butanés", "Nu.", 2),
                ("BWP", "Pula botsuano", "P", 2),
                ("BYN", "Rublo bielorruso", "Br", 2),
                ("BZD", "Dólar beliceño", "BZ$", 2),
                // C
                ("CAD", "Dólar canadiense", "C$", 2),
                ("CDF", "Franco congoleño", "FC", 2),
                ("CHF", "Franco suizo", "CHF", 2),
                ("CLP", "Peso chileno", "$", 0),
                ("CNY", "Yuan chino", "¥", 2),
                ("COP", "Peso colombiano", "$", 2),
                ("CRC", "Colón costarricense", "₡", 2),
                ("CUP", "Peso cubano", "$", 2),
                ("CVE", "Escudo caboverdiano", "Esc", 2),
                ("CZK", "Corona checa", "Kč", 2),
                // D
                ("DJF", "Franco yibutiano", "Fdj", 0),
                ("DKK", "Corona danesa", "kr", 2),
                ("DOP", "Peso dominicano", "RD$", 2),
                ("DZD", "Dinar argelino", "د.ج", 2),
                // E
                ("EGP", "Libra egipcia", "E£", 2),
                ("ERN", "Nakfa eritreo", "Nfk", 2),
                ("ETB", "Birr etíope", "Br", 2),
                ("EUR", "Euro", "€", 2),
                // F
                ("FJD", "Dólar fiyiano", "FJ$", 2),
                ("FKP", "Libra malvinense", "FK£", 2),
                // G
                ("GBP", "Libra esterlina", "£", 2),
                ("GEL", "Lari georgiano", "₾", 2),
                ("GHS", "Cedi ghanés", "GH₵", 2),
                ("GIP", "Libra gibraltareña", "£", 2),
                ("GMD", "Dalasi gambiano", "D", 2),
                ("GNF", "Franco guineano", "FG", 0),
                ("GTQ", "Quetzal guatemalteco", "Q", 2),
                ("GYD", "Dólar guyanés", "G$", 2),
                // H
                ("HKD", "Dólar de Hong Kong", "HK$", 2),
                ("HNL", "Lempira hondureño", "L", 2),
                ("HTG", "Gourde haitiano", "G", 2),
                ("HUF", "Forinto húngaro", "Ft", 2),
                // I
                ("IDR", "Rupia indonesia", "Rp", 2),
                ("ILS", "Nuevo séquel israelí", "₪", 2),
                ("INR", "Rupia india", "₹", 2),
                ("IQD", "Dinar iraquí", "ع.د", 3),
                ("IRR", "Rial iraní", "﷼", 2),
                ("ISK", "Corona islandesa", "kr", 0),
                // J
                ("JMD", "Dólar jamaiquino", "J$", 2),
                ("JOD", "Dinar jordano", "د.ا", 3),
                ("JPY", "Yen japonés", "¥", 0),
                // K
                ("KES", "Chelín keniano", "KSh", 2),
                ("KGS", "Som kirguís", "сом", 2),
                ("KHR", "Riel camboyano", "៛", 2),
                ("KMF", "Franco comorense", "CF", 0),
                ("KPW", "Won norcoreano", "₩", 2),
                ("KRW", "Won surcoreano", "₩", 0),
                ("KWD", "Dinar kuwaití", "د.ك", 3),
                ("KYD", "Dólar de las Islas Caimán", "CI$", 2),
                ("KZT", "Tenge kazajo", "₸", 2),
                // L
                ("LAK", "Kip laosiano", "₭", 2),
                ("LBP", "Libra libanesa", "ل.ل", 2),
                ("LKR", "Rupia esrilanquesa", "Rs", 2),
                ("LRD", "Dólar liberiano", "L$", 2),
                ("LSL", "Loti lesotense", "L", 2),
                ("LYD", "Dinar libio", "ل.د", 3),
                // M
                ("MAD", "Dirham marroquí", "د.م.", 2),
                ("MDL", "Leu moldavo", "L", 2),
                ("MGA", "Ariary malgache", "Ar", 2),
                ("MKD", "Denar macedonio", "ден", 2),
                ("MMK", "Kyat birmano", "K", 2),
                ("MNT", "Tugrik mongol", "₮", 2),
                ("MOP", "Pataca macaense", "MOP$", 2),
                ("MRU", "Uguiya mauritana", "UM", 2),
                ("MUR", "Rupia mauriciana", "Rs", 2),
                ("MVR", "Rufiyaa maldiva", "Rf", 2),
                ("MWK", "Kwacha malauí", "MK", 2),
                ("MXN", "Peso mexicano", "MX$", 2),
                ("MYR", "Ringgit malayo", "RM", 2),
                ("MZN", "Metical mozambiqueño", "MT", 2),
                // N
                ("NAD", "Dólar namibio", "N$", 2),
                ("NGN", "Naira nigeriana", "₦", 2),
                ("NIO", "Córdoba nicaragüense", "C$", 2),
                ("NOK", "Corona noruega", "kr", 2),
                ("NPR", "Rupia nepalesa", "Rs", 2),
                ("NZD", "Dólar neozelandés", "NZ$", 2),
                // O
                ("OMR", "Rial omaní", "ر.ع.", 3),
                // P
                ("PAB", "Balboa panameño", "B/.", 2),
                ("PEN", "Sol peruano", "S/", 2),
                ("PGK", "Kina papuana", "K", 2),
                ("PHP", "Peso filipino", "₱", 2),
                ("PKR", "Rupia pakistaní", "Rs", 2),
                ("PLN", "Esloti polaco", "zł", 2),
                ("PYG", "Guaraní paraguayo", "₲", 0),
                // Q
                ("QAR", "Riyal catarí", "ر.ق", 2),
                // R
                ("RON", "Leu rumano", "lei", 2),
                ("RSD", "Dinar serbio", "din.", 2),
                ("RUB", "Rublo ruso", "₽", 2),
                ("RWF", "Franco ruandés", "RF", 0),
                // S
                ("SAR", "Riyal saudí", "ر.س", 2),
                ("SBD", "Dólar de las Islas Salomón", "SI$", 2),
                ("SCR", "Rupia seychelense", "Rs", 2),
                ("SDG", "Libra sudanesa", "ج.س.", 2),
                ("SEK", "Corona sueca", "kr", 2),
                ("SGD", "Dólar de Singapur", "S$", 2),
                ("SHP", "Libra de Santa Helena", "£", 2),
                ("SLE", "Leone sierraleonés", "Le", 2),
                ("SOS", "Chelín somalí", "Sh", 2),
                ("SRD", "Dólar surinamés", "Sr$", 2),
                ("SSP", "Libra sursudanesa", "£", 2),
                ("STN", "Dobra santotomense", "Db", 2),
                ("SVC", "Colón salvadoreño", "₡", 2),
                ("SYP", "Libra siria", "£S", 2),
                ("SZL", "Lilangeni suazi", "E", 2),
                // T
                ("THB", "Baht tailandés", "฿", 2),
                ("TJS", "Somoni tayiko", "SM", 2),
                ("TMT", "Manat turcomano", "T", 2),
                ("TND", "Dinar tunecino", "د.ت", 3),
                ("TOP", "Paʻanga tongano", "T$", 2),
                ("TRY", "Lira turca", "₺", 2),
                ("TTD", "Dólar trinitense", "TT$", 2),
                ("TWD", "Nuevo dólar taiwanés", "NT$", 2),
                ("TZS", "Chelín tanzano", "TSh", 2),
                // U
                ("UAH", "Grivna ucraniana", "₴", 2),
                ("UGX", "Chelín ugandés", "USh", 0),
                ("USD", "Dólar estadounidense", "US$", 2),
                ("UYU", "Peso uruguayo", "$U", 2),
                ("UZS", "Som uzbeko", "сўм", 2),
                // V
                ("VES", "Bolívar venezolano", "Bs.S", 2),
                ("VND", "Dong vietnamita", "₫", 0),
                ("VUV", "Vatu vanuatense", "VT", 0),
                // W
                ("WST", "Tala samoano", "WS$", 2),
                // X
                ("XAF", "Franco CFA de África Central", "FCFA", 0),
                ("XCD", "Dólar del Caribe Oriental", "EC$", 2),
                ("XOF", "Franco CFA de África Occidental", "CFA", 0),
                ("XPF", "Franco CFP", "₣", 0),
                // Y
                ("YER", "Rial yemení", "﷼", 2),
                // Z
                ("ZAR", "Rand sudafricano", "R", 2),
                ("ZMW", "Kwacha zambiano", "ZK", 2),
                ("ZWL", "Dólar zimbabuense", "Z$", 2),
            };

            foreach (var (code, name, symbol, decimals) in currencies)
            {
                var escapedName = name.Replace("'", "''");
                var escapedSymbol = symbol.Replace("'", "''");

                migrationBuilder.Sql($@"
                    INSERT INTO currencies (currency_id, code, name, symbol, decimal_places, created_at, created_by)
                    VALUES (gen_random_uuid(), '{code}', '{escapedName}', '{escapedSymbol}', {decimals}, {now}, '00000000-0000-0000-0000-000000000000')
                    ON CONFLICT DO NOTHING;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM currencies;");
        }
    }
}
