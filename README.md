Project dtags written .NET C# creates program dtag.exe for downloading music meta information from discogs.com.<br>
It is mainly for generating cue-sheet files based on Discogs resource id.

Usage is quite simple:
  dtag.exe {r|d}id [txt|cue|chap|srt|json] [filename]

Examples:

- dtag.exe r23900384 cue image.cue<br>
creates cue-sheet image.cue file from the json file downloaded from https://api.discogs.com/releases/23900384

- dtag.exe r10960506<br>
writes text information to the console

- dtag.exe m1000 json image.json<br>
writes downloaded json from https://api.discogs.com/masters/1000 to the image.json file

Index can be in the form [r23900384] like used on discogs.com pages.

I have been inspired by sorts of dcue projects written in C++ which are unnecesessarily complicated.

Piotr Filipowicz
