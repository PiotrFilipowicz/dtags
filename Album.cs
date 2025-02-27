using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

namespace dtags {
    internal class Album {
        internal string? Title { get; set; }
        internal string? Year { get; set; }
        internal string? Artists { get; set; }
        internal string? ReleaseId { get; set; }
        internal string? MasterId { get; set; }
        internal string? RecentId { get; set; }
        internal string? MainId { get; set; }
        internal List<Disc>? Discs { get; set; }

        internal Album(JsonObject json) {
            Title = ((string?)json["title"])?.Trim();
            Artists = Helpers.Artists(json);

            string? id = Helpers.GetId((string?)json["resource_url"]);
            if (id != null)
                if (id[0] == 'r') {
                    ReleaseId = id;
                    MasterId = Helpers.GetId((string?)json["master_url"]);
                }
                else if (id[0] == 'm')
                    MasterId = id;
            RecentId = Helpers.GetId((string?)json["most_recent_release_url"]);
            MainId = Helpers.GetId((string?)json["main_release_url"]);
            var Year = json["year"]?.ToString();
            if (Year == "0")
                Year = null;
            var jTracks = json["tracklist"]?.AsArray();
            if (jTracks == null)
                return;
            Discs = new List<Disc>();
            int jTrackIndex = 0;
            while (jTrackIndex < jTracks.Count) {
                var disc = new Disc(this);
                Discs.Add(disc);
                if (disc.ScanTracks(jTracks, ref jTrackIndex)) //Disk label changed
                    jTrackIndex--;
            }
        }

        internal static Album FromJson(JsonObject json) {
            return new Album(json);
        }
    }

    internal class Disc {
        internal Album Album { get; set; }
        internal string? DiscLabel { get; set; }
        internal string? Section { get; set; }
        internal string? Composer { get; set; }
        internal double Total { get; set; }
        internal bool HasEmptyTrack { get; set; }
        public List<Track> Tracks { get; set; }

        internal Disc(Album album) {
            Album = album;
            Tracks = new List<Track>();
        }

        internal bool ScanTracks(JsonArray jTracks, ref int jTrackIndex) {
            while (jTrackIndex < jTracks.Count) {
                var jTrack = jTracks[jTrackIndex++];
                var duration = (string?)jTrack!["duration"];
                if (duration == null)
                    continue;
                if (duration == "") { // new section
                    var subtracks = (JsonArray?)jTrack?["sub_tracks"];
                    if (subtracks != null) { // subtracks
                        Section = ((string?)jTrack?["title"])?.Trim() ?? Section;
                        Composer = Helpers.Artists(jTrack) ?? Composer;
                        int subtrackIndex = 0;
                        if (ScanTracks(subtracks, ref subtrackIndex))
                            return true; //disc label changed
                        continue;
                    }
                }

                var position = (string?)jTrack!["position"];
                if (!string.IsNullOrWhiteSpace(position)) {
                    string[] parts = position.Split(['.', '-', '_', '/']);
                    if (parts.Length == 2) {
                        if (DiscLabel == null)
                            DiscLabel = parts[0];
                        else if (DiscLabel != parts[0])
                            return true;
                    }
                    var track = new Track();
                    track.Position = position.Trim();
                    track.Section = Section;
                    track.Composer = Composer;
                    track.Artists = Helpers.Artists(jTrack)?.Trim();
                    track.Title = ((string?)jTrack!["title"])?.Trim();
                    track.Index = Total;
                    if (!string.IsNullOrEmpty(duration)) {
                        int index = duration.IndexOf(':');
                        int seconds;
                        if ((index > 0 && index < duration.Length - 2))
                            seconds = Int32.Parse(duration[..index]) * 60 + Int32.Parse(duration[(index + 1)..]);
                        else
                            seconds = Int32.Parse(duration);
                        track.Duration = seconds;
                        Total += seconds;
                    }
                    else
                        HasEmptyTrack = true;

                    Tracks.Add(track);
                }
            }
            return false;
        }
    }

    internal class Track {
        internal string? Position { get; set; }
        internal string? Title { get; set; }
        internal string? Artists { get; set; }
        internal string? Section { get; set; }
        internal string? Composer { get; set; }
        internal double Index { get; set; }
        internal double Duration { get; set; }
    }

    internal static class Helpers {
        internal static string? Artists(JsonNode? track) {
            if (track == null)
                return null;
            JsonArray? trackArtistsArray = track["artists"]?.AsArray();
            if (trackArtistsArray != null) {
                JsonArray? trackExtraartists = track["extraartists"]?.AsArray();
                if (trackExtraartists != null)
                    trackArtistsArray.Concat(trackExtraartists);
            }
            else
                trackArtistsArray = track["extraartists"]?.AsArray();
            return Artists(trackArtistsArray);
        }

        private static string? Artists(JsonArray? artists) => (artists != null) ? string.Join(", ", artists.Select(a => ((string?)a?["name"])?.Trim())) : null;
 
        internal static string? GetId(string? url) => (url != null) ? $"{((url?.Contains("masters") == true) ? "m" : "r")}{url?.Split('/')[^1]}" : null;

        internal static string? SectionText(Track track) => (track.Composer != null) ? (track.Section != null) ? $"{track.Section} ({track.Composer})" : $"({track.Composer})" : track.Section;

        internal static string? DiscText(Disc disc) {
            string? text = null;
            string? s;
            if ((s = disc.Album.Artists) != null)
                text = s;
            if ((s = disc.Album.Year) != null)
                text = (text != null) ? $"{text} [{s}]" : $"[{s}]";
            if ((s = disc.Album.Title) != null)
                text = (text != null) ? $"{text} - {s}" : s;
            return text;
        }

        internal static string? DurationText(Track track) {
            double? time = track.Duration;
            if (time != 0) {
                int minutes = (int)time / 60;
                int seconds = (int)time % 60;
                return $"{minutes}:{seconds:D2}";
            }
            return null;
        }

        internal static string? TrackText(Track track) {
            string? text = null;
            string? s;

            if ((s = track.Title) != null)
                text = (text != null) ?
                    $"{text} {s}" : s;
            if ((s = track.Artists) != null)
                text = (text != null) ? $"{text} ({s})" : $"({s})";
            return text;
        }
        internal static string? TrackSectionText(Track track) {
            string? s = Helpers.SectionText(track);
            return (s != null) ? $"{Helpers.TrackText(track)} - {s}" : Helpers.TrackText(track);
        }

    }

}
