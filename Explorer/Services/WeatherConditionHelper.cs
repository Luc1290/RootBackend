namespace RootBackend.Explorer.Helpers
{
    public static class WeatherConditionHelper
    {
        public static string GetConditionDescription(int code)
        {
            return code switch
            {
                0 => "Ciel dégagé",
                1 or 2 => "Partiellement nuageux",
                3 => "Couvert",
                45 or 48 => "Brouillard",
                51 or 53 or 55 => "Bruine",
                56 or 57 => "Bruine verglaçante",
                61 or 63 or 65 => "Pluie légère",
                66 or 67 => "Pluie verglaçante",
                71 or 73 or 75 => "Neige",
                77 => "Neige en grains",
                80 or 81 or 82 => "Averses",
                85 or 86 => "Averses de neige",
                95 => "Orages",
                96 or 99 => "Orages violents",
                _ => "Conditions météo inconnues"
            };
        }
    }
}
