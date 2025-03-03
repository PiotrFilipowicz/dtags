using System.Text;

namespace dtags {
    delegate void Format(string s, params object?[] args);
    static class Meta {
        internal static void Cue(Disc disc, Format Out) {

            string CueTime(double time) {
                int floor = (int)Math.Floor(time);
                int minutes = floor / 60;
                int seconds = floor % 60;
                int ms = (int)((time - floor) * 100);
                return $"{minutes}:{seconds:D2}:{ms:D2}";
            }

            Out("REM YEAR {0}\r\n", disc.Album.Year);

            Out("REM YEAR {0}\r\n", disc.Album.Year);
            Out("REM RELEASEID {0}\r\n", disc.Album.ReleaseId);
            Out("REM MASTERID {0}\r\n", disc.Album.MasterId);
            Out("REM RECENTID {0}\r\n", disc.Album.RecentId);
            Out("REM MAINID {0}\r\n", disc.Album.MainId);
            Out("REM DISCNUMBER {0}\r\n", disc.Label);
            int? nDiscs = disc.Album.Discs?.Count;
            if(nDiscs > 1)
                Out("REM TOTALDISCS {0}\r\n", nDiscs);
            Out("REM DISCDURATION {0}\r\n", CueTime(disc.Total));
            Out("PERFORMER \"{0}\"\r\n", disc.Album.Artists);
            Out("TITLE \"{0}\"\r\n", disc.Album.Title);
            Out("FILE \"image.wav\" WAVE\r\n");

            var tracks = disc.Tracks;
            if (tracks != null) {
                int nTrack = 0;
                foreach (var track in tracks) {
                    if (track.Duration == 0)
                        continue;
                    nTrack++;
                    Out($"\tTRACK {nTrack:D02} AUDIO\r\n");
                    Out("\t\tPERFORMER \"{0}\"\r\n", track.Artists);
                    string? s = Helpers.HeadingText(track);
                    Out($"\t\tTITLE \"{((s != null) ? $"{s} - {track.Title}" : track.Title)}\"\r\n");
                    Out($"\t\tINDEX 01 {CueTime(track.Index)}\r\n");
                    if (track.Duration != 0)
                        Out($"\t\tREM TRACKDURATION {CueTime(track.Duration)}\r\n");
                }
            }
            Out("\r\n");
        }

        internal static void Text(Disc disc, Format Out) {
            Out("{0}", Helpers.DiscText(disc));
            Out(" {0}", Helpers.TimeText(disc.Total));
            Out("\r\n\r\n");

            var tracks = disc.Tracks;
            string? currentHeading = null;
            if (tracks != null)
                foreach (var track in tracks) {
                    string? trackHeading = Helpers.HeadingText(track);
                    if (trackHeading != currentHeading) {
                        currentHeading = trackHeading;
                        Out("  {0}\r\n", currentHeading);
                    }
                    Out($"{track.Position}. {Helpers.TrackText(track)} {Helpers.TimeText(track.Duration)}\r\n");
                }
            Out("\r\n");
        }

        internal static void Chapters(Disc disc, Format Out) {
            const long timeBase = 10000000L;
            string ChaptersTime(double time) => ((long)(time) * timeBase).ToString();

            Out(";FFMETADATA1\r\n");
            Out("artist={0}\r\n", disc.Album.Artists);
            Out("album={0}\r\n", disc.Album.Title);
            var tracks = disc.Tracks;
            if (tracks == null)
                return;
            int n = tracks.Count;
            for (int i = 0; i < tracks.Count; i++) {
                Track track = tracks[i];
                if (track.Duration == 0)
                    continue;
                Out($"[CHAPTER]\r\nTIMEBASE=1/{timeBase}\r\n");
                Out($"START={ChaptersTime(track.Index)}\r\n");
                if (i == n - 1)
                    Out($"END={ChaptersTime(track.Index + track.Duration)}\r\n");
                else
                    Out($"END={ChaptersTime(tracks[i + 1].Index)}\r\n");
                Out($"title={Helpers.TrackHeadingText(track)} {Helpers.TimeText(track.Duration)}\r\n");
            }
            Out("\r\n");
        }

        internal static void Srt(Disc disc, Format Out) {
            string SrtTime(double time) => new TimeSpan((long)time * TimeSpan.TicksPerSecond).ToString(@"hh\:mm\:ss\,fff");
            var tracks = disc.Tracks;
            if (tracks != null) {
                int nTrack = 0;
                foreach (var track in tracks) {
                    if (track.Duration == 0)
                        continue;
                    nTrack++;
                    Out($"{nTrack}\r\n");
                    string from = SrtTime(track.Index);
                    string to = SrtTime(track.Index + track.Duration);
                    Out($"{from} --> {to}\r\n");
                    Out("{0}\r\n", Helpers.DiscText(disc));
                    Out("{0}\r\n", Helpers.HeadingText(track));
                    Out($"{Helpers.TrackText(track)} {Helpers.TimeText(track.Duration)}\r\n\r\n");
                }
                Out("\r\n");
            }
        }

        internal static void OutTags(string? fileName, Disc disc, Action<Disc, Format> Tags) {
            FileStream? fs = (string.IsNullOrEmpty(fileName)) ? null : File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            void Out(string format, params object?[] args) {
                if (args.Count() == 0 || args[0] != null)
                    if (fs == null)
                        Console.Write(string.Format(format, args));
                    else
                        fs.Write(Encoding.UTF8.GetBytes(string.Format(format, args)));
            }
            Tags(disc, Out);
            fs?.Close();
        }
    }
}
