using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace dtags {
    internal class Program {
        static async Task<int> Main1(string[] args) {
            try {
                int count = 0;
                for (int i = Int32.Parse(args[0]); i < Int32.Parse(args[1]); i++) {
                    count++;
                    if (count == 25) {
                        count = 0;
                        await Task.Delay(Int32.Parse(args[2]) * 1000);
                    }
                    await dtags([$"[r{i}]", "json", $"r{i}.json"]);
                }
                return 0;
            }
            catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                return -1;
            }
        }

        static async Task<int> Main(string[] args) {
            try {
                return await dtags(args);
            }
            catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                return -1;
            }
        }

        static async Task<int> dtags(string[] args) {
            //check [r10960506], [r23900384], [r27]
            //r23900552 1 fragment, 2 discs
            (bool res, string id, string mode, string fileName) = GetParams(args);
            if (!res)
                return -1;

            string? jsonString = (File.Exists(id)) ? File.ReadAllText(id) : await QueryDiscogs(id);
            if (jsonString == null)
                return -1;

            JsonObject? json = GetJson(jsonString);
            if (json == null)
                return -1;
            if (mode == "json") {
                WriteJson(fileName, json);
                return 0;
            }
            if (mode == "all" && fileName != null)
                WriteJson($"{fileName}.json", json);

            Album album = new Album(json);
            if (album == null || album.Discs == null)
                return -1;

            if (album.Discs.Count == 0) {
                Console.WriteLine("Error: no discs!");
                return -1;
            }

            if (mode == "txt") {
                foreach (var elem in album.Discs)
                    CreateMeta(fileName, elem, WriteMeta.Text);
                return 0;
            }
            if (mode == "all" && fileName != null) {
                foreach (var elem in album.Discs)
                    CreateMeta($"{fileName}.txt", elem, WriteMeta.Text);
            }

            var disc = album.Discs[0];
            if (disc.Total == 0) {
                Console.WriteLine($"Error: {id} - no tracks duration!");
                return -1;
            }
            if (disc.HasEmptyTrack) {
                Console.WriteLine($"Error: {id} - one or more empty tracks!");
                return -1;
            }

            if (mode == "cue")
                CreateMeta(fileName, disc, WriteMeta.Cue);
            else if (mode == "chap")
                CreateMeta(fileName, disc, WriteMeta.Chapters);
            else if (mode == "srt")
                CreateMeta(fileName, disc, WriteMeta.Srt);
            else if (mode == "all" && !string.IsNullOrEmpty(fileName)) {
                CreateMeta($"{fileName}.cue", disc, WriteMeta.Cue);
                CreateMeta($"{fileName}.chap", disc, WriteMeta.Chapters);
                CreateMeta($"{fileName}.srt", disc, WriteMeta.Srt);
            }
            else
                return -1;
            return 0;
        }

        static (bool, string id, string mode, string fileName) GetParams(string[] args) {
            string[] exts = { "txt", "cue", "chap", "srt", "json" };
            string s = string.Join('|', exts);

            if (args.Length == 0) {
                Console.WriteLine($"arguments: {{r|d}}id [all|{string.Join('|', exts)}] [filename]");
                return (false, "", "", "");
            }
            string id = args[0];
            if (id[0] == '[')
                id = id[1..];
            if (id[^1] == ']')
                id = id[..^1];

            if (id.Length < 2) {
                Console.WriteLine("id is too short");
                return (false, "", "", "");
            }
            if (id[0] != 'r' && id[0] != 'm') {
                Console.WriteLine("bad id");
                return (false, "", "", "");
            }
            if (args.Length == 1)
                return (true, id, "txt", "");
            else if (args.Length == 2) {
                if (exts.Contains(args[1]))
                    return (true, id, args[1], "");
            }
            else if (exts.Contains(args[1]) || args[1] == "all")
                return (true, id, args[1], args[2]);
            return (false, "", "", "");
        }

        static async public Task<string?> QueryDiscogs(string id) {
            string url;
            if (id[0] == 'r')
                url = $"https://api.discogs.com/releases/{id[1..]}";
            else if (id[0] == 'm')
                url = $"https://api.discogs.com/masters/{id[1..]}";
            else
                return "";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "dtags/0.1");
            client.DefaultRequestHeaders.Add("Connection", "close");
            HttpResponseMessage response;
            try {
                response = await client.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.OK) {
                    Console.WriteLine($"Discogs: {response.StatusCode}");
                    return null;
                }
            }
            catch (HttpRequestException e) {
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
            string? jsonString = await response.Content.ReadAsStringAsync();
            if (jsonString == null)
                Console.WriteLine("Error: no json!");
            return jsonString;
        }

        static public JsonObject? GetJson(string jsonString) {
            try {
                return JsonSerializer.Deserialize<JsonObject>(jsonString);
            }
            catch (Exception e) {
                Console.WriteLine("Error: cannot serialize json!");
                Console.WriteLine($"Error: {e.Message}");
                return null;
            }
        }

        static void WriteJson(string fileName, JsonObject json) {
            if (string.IsNullOrEmpty(fileName))
                Console.Write(json.ToString());
            else {
                FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                fs.Write(Encoding.UTF8.GetBytes(json.ToString()));
                fs.Close();
            }
        }

        static void CreateMeta<T>(string? metaName, T disc, Action<T, Action<string, object?>> WriteMeta) {
            FileStream? fs = (string.IsNullOrEmpty(metaName)) ? null : File.Open(metaName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            Action<string, object?> FormatWrite = (fs == null) ? (f, o) => { if (o != null) Console.Write(string.Format(f, o)); }
            : (f, o) => { if (o != null) fs.Write(Encoding.UTF8.GetBytes(string.Format(f, o))); };
            WriteMeta(disc, FormatWrite);
            fs?.Close();
        }
    }
}
