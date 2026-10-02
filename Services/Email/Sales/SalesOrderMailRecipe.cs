namespace GranitWebApi.Services.Email.Sales
{
    public static class SalesOrderMailRecipe
    {
        public static string CreateBody(
            string systemOrderNumber,
            string customerName,
            string customerCode)
        {
            return $@"
<html>
<body>
    <p>Merhaba,</p>

    <p>
        Satış siparişi Netsis aktarımı başarıyla tamamlanmıştır.
    </p>

    <p>
        <strong>Sipariş No:</strong> {systemOrderNumber}<br />
        <strong>Müşteri:</strong> {customerName}<br />
        <strong>Müşteri Kodu:</strong> {customerCode}
    </p>

    <p>
        Sipariş formu ve sipariş kalemlerine ait görseller ekte yer almaktadır.
    </p>

    <p>
        İyi çalışmalar.
    </p>
</body>
</html>";
        }
    }
}