namespace JobBus.Classes
{
    public class JobBusConfig
    {
        public int CityHallId = 0;

        public float TaxPercentage = 0;
        public float PlayerReceivePercentage = 0;

        public int MinCustomerPerBusStop = 0;
        public int MaxCustomerPerBusStop = 0;

        public float MinMoneyPerCustomer = 0f;
        public float MaxMoneyPerCustomer = 0f;

        public double TicketHeure = 0f;
        public double TicketJournée = 0f;
        public double TicketMensuel = 0f;

        public string UrlWebhookForNotifyService = "";
    }
}
