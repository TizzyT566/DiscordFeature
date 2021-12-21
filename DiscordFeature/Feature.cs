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
        public static DiscordSocketClient? Client { get; private set; }

        static Feature()
        {
            // Init values
            _features = new();
            _lock = 0;
            _hashGen = 0;

            // Creates a new instance for each feature
            Type featureType = typeof(Feature);
            foreach (Type type in System.Reflection.Assembly.GetEntryAssembly().GetTypes())
                if (!type.IsAbstract && featureType.IsAssignableFrom(type) && type.FullName != null)
                    try
                    {
                        Feature feature = (Feature)type.Assembly.CreateInstance(type.FullName);
                        _features.TryAdd(type, feature);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
        }

        // Try to get the instance for the feature
        public static bool TryGetFeatureInstance<T>(out T? feature) where T : Feature
        {
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            bool result = _features.TryGetValue(typeof(T), out Feature? outVar) && outVar != null;
            Interlocked.Exchange(ref _lock, 0);
            feature = outVar as T;
            return result;
        }

        private static Task LogMessage(LogMessage message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        // Creates the DiscordSocketClient, logs in, initializes all features and starts the bot
        public static async Task LoginAsync(TokenType tokenType, string token, DiscordSocketConfig? config = null)
        {
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            if (Client != null)
                throw new InvalidOperationException("Only one instance of bot permitted.");
            Client = config == null ? new() : new(config);
            await Client.LoginAsync(tokenType, token);
            Client.Log += LogMessage;
            foreach (Feature feature in _features.Values)
                feature.Init(Client);
            await Client.StartAsync();
            Interlocked.Exchange(ref _lock, 0);
        }

        // Stops the bot and logs out of discord
        public static async Task LogoutAsync()
        {
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            if (Client == null)
                throw new NullReferenceException();
            Client.Log -= LogMessage;
            await Client.StopAsync();
            await Client.LogoutAsync();
            Interlocked.Exchange(ref _lock, 0);
        }

        // Waits for user input to continue
        public static Task LogoutKeyword(string keyword = "")
        {
            return Task.Run(() =>
            {
                while ((Console.ReadLine() ?? "") != keyword) ;
                return LogoutAsync();
            });
        }

        public Feature()
        {
            bool result = true;
            while (Interlocked.Exchange(ref _lock, 1) == 1) ;
            if (_features.ContainsKey(GetType()))
                result = false;
            // Generates a unique hashcode for any implementing classes
            Interlocked.Exchange(ref _lock, 0);
            if (result)
                _hashCode = Interlocked.Increment(ref _hashGen);
            else
                throw new Exception("Feature instances cannot be manually created.");
        }

        // Method for initializing features
        public abstract void Init(in DiscordSocketClient client);

        // Path to be used for feature specific files
        public string Path(params string[] relativePath) => System.IO.Path.GetFullPath(@$"Features\{GetType().Name}\{System.IO.Path.Combine(relativePath)}");

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