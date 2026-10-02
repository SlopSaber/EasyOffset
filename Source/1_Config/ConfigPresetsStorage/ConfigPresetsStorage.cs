using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using IPA.Utilities;

namespace EasyOffset {
    public static class ConfigPresetsStorage {
        private static readonly string PresetsFolderPath = Path.Combine(UnityGame.UserDataPath, "EasyOffset\\Presets\\");

        #region SaveCurrentPreset

        public static bool SaveCurrentPreset(string fileName) {
            var absolutePath = Path.Combine(PresetsFolderPath, $"{fileName}.json");
            return PresetUtils.WritePresetToFile(absolutePath, PluginConfig.GeneratePreset());
        }

        #endregion

        #region LoadPreset

        public static bool LoadPreset(string fileName) {
            var absolutePath = Path.Combine(PresetsFolderPath, $"{fileName}.json");
            if (!PresetUtils.ReadPresetFromFile(absolutePath, out var preset)) return false;
            PluginConfig.ApplyPreset(preset);
            return true;
        }

        #endregion

        #region GetAllStoredPresets

        public static List<StoredConfigPreset> GetAllStoredPresets() {
            lock (OwnedFileWork.FileGate) {
                CreateDirectoryIfNecessary();

                var allPresets = new List<StoredConfigPreset>();

                foreach (var absoluteFilePath in GetAllJsonFilePaths()) {
                    var successful = PresetUtils.ReadPresetFromFile(absoluteFilePath, out var preset);
                    var storedConfigPreset = new StoredConfigPreset(
                        absoluteFilePath,
                        !successful,
                        preset
                    );
                    allPresets.Add(storedConfigPreset);
                }

                allPresets.Sort();
                allPresets.Reverse();
                return allPresets;
            }
        }

        internal static Task<OwnedFileWork.Result> ReadCatalogAsync(CancellationToken token) =>
            OwnedFileWork.Queue(new OwnedFileWork.Request(OwnedFileWork.Operation.Catalog, PresetsFolderPath, token));

        internal static List<StoredConfigPreset> BuildCatalog(OwnedFileWork.Result result) {
            var presets = new List<StoredConfigPreset>();
            foreach (var file in result.Files) {
                var successful = PresetUtils.ReadPresetFromJson(file.Json, out var preset);
                presets.Add(new StoredConfigPreset(file.Path, !successful, preset));
            }
            presets.Sort();
            presets.Reverse();
            return presets;
        }

        internal static Task<OwnedFileWork.Result> ReadPresetAsync(string fileName, CancellationToken token) =>
            OwnedFileWork.Queue(new OwnedFileWork.Request(OwnedFileWork.Operation.ReadJson,
                Path.Combine(PresetsFolderPath, $"{fileName}.json"), token));

        internal static Task<OwnedFileWork.Result> SaveCurrentPresetAsync(string fileName, CancellationToken token) {
            try {
                var text = PluginConfig.GeneratePreset().Serialize().ToString();
                return OwnedFileWork.Queue(new OwnedFileWork.Request(OwnedFileWork.Operation.WriteText,
                    Path.Combine(PresetsFolderPath, $"{fileName}.json"), token, text));
            } catch (Exception error) {
                return Task.FromResult(new OwnedFileWork.Result { Error = error });
            }
        }

        #endregion

        #region Utils

        private static string[] GetAllJsonFilePaths() {
            return Directory.GetFiles(PresetsFolderPath, "*.json");
        }

        private static void CreateDirectoryIfNecessary() {
            var isDirectoryPresent = Directory.Exists(PresetsFolderPath);
            if (!isDirectoryPresent) Directory.CreateDirectory(PresetsFolderPath);
        }

        #endregion
    }
}
