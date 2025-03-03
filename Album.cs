using System.Text.Json.Nodes;

namespace dtags {
    class Album {
        internal string? Title { get; set; }
        internal string? Year { get; set; }
        internal string? Artists { get; set; }
        internal string? ReleaseId { get; set; }
        internal string? MasterId { get; set; }
        internal string? RecentId { get; set; }
        internal string? MainId { get; set; }
        internal string? lastHeading { get; set; }
        internal string? lastComposer { get; set; }
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
                if (disc.ScanTracks(jTracks, ref jTrackIndex)) //Disk label changed
                    jTrackIndex--;
                Discs.Add(disc);
            }
        }

        internal static Album FromJson(JsonObject json) => new Album(json);
    }

    class Disc {
        internal Album Album { get; set; }
        internal string? Label { get; set; }
        internal double Total { get; set; }
        internal bool HasEmptyTrack { get; set; }
        public List<Track> Tracks { get; set; }

        internal Disc(Album album) {
            Album = album;
            Tracks = new List<Track>();
        }

        internal bool ScanTracks(JsonArray jTracks, ref int jTrackIndex) {
            string? GetDiskLabel(string? position) {
                if (position == null || position.Length == 0)
                    return null;
                bool hasSeparator = false;
                for (int i = position.Length - 1; i >= 0; i--) {
                    char c = position[i];
                    if (".-_/".Contains(c))
                        hasSeparator = true;
                    else if (hasSeparator || char.IsLetter(c))
                        return position[..(i + 1)];
                }
                return null;
            }

            while (jTrackIndex < jTracks.Count) {
                var jTrack = jTracks[jTrackIndex++];
                string? type = ((string?)jTrack!["type_"])?.Trim();
                string? position = ((string?)jTrack!["position"])?.Trim();
                if (string.Compare(type, "heading") == 0 || position == null || position == "") { // new heading
                    Album.lastHeading = ((string?)jTrack?["title"])?.Trim() ?? Album.lastHeading;
                    Album.lastComposer = Helpers.Artists(jTrack) ?? Album.lastComposer;
                    var subtracks = (JsonArray?)jTrack?["sub_tracks"];
                    if (subtracks != null) { // subtracks
                        int subtrackIndex = 0;
                        if (ScanTracks(subtracks, ref subtrackIndex))
                            return true; //disc label changed
                    }
                    continue;
                }
                string? diskLabel = GetDiskLabel(position);
                if (Label == null)
                    Label = diskLabel;
                else if (Label != diskLabel)
                    return true;
                //type== "track"
                var track = new Track();
                track.Position = position;
                track.Heading = Album.lastHeading;
                track.Composer = Album.lastComposer;
                track.Artists = Helpers.Artists(jTrack)?.Trim();
                track.Title = ((string?)jTrack!["title"])?.Trim();
                track.Index = Total;
                var duration = (string?)jTrack!["duration"];
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
            return false;
        }
    }

    class Track {
        internal string? Position { get; set; }
        internal string? Title { get; set; }
        internal string? Artists { get; set; }
        internal string? Heading { get; set; }
        internal string? Composer { get; set; }
        internal double Index { get; set; }
        internal double Duration { get; set; }
    }

    static class Helpers {
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

        internal static string? HeadingText(Track track) => (track.Composer != null) ? (track.Heading != null) ? $"{track.Heading} ({track.Composer})" : $"({track.Composer})" : track.Heading;

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


        internal static string? TimeText(double time) {
            if (time == 0)
                return null;
            int minutes = (int)time / 60;
            int seconds = (int)time % 60;
            int hours = (int)time / 3600;
            if (hours == 0)
                return $"{minutes}:{seconds:D2}";
            else
                return $"{hours}:{minutes % 60:D2}:{seconds:D2}";
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
        internal static string? TrackHeadingText(Track track) {
            string? s = Helpers.HeadingText(track);
            return (s != null) ? $"{Helpers.TrackText(track)} - {s}" : Helpers.TrackText(track);
        }

    }
}
