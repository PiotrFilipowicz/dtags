namespace dtags {
    internal static class WriteMeta {
        internal static void Cue(Disc disc, Action<string, object?> Out) {
            string CueTime(double time) {
                int floor = (int)Math.Floor(time);
                int minutes = floor / 60;
                int seconds = floor % 60;
                int ms = (int)((time - floor) * 100);
                return $"{minutes}:{seconds:D2}:{ms:D2}";
            }

            Out("REM YEAR {0}\r\n", disc.Album.Year);
            Out("REM RELEASEID {0}\r\n", disc.Album.ReleaseId);
            Out("REM MASTERID {0}\r\n", disc.Album.MasterId);
            Out("REM RECENTID {0}\r\n", disc.Album.RecentId);
            Out("REM MAINID {0}\r\n", disc.Album.MainId);
            Out("PERFORMER \"{0}\"\r\n", disc.Album.Artists);
            Out("TITLE \"{0}\"\r\n", disc.Album.Title);
            Out("FILE \"image.wav\" WAVE\r\n", "");

            var tracks = disc.Tracks;
            if (tracks != null) {
                int nTrack = 0;
                foreach (var track in tracks) {
                    if (track.Duration == 0)
                        continue;
                    nTrack++;
                    Out("\tTRACK {0:D02} AUDIO\r\n", nTrack);
                    Out("\t\tPERFORMER \"{0}\"\r\n", track.Artists);
                    string? s = Helpers.SectionText(track);
                    Out("\t\tTITLE \"{0}\"\r\n", (s != null) ? $"{s} - {track.Title}" : track.Title);
                    Out("\t\tINDEX 01 {0}\r\n", CueTime(track.Index));
                    if (track.Duration != 0)
                        Out("\t\tREM DURATION {0}\r\n", CueTime(track.Duration));
                }
            }
        }

        public static void Text(Disc disc, Action<string, object?> Out) {
            Out("\r\n", "");
            Out("{0}\r\n\r\n", Helpers.DiscText(disc));

            var tracks = disc.Tracks;
            string? section = null;
            if (tracks != null)
                foreach (var track in tracks) {
                    string? trackSection = Helpers.SectionText(track);
                    if (trackSection != section) {
                        section = trackSection;
                        Out("  {0}\r\n", section);
                    }
                    Out("{0}\r\n", $"{track.Position}. {Helpers.TrackText(track)} {Helpers.DurationText(track)}");
                }
        }

        public static void Chapters(Disc disc, Action<string, object?> Out) {
            const long timeBase = 10000000L;
            string ChaptersTime(double time) => ((long)(time) * timeBase).ToString();

            Out(";FFMETADATA1\r\n", "");
            Out("artist={0}\r\n", disc.Album.Artists);
            Out("album={0}\r\n", disc.Album.Title);
            Out("comment={0}\r\n", "by dtags");
            var tracks = disc.Tracks;
            if (tracks == null)
                return;
            int n = tracks.Count;
            for (int i = 0; i < tracks.Count; i++) {
                Track track = tracks[i];
                if (track.Duration == 0)
                    continue;
                Out("[CHAPTER]\r\nTIMEBASE=1/{0}\r\n", timeBase);
                Out("START={0}\r\n", ChaptersTime(track.Index));
                if (i == n - 1)
                    Out("END={0}\r\n", ChaptersTime(track.Index + track.Duration));
                else
                    Out("END={0}\r\n", ChaptersTime(tracks[i + 1].Index));
                Out("title={0}\r\n", $"{Helpers.TrackSectionText(track)} {Helpers.DurationText(track)}");
            }
        }

        public static void Srt(Disc disc, Action<string, object?> Out) {
            string SrtTime(double time) => new TimeSpan((long)time * TimeSpan.TicksPerSecond).ToString(@"hh\:mm\:ss\,fff");
            var tracks = disc.Tracks;
            if (tracks != null) {
                int nTrack = 0;
                foreach (var track in tracks) {
                    if (track.Duration == 0)
                        continue;
                    nTrack++;
                    Out("{0}\r\n", nTrack);
                    string from = SrtTime(track.Index);
                    string to = SrtTime(track.Index + track.Duration);
                    Out($"{from} --> {to}\r\n", "");
                    Out("{0}\r\n", Helpers.DiscText(disc));
                    Out("{0}\r\n", Helpers.SectionText(track));
                    Out("{0}\r\n\r\n", $"{Helpers.TrackText(track)} {Helpers.DurationText(track)}");
                }
            }
        }
    }
}
