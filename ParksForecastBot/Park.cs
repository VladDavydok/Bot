public class Park
{
    public string Name { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public Park(string name, double lat, double lng)
    {
        Name = name;
        Lat = lat;
        Lng = lng;
    }
}