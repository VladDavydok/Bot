using Microsoft.VisualBasic;
using ParksForecastBot;
using Telegram.Bot.Types;
using Telegram.Bot;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Globalization;
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;

class Program
{
    static ITelegramBotClient bot = new TelegramBotClient(constants.botId);
    static HttpClient httpClient = new HttpClient();
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message && update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
        {
            var message = update.Message;
            User user = message.From;
            string user_firstname = user.FirstName;
            long user_id = user.Id;

            var parks_list = new List<Park>();

            var document = new BsonDocument
                    {
                        { "user_id", user_id},
                        { "user_firstname", user_firstname },
                         { "CityName", string.Empty },
                          { "Lat", string.Empty },
                         {"bot_is_waiting_for_park_city", false },
                         {"bot_is_waiting_for_forecast_days", false },
                         {"bot_is_waiting_for_lat", false },
                          {"bot_is_waiting_for_lng", false },
                {"bot_is_waiting_for_number_of_park_to_add", false },
                {"bot_is_waiting_for_number_of_park_to_delete", false },
                  {"parks", new BsonArray(parks_list.Select(t => t.ToBsonDocument())) }
            };


            var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
            var exists = constants.collection.Find(filter).Any();

            if (!exists)
            {
                constants.collection.InsertOne(document);
            }

            var resp = await httpClient.GetAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_park_city/{user_id}");
            var res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_park_city = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_forecast_days/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_forecast_days = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_lat/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_lat = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_lng/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_lng = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_number_of_park_to_add/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_number_of_park_to_add = Convert.ToBoolean(res);
            resp = await httpClient.GetAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_number_of_park_to_delete/{user_id}");
            res = await resp.Content.ReadAsStringAsync();
            bool bot_is_waiting_for_number_of_park_to_delete = Convert.ToBoolean(res);

            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привіт!\nЯ допоможу тобі підібрати парк та найкращу дату для пікніка. Виберіть команду /keyboard, щоб дізнатись що я можу.");
                return;
            }
            if (message.Text.ToLower() == "/keyboard")
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new(new[] { new KeyboardButton[] { "/parks", "/forecast" }, new KeyboardButton[] { "/water", "/my_parks" },  new KeyboardButton[] { "/add_park_to_my_list", "/delete_park_from_my_list" } })
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Виберіть пункт меню", replyMarkup: replyKeyboardMarkup);
                return;
            }
            if (message.Text.ToLower() == "/forecast")
            {
                await bot.SendTextMessageAsync(user_id, "Добре, введіть кількість днів, на скільки вперед ви хочете дізнатися погоду(допустимо від 0 до 14).");
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_forecast_days/{user_id}?b=true", null);
                return;
            }
            if (message.Text.ToLower() == "/water")
            {
                await bot.SendTextMessageAsync(user_id, "Добре, введіть широту(її можна отримати через /parks).");
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_lat/{user_id}?b=true", null);
                return;
            }
            if (message.Text.ToLower() == "/parks")
            {
                await bot.SendTextMessageAsync(user_id, "Добре, введіть назву міста");
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_park_city/{user_id}?b=true", null);
                return;
            }
            if (message.Text.ToLower() == "/add_park_to_my_list")
            {
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_number_of_park_to_add/{user_id}?b=true", null);
                await bot.SendTextMessageAsync(user_id, "Добре, введіть номер парку, який ви хочете додати у свій список. Не забудь зафіксувати назву міста в /parks.");
                return;
            }
            if (message.Text.ToLower() == "/delete_park_from_my_list")
            {
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_number_of_park_to_delete/{user_id}?b=true", null);
                await bot.SendTextMessageAsync(user_id, "Добре, введіть номер парку, який ви хочете видалити зі свого списку");
                return;
            }
            if (message.Text.ToLower() == "/my_parks")
            {
                await httpClient.PostAsync($"https://{constants.host}/Controllers/post_my_parks_list?id={user_id}", null);
                return;
            }
            if (bot_is_waiting_for_park_city)
            {
                string CityName = message.Text;
                try
                {
                    await httpClient.PostAsync($"https://{constants.host}/Controllers/post_parks_list?id={user_id}&CityName={CityName}", null);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                }
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_park_city/{user_id}?b=false", null);
                return;
            }
            if (bot_is_waiting_for_forecast_days)
            {
                string number = message.Text;
                if (double.TryParse(number, out double numberValue))
                {
                    var filters = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var documents = constants.collection.Find(filters).FirstOrDefault();
                    string cityName = documents.GetValue("CityName").AsString;
                try
                {
                    int i = Convert.ToInt32(number);
                    await httpClient.PostAsync($"https://{constants.host}/Controllers/post_forecast_list?id={user_id}&%D1%81ityName={cityName}&number={number}", null);
                }
                catch
                {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                }
                await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_forecast_days/{user_id}?b=false", null);
                return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Будь ласка, вводіть лише числове значення.");
                    return;
                }
            }
            if (bot_is_waiting_for_lat)
            {
                string lat = message.Text;
                if (double.TryParse(lat, out double latValue))
                {
                    var filters = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var updates = Builders<BsonDocument>.Update.Set("Lat", lat);
                    var results = constants.collection.UpdateOne(filters, updates);
                    var documents = constants.collection.Find(filters).FirstOrDefault();
                    try
                    {
                        await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_lng/{user_id}?b=true", null);
                    }
                    catch
                    {
                        await botClient.SendTextMessageAsync(user_id, "Помилка");
                    }
                    await botClient.SendTextMessageAsync(user_id, "Добре, введіть довготу(її можна отримати через /parks)");
                    await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_lat/{user_id}?b=false", null);
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Будь ласка, вводіть лише числове значення.");
                    return;
                }
            }
            if (bot_is_waiting_for_lng)
            {
                string lng = message.Text;
                if (double.TryParse(lng, out double lngValue))
                {
                  var filters = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                  var documents = constants.collection.Find(filters).FirstOrDefault();
                  string lat = documents.GetValue("Lat").AsString;
                  try
                  {
                    await httpClient.PostAsync($"https://{constants.host}/Controllers/post_water_list?id={user_id}&lat={lat}&lng={lng}", null);
                  }
                  catch
                  {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                  }
                  await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_lng/{user_id}?b=false", null);
                  return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Будь ласка, вводіть лише числове значення.");
                    return;
                }
            }
            if (bot_is_waiting_for_number_of_park_to_add)
            {
                string number = message.Text;
                if (double.TryParse(number, out double numberValue))
                {
                    var filters = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
                    var documents = constants.collection.Find(filters).FirstOrDefault();
                    string cityName = documents.GetValue("CityName").AsString;
                    try
                    {
                            int i = Convert.ToInt32(number);
                            await httpClient.PutAsync($"https://{constants.host}/Controllers/put_park_to_list?id={user_id}&CityName={cityName}&number={number}", null);
                    }
                    catch
                    { 
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                    }
                    await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_number_of_park_to_add/{user_id}?b=false", null);
                    return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Будь ласка, вводіть лише числове значення.");
                    return;
                }
            }
            if (bot_is_waiting_for_number_of_park_to_delete)
            {
                string number = message.Text;
                if (double.TryParse(number, out double numberValue))
                {
                 try
                 {
                        int i = Convert.ToInt32(number);
                        await httpClient.DeleteAsync($"https://{constants.host}/Controllers/delete_park_from_list?id={user_id}&number={number}");
                 }
                 catch
                 {
                    await botClient.SendTextMessageAsync(user_id, "Помилка");
                 }
                 await httpClient.PutAsync($"https://{constants.host}/Controllers/bot_is_waiting_for_number_of_park_to_delete/{user_id}?b=false", null);
                 return;
                }
                else
                {
                    await botClient.SendTextMessageAsync(user_id, "Будь ласка, вводіть лише числове значення.");
                    return;
                }
            }
            await botClient.SendTextMessageAsync(user_id, "Я не розумію, що ти хочеш");
            return;
        }
        else
        {
            if (update.Message != null && update.Message.From != null)
            {
                long user_id = update.Message.From.Id;
                await botClient.SendTextMessageAsync(user_id, "Я розрахований лише на текстові повідомлення");
            }
        }
    }
    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }



    static void Main(string[] args)
    {

        Console.WriteLine("Запущен бот" + bot.GetMeAsync().Result.FirstName);

        constants.mongoClient = new MongoClient("mongodb+srv://Vladdavydok:09123456d@cluster0.kzkmm9o.mongodb.net/");
        constants.database = constants.mongoClient.GetDatabase("park");
        constants.collection = constants.database.GetCollection<BsonDocument>("collection1");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { },
        };
        bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddRazorPages();
        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}