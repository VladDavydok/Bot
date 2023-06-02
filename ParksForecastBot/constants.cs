using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
    public class constants
    {
        public static string botId = "6119870516:AAEsfjrigfjPHMaVRbzM93oNjRGx51RMWo4";
    public static string host = "parksforecastapi20230601201108.azurewebsites.net";
    public static MongoClient mongoClient;
        public static IMongoDatabase database;
        public static IMongoCollection<BsonDocument> collection;
    }

