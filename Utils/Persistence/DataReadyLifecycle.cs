using STS2ViewedCardsStatistics.Data;

namespace STS2ViewedCardsStatistics.Utils.Persistence
{
    /// <summary>
    ///     Unified lifecycle API: notifies when profile-scoped mod data path is ready and data has been synchronized.
    ///     Other modules can subscribe to DataReady or queue work through RunWhenReady.
    /// </summary>
    public static class DataReadyLifecycle
    {
        private static readonly List<Action<int>> PendingCallbacks = [];

        public static bool IsReady { get; private set; }
        public static int ReadyProfileId { get; private set; } = -1;

        public static event Action<int, string>? DataReady;

        public static void RunWhenReady(Action<int> action)
        {
            if (IsReady)
            {
                action(ReadyProfileId);
                return;
            }

            PendingCallbacks.Add(action);
        }

        public static void NotifyPotentialReady(string source)
        {
            try
            {
                Main.EnsureRuntimeServicesInitialized();

                if (!ModDataStore.Instance.IsInitialized)
                    return;

                ProfileManager.Instance.RefreshCurrentProfile();
                ModDataStore.Instance.ReloadIfPathChanged();

                var profileDir = ProfileManager.Instance.GetProfileDirectory();
                var pathReady = profileDir.Contains("modded/", StringComparison.OrdinalIgnoreCase);
                if (!pathReady)
                    return;

                var profileId = ProfileManager.Instance.CurrentProfileId;
                var changed = !IsReady || ReadyProfileId != profileId;

                IsReady = true;
                ReadyProfileId = profileId;

                if (!changed)
                    return;

                Main.Logger.Info($"Data ready for profile {profileId} ({source})");
                DataReady?.Invoke(profileId, source);

                if (PendingCallbacks.Count == 0) return;

                var callbacks = PendingCallbacks.ToArray();
                PendingCallbacks.Clear();

                foreach (var callback in callbacks)
                    callback(profileId);
            }
            catch (Exception ex)
            {
                PersistenceLog.Warn($"Failed to notify data ready lifecycle from '{source}': {ex.Message}");
            }
        }
    }
}
