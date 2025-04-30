using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Hotel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<RoomType> RoomTypes { get; set; }
    public List<Room> Rooms { get; set; }
}

class RoomType
{
    public string Code { get; set; }
    public string Description { get; set; }
    public List<string> Amenities { get; set; }
    public List<string> Features { get; set; }
}

class Room
{
    public string RoomType { get; set; }
    public string RoomId { get; set; }
}

class Booking
{
    public string HotelId { get; set; }
    public string RoomId { get; set; }
    public string Date { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        var hotels = LoadData<List<Hotel>>("hotels.json");
        var bookings = LoadData<List<Booking>>("bookings.json");

        Console.WriteLine("Enter command:");
        string input;
        while (!string.IsNullOrEmpty(input = Console.ReadLine()))
        {
            if (input.StartsWith("Availability", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Substring(12).Trim('(', ')').Split(',');
                if (parts.Length == 3)
                {
                    var result = CheckAvailability(hotels, bookings, parts[0], parts[1], parts[1], parts[2]);
                    Console.WriteLine(result);
                }
                else if (parts.Length == 4)
                {
                    var result = CheckAvailability(hotels, bookings, parts[0], parts[1], parts[2], parts[3]);
                    Console.WriteLine(result);
                }
                else Console.WriteLine("Invalid Availability format.");
            }
            else if (input.StartsWith("Search", StringComparison.OrdinalIgnoreCase))
            {
                var parts = input.Substring(7).Trim('(', ')').Split(',');
                if (parts.Length == 4 && int.TryParse(parts[1], out int days))
                {
                    var result = SearchAvailability(hotels, bookings, parts[0], days, parts[3]);
                    Console.WriteLine(string.Join(", ", result.Select(r => $"[{r.Start}-{r.End}]: {r.Count}")));
                }
                else Console.WriteLine("Invalid Search format.");
            }
            else Console.WriteLine("Unknown command.");
        }
    }

    static T LoadData<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path));

    static string CheckAvailability(List<Hotel> hotels, List<Booking> bookings,
        string hotelId, string startDate, string endDate, string roomType)
    {
        var hotel = hotels.FirstOrDefault(h => h.Id == hotelId);
        if (hotel == null) return "Hotel not found.";

        var rooms = hotel.Rooms.Where(r => r.RoomType == roomType).Select(r => r.RoomId).ToList();
        if (!rooms.Any()) return "No rooms found.";

        var booked = bookings
            .Where(b => b.HotelId == hotelId && rooms.Contains(b.RoomId) &&
                        String.Compare(b.Date, startDate) >= 0 && String.Compare(b.Date, endDate) <= 0)
            .Select(b => b.RoomId)
            .Distinct();

        int availableCount = rooms.Count(r => !booked.Contains(r));
        return $"Available: {availableCount}";
    }

    static List<(string Start, string End, int Count)> SearchAvailability(List<Hotel> hotels, List<Booking> bookings,
        string hotelId, int days, string roomType)
    {
        var hotel = hotels.FirstOrDefault(h => h.Id == hotelId);
        var rooms = hotel?.Rooms.Where(r => r.RoomType == roomType).Select(r => r.RoomId).ToList();

        var bookingsGrouped = bookings
            .Where(b => b.HotelId == hotelId && rooms.Contains(b.RoomId))
            .GroupBy(b => b.Date)
            .ToDictionary(g => g.Key, g => g.Select(b => b.RoomId).ToHashSet());

        var result = new List<(string Start, string End, int Count)>();
        var current = new List<string>();
        string start = null;

        for (int i = 1; i <= 31 - days; i++)
        {
            var dateRange = Enumerable.Range(i, days).Select(d => $"2025-01-{d:D2}").ToList();
            var available = rooms.Count(r => dateRange.All(d => !bookingsGrouped.ContainsKey(d) || !bookingsGrouped[d].Contains(r)));

            if (available > 0)
            {
                if (start == null) start = dateRange.First();
                current = dateRange;
            }
            else if (start != null)
            {
                result.Add((start, current.Last(), current.Count));
                start = null;
