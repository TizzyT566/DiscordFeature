using System;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Discord
{
    public abstract class Feature
    {
        // Collection of features initialized
        private static readonly Dictionary<Type, Feature> _features;
        // Lock for _features and int for keep/generating feature hashes
        private static int _lock, _hashGen;

        // Feature instance hash
        private readonly int _hashCode;

        // A DiscordSocketClient which is exposed to all features
        private static DiscordSocketClient? _client;

        static Feature()
        {
            // Init values
            _features = new();
            _lock = 0;
            _hashGen = 0;

            // Starts dedicated logging
            System.Logging.StartLogging();

            // Creates a new instance for each feature
            foreach (Type type in System.Reflection.Assembly.GetEntryAssembly().GetTypes())
                if (type != typeof(Feature) && typeof(Feature).IsAssignableFrom(type) && type.FullName != null)
                    try
                    {
                        Feature? feature = (Feature?)type.Assembly.CreateInstance(type.FullName);
                        if (feature != null)
                            _features.TryAdd(type, feature);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
        }

        // Try to get the instance for the feature
        public static bool TryGetFeatureInstance<T>(out T? feature) where T : Feature, new()
        {
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            bool result = _features.TryGetValue(typeof(T), out Feature? outVar);
            Interlocked.Exchange(ref _lock, 0);
            feature = outVar as T;
            return result;
        }

        // Method for initializing all required events and values for a given feature
        public abstract void Init(in DiscordSocketClient client);

        private static Task LogMessage(LogMessage message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        public static async Task LoginAsync(TokenType tokenType, string token, DiscordSocketConfig? config = null)
        {
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;

            if (_client != null)
                throw new InvalidOperationException("Only one instance of bot permitted.");

            _client = config == null ? new() : new(config);

            await _client.LoginAsync(tokenType, token);

            _client.Log += LogMessage;

            foreach (Feature feature in _features.Values)
                feature.Init(_client);

            await _client.StartAsync();

            Interlocked.Exchange(ref _lock, 0);
        }

        public static async Task LogoutAsync()
        {
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            if (_client == null)
                throw new InvalidOperationException("Bot has not started.");
            _client.Log -= LogMessage;
            await _client.LogoutAsync();
            Interlocked.Exchange(ref _lock, 0);
        }

        public static Task LogoutKeyword(string keyword = "")
        {
            return Task.Run(() =>
            {
                while (true)
                    if ((Console.ReadLine() ?? "").Trim().ToLower() == keyword)
                        break;
                return LogoutAsync();
            });
        }

        public Feature()
        {
            bool result = true;
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            if (_features.ContainsKey(GetType()))
                result = false;
            // Generates a unique hash code for any implementing classes
            Interlocked.Exchange(ref _lock, 0);
            if (result)
            {
                _hashCode = Interlocked.Increment(ref _hashGen);
                System.Logging.EnableLogger(GetType().Name);
            }
            else
                throw new Exception("Feature instances cannot be manually created.");
        }

        // Dedicated logging methods
        public void Log(string message, Action? misc = null) => System.Logging.Log(GetType().Name, message, misc);
        public void Log(string message, long ticks, Action? misc = null) => System.Logging.Log(GetType().Name, message, ticks, misc);
        public void Log(string message, ConsoleColor foreColor, long ticks = 0, Action? misc = null) => System.Logging.Log(GetType().Name, message, foreColor, ticks, misc);
        public void Log(string message, ConsoleColor foreColor, ConsoleColor backColor, long ticks = 0, Action? misc = null) => System.Logging.Log(GetType().Name, message, foreColor, backColor, ticks, misc);

        public void LogLine(string message, Action? misc = null) => System.Logging.LogLine(GetType().Name, message, misc);
        public void LogLine(string message, long ticks, Action? misc = null) => System.Logging.LogLine(GetType().Name, message, ticks, misc);
        public void LogLine(string message, ConsoleColor foreColor, long ticks = 0, Action? misc = null) => System.Logging.LogLine(GetType().Name, message, foreColor, ticks, misc);
        public void LogLine(string message, ConsoleColor foreColor, ConsoleColor backColor, long ticks = 0, Action? misc = null) => System.Logging.LogLine(GetType().Name, message, foreColor, backColor, ticks, misc);

        // For ensuring single feature instances when being compared
        public override bool Equals(object? obj)
        {
            if (obj != null)
                return GetType() == obj.GetType();
            return false;
        }
        public override int GetHashCode() => _hashCode;
    }
}
