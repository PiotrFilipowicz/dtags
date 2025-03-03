using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using static dtags.Meta;

namespace dtags {
    class Program {
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
            //[r10960506], [r23900384], [r27]
            //r23900552 1 fmts, 2 discs
            //[r5302954] 4 fmts, 4 discs
            //[r11252330] 2 discs
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
                OutJson(fileName, json);
                return 0;
            }
            if (mode == "all" && fileName != null)
                OutJson($"{fileName}.json", json);

            Album album = new Album(json);
            if (album == null) {
                Console.WriteLine("Error: json not parsed!");
                return -1;
            }

            if (album.Discs == null || album.Discs.Count == 0) {
                Console.WriteLine("Error: no discs!");
                return -1;
            }
            foreach (var disc in album.Discs) {
                string? currentFileName = fileName;
                if (!string.IsNullOrEmpty(fileName) && album.Discs.Count > 1) {
                    int index = fileName.LastIndexOf('.');
                    if (index > 0)
                        currentFileName = $"{fileName[..index]}.{disc.Label}{fileName[index..]}";
                    else
                        currentFileName += $".{disc.Label}";
                }
                if (mode == "txt") {
                    OutTags(currentFileName, disc, Meta.Text);
                    continue;
                }
                if (disc.Total == 0) {
                    Console.WriteLine($"Error: {id} - disc {disc.Label} has no tracks duration!");
                    continue;
                }
                if (disc.HasEmptyTrack) {
                    Console.WriteLine($"Error: {id} - disc {disc.Label} has one or more empty tracks!");
                    continue;
                }
                if (mode == "cue")
                    OutTags(currentFileName, disc, Meta.Cue);
                else if (mode == "chap")
                    OutTags(currentFileName, disc, Meta.Chapters);
                else if (mode == "srt")
                    OutTags(currentFileName, disc, Meta.Srt);
            }
            return 0;
        }

        static (bool, string id, string mode, string fileName) GetParams(string[] args) {
            string[] exts = { "txt", "cue", "chap", "srt", "json" };
            string s = string.Join('|', exts);

            if (args.Length == 0) {
                Console.WriteLine($"arguments: {{r|d}}id [{string.Join('|', exts)}] [filename]");
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
            else if (exts.Contains(args[1]))
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

        static void OutJson(string fileName, JsonObject json) {
            if (string.IsNullOrEmpty(fileName))
                Console.Write(json.ToString());
            else {
                FileStream fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                fs.Write(Encoding.UTF8.GetBytes(json.ToString()));
                fs.Close();
            }
        }
    }
}
