namespace BioChecadorAPI.Helpers
{
    public static class CaculatorHelper
    {
        public static double CalcularDistanciaMetros(double lat1, double lon1, double lat2, double lon2)
        {
            const double RadioTierraMetros = 6371000;

            var dLat = (lat2 - lat1) * (Math.PI / 180.0);
            var dLon = (lon2 - lon1) * (Math.PI / 180.0);

            var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                    Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                    Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

            var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            return RadioTierraMetros * c;
        }
    }
}
