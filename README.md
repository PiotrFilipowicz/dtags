Project dtag written .NET C# creates program dtag.exe for downloading music meta information from discogs.com.<br>
It is mainly for generating cuesheet files based on Discogs resource id.

Usage is quite simple:
  dtag.exe {r|d}id [all|txt|cue|chap|srt|json] [filename]

Examples:

- dtag.exe r23900384 cue image.cue<br>
creates cuesheet image.cue file from json file downloaded from https://api.discogs.com/releases/23900384

- dtag.exe r10960506<br>
writes text information on console

- dtag.exe m1000 json image.json<br>
writes downloaded json from https://api.discogs.com/masters/1000 to the image.json file

Index can be in the form [r23900384] like used on discogs.com pages.

I was inspired by sorts of dcue projects written in C++ which are unnecesessarily complicated.

Piotr Filipowicz
