using System;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Discord
{
    /// <summary>
    /// An abstract class to create Discord feature classes with.
    /// </summary>
    public abstract class Feature
    {
        // Collection of features initialized
        private static readonly Dictionary<Type, Feature> _features;
        // Lock for _features and int for keep/generating feature hashes
        private static int _lock, _hashGen;

        // Feature instance hash
        private readonly int _hashCode;

        /// <summary>
        /// A DiscordSocketClient which is exposed to all features
        /// </summary>
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

        /// <summary>
        /// Try to get the instance for the feature
        /// </summary>
        /// <typeparam name="T">The name of the class which inherits from the Feature class.</typeparam>
        /// <param name="feature">The instance of a Feature.</param>
        /// <returns>true if successfully retreived an instance of the specified Feature.</returns>
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

        /// <summary>
        /// Creates the DiscordSocketClient, logs in, initializes all features and starts the bot
        /// </summary>
        /// <param name="tokenType">Specifies the type of token to use with the client.</param>
        /// <param name="token">The discrod token.</param>
        /// <param name="config">DiscordSocketClient configuration.</param>
        /// <returns>A task which logs into Discord using the specified credentials and configuration.</returns>
        /// <exception cref="InvalidOperationException">Using DiscordFeature you are only allowed one instance.</exception>
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

        /// <summary>
        /// Stops the bot and logs out of discord
        /// </summary>
        /// <returns>A task which logs out of Discord.</returns>
        /// <exception cref="NullReferenceException">There is no client to log out of.</exception>
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

        /// <summary>
        /// Waits for user input to continue
        /// </summary>
        /// <param name="keyword">The keyword to wait for.</param>
        /// <returns>A task which waits for a specified input.</returns>
        public static Task LogoutKeyword(string keyword = "")
        {
            return Task.Run(() =>
            {
                while ((Console.ReadLine() ?? "") != keyword) ;
                return LogoutAsync();
            });
        }

        /// <summary>
        /// Creates a Feature.
        /// </summary>
        /// <exception cref="Exception">DiscordFeature instances cannot be manually created.</exception>
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

        /// <summary>
        /// Method for initializing Features
        /// </summary>
        /// <param name="client">The client to pass into sub-classes of Feature.</param>
        public abstract void Init(in DiscordSocketClient client);

        /// <summary>
        /// Path to be used for feature specific files
        /// </summary>
        /// <param name="relativePath">The relative file name or directory name.</param>
        /// <returns>A full path which describes the file or directory constucted.</returns>
        public string Path(params string[] relativePath) => System.IO.Path.GetFullPath(@$"Features\{GetType().Name}\{System.IO.Path.Combine(relativePath)}");

        /// <summary>
        /// For ensuring single feature instances when being compared
        /// </summary>
        /// <param name="obj">The object to be compared to.</param>
        /// <returns>true if object is of the same type, otherwise false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj != null)
                return GetType() == obj.GetType();
            return false;
        }
        /// <summary>
        /// For ensuring single feature instances when being compared
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => _hashCode;
    }
}